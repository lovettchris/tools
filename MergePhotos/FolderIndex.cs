﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MergePhotos
{
    class MergeOptions
    {
        /// <summary>
        /// Preview what will be copied and deleted, don't actually do it.
        /// </summary>
        public bool Preview;
    }


    class FolderIndex
    {
        bool verbose;
        string dir;
        int files;
        // level 1 index is just based on file length.  Clearly this index could degenerate if there's a lot of files
        // that have the same length, so in those cases where there is > 1 cache hit, we create a level 2 index
        Dictionary<FileLengthHash, List<FileLengthHash>> index1 = new Dictionary<FileLengthHash, List<FileLengthHash>>();
        Dictionary<string, FileLengthHash> reverseIndex = new Dictionary<string, FileLengthHash>(StringComparer.OrdinalIgnoreCase);
        // level 2 index is based on a sha 256 hash of the first 64kb of the file, where this degenerates we create
        // a whole file index
        Dictionary<FileBlockHash, List<FileBlockHash>> index2 = new Dictionary<FileBlockHash, List<FileBlockHash>>();
        // level 3 index is based on sha 256 hash of the entire file.
        Dictionary<EntireFileHash, List<EntireFileHash>> index3 = new Dictionary<EntireFileHash, List<EntireFileHash>>();

        public FolderIndex(string dir, bool verbose=false)
        {
            this.dir = dir;
            this.verbose = verbose;

            Stopwatch watch = new Stopwatch();
            watch.Start();

            // index the files
            CreateLevel1Index(dir);

            watch.Stop();
            Console.WriteLine("Hashed {0} files in {1:N3} seconds", files, (double)watch.ElapsedMilliseconds / 1000.0);
            watch.Reset();
            watch.Start();

            // optimize the index
            OptimizeIndex();

            watch.Stop();
            Console.WriteLine("Optimized the index in {1:N3} seconds", files, (double)watch.ElapsedMilliseconds / 1000.0);
        }

        private void CreateLevel1Index(string path)
        {
            if (verbose) Console.WriteLine(path);
            foreach (string file in Directory.GetFiles(path))
            {
                files++;
                FileLengthHash key = null;
                key = new FileLengthHash(file);
                AddLevel1(key);
            }

            foreach (string dir in Directory.GetDirectories(path))
            {
                CreateLevel1Index(dir);
            }
        }

        private void AddLevel1(FileLengthHash key)
        {
            List<FileLengthHash> list = null;
            if (!index1.TryGetValue(key, out list))
            {
                list = new List<FileLengthHash>();
                index1[key] = list;
            }
            list.Add(key);
            reverseIndex[key.Path] = key;
        }

        private void AddLevel2(FileLengthHash file)
        {
            var key = file.InnerHash;
            if (key == null)
            {
                key = new FileBlockHash(file.Path);
                file.InnerHash = key;
                List<FileBlockHash> list = null;
                if (!index2.TryGetValue(key, out list))
                {
                    list = new List<FileBlockHash>();
                    index2[key] = list;
                }
                list.Add(key);
            }
        }

        private void AddLevel3(FileBlockHash file)
        {
            var key = file.InnerHash;
            if (key == null)
            {
                key = new EntireFileHash(file.Path);
                file.InnerHash = key;
                List<EntireFileHash> list = null;
                if (!index3.TryGetValue(key, out list))
                {
                    list = new List<EntireFileHash>();
                    index3[key] = list;
                }
                list.Add(key);
            }
        }

        private void CreateLevel2Index(FileLengthHash foreignKey)
        {
            List<FileLengthHash> list;
            if (this.index1.TryGetValue(foreignKey, out list))
            {
                // re-hash these files
                foreach (var info in list)
                {
                    AddLevel2(info);
                }
            }
        }

        private void CreateLevel3Index(FileBlockHash foreignKey)
        {
            List<FileBlockHash> list;
            if (this.index2.TryGetValue(foreignKey, out list))
            {
                // re-hash these files
                foreach (var info in list)
                {
                    AddLevel3(info);
                }
            }
        }

        class Progress
        {
            int amount;
            int pos;
            int percent;

            public Progress(int amount)
            {
                this.amount = amount;
            }

            public void Increment()
            {
                pos++;
                int p = (pos * 100 / amount);
                if (p != percent)
                {
                    Console.Write(".");
                    percent = p;
                }
            }
        }

        private void OptimizeIndex()
        {
            // Now if file length alone is not a good enough hash and we have hash
            // conflicts then hash again using a 64kb block buffer.            
            int conflicts = (from i in index1.Values where i.Count > 1 select i.Count).Sum();
            if (conflicts > 0)
            {
                Console.WriteLine("Rehashing {0} level 2 files", conflicts);
                Progress progress = new Progress(conflicts);
                // now rehashing anything that shows same file size to get less clashes.
                foreach (var pair in index1.ToArray())
                {
                    var list = pair.Value;
                    if (list.Count > 1)
                    {
                        // re-hash these files
                        foreach (var info in list)
                        {
                            AddLevel2(info);
                        }
                        progress.Increment();
                    }
                }
                Console.WriteLine();
            }

            // Now if the level 2 index is also degenerated, we go to level 3
            conflicts = (from i in index2.Values where i.Count > 1 select i.Count).Sum();
            if (conflicts > 1)
            {
                Console.WriteLine("Rehashing {0} level 3 files", conflicts);
                Progress progress = new Progress(conflicts);
                // now rehashing anything that shows same file size to get less clashes.
                foreach (var pair in index2.ToArray())
                {
                    var list = pair.Value;
                    if (list.Count > 1)
                    {
                        // re-hash these files
                        foreach (var info in list)
                        {
                            AddLevel3(info);
                        }
                        progress.Increment();
                    }
                }
            }
        }


        public IEnumerable<List<EntireFileHash>> FindDuplicates()
        {
            List<EntireFileHash> duplicates = new List<EntireFileHash>();

            // Now do a deep compare of any files that have identical hashes to see what is really duplicated or not.
            // Note: if they have identical hashes then they would have made it into index3.
            foreach (var pair in index3)
            {
                var list = new List<EntireFileHash>(pair.Value);
                if (list.Count > 1)
                {
                    List<EntireFileHash> nondups = new List<EntireFileHash>();
                    EntireFileHash first = list[0];
                    bool foundDup = false;
                    for (int i = 1; i < list.Count; i++)
                    {
                        EntireFileHash other = list[i];
                        if (!first.Equals(other))
                        {
                            nondups.Add(other);
                        }
                        else if (first.FileEquals(other))
                        {
                            if (!foundDup)
                            {
                                foundDup = true;
                                duplicates.Add(first);
                            }

                            duplicates.Add(other);
                        }
                        else
                        {
                            nondups.Add(other);
                        }
                    }

                    if (foundDup)
                    {
                        yield return duplicates;
                        duplicates = new List<EntireFileHash>();
                        foundDup = false;
                    }
                    if (nondups.Count > 0)
                    {
                        // now search the remainder for other matches.
                        list = nondups;
                    }
                }
            }
        }

        static Regex re = new Regex("[-_\\(\\)0-9]+$"); // any combination of - ( ) and 0-9.

        /// <summary>
        /// Process the results of FindDuplicates, picking the best master file and deleting the others
        /// </summary>
        /// <param name="name">The name of this index for log messages</param>
        /// <param name="duplicates">Result of FindDuplicates</param>
        /// <param name="options">The current command line merge options</param>
        internal void PickDuplicate(string name, List<EntireFileHash> duplicates, MergeOptions options)
        {
            // heuristic, if file name ends with (1), (2) or "-1", "-2" and so on, then it is probably a copy paste error, so pick the
            // file name that doesn't contain this suffix.
            // Otherwise pick the longest file name because it is probably the most descriptive.

            string longest = null;
            EntireFileHash longestFile = null;
            string shortest = null;
            string shortestActual = null;
            EntireFileHash shortestFile = null;

            foreach (var item in duplicates)
            {
                string baseName = Path.GetFileNameWithoutExtension(item.Path);
                var match = re.Match(baseName);
                if (match.Success && match.Index > 1)
                {
                    baseName = baseName.Substring(0, match.Index);
                }
                if (longest == null || longest.Length < baseName.Length)
                {
                    longest = baseName;
                    longestFile = item;
                }
                if (shortest == null || shortest.Length > baseName.Length)
                {
                    shortest = baseName;
                    shortestFile = item;
                    shortestActual = Path.GetFileNameWithoutExtension(item.Path);
                }
            }
            bool isIndexed = true;
            // now see if all the names are an indexed (1) or -1 extension on the shortest name.
            foreach (var item in duplicates)
            {
                string baseName = Path.GetFileNameWithoutExtension(item.Path);
                if (baseName != shortestActual)
                {
                    string head = baseName.Substring(0, shortest.Length);
                    if (string.Compare(head, shortest, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        string suffix = baseName.Substring(shortest.Length);
                        var match = re.Match(baseName);
                        if (!match.Success)
                        {
                            isIndexed = false;
                        }
                    } 
                    else
                    {
                        isIndexed = false;
                    }
                }
            }

            if (isIndexed)
            {
                Console.WriteLine("Picking " + name + " duplicate: " + shortestFile.Path);
                this.ResolveDuplicate(shortestFile, options);
            }
            else
            {
                Console.WriteLine("Picking " + name + " duplicate: " + longestFile.Path);
                this.ResolveDuplicate(longestFile, options);
            }
        }

        private void ResolveDuplicate(EntireFileHash choice, MergeOptions options)
        {
            List<EntireFileHash> dups = index3[choice];
            foreach (var item in dups.ToArray())
            {
                if (item.Path != choice.Path)
                {
                    dups.Remove(item);
                    if (options.Preview || this.verbose)
                    {
                        Console.WriteLine("    delete " + item.Path);
                    }
                    if (!options.Preview)
                    {
                        SafeDelete(item.Path);
                    }
                    MergeDuplicateMetadata(item.Path, choice.Path, options);
                }
            }
        }

        internal void Merge(FolderIndex sourceIndex, MergeOptions options)
        {
            // When comparing FolderIndexes, we don't know up front which files in this or the other index
            // have been indexed to level 2 or 3, so we have to look at the "InnerHashes" to figure that out.

            foreach (var pair in sourceIndex.index1)
            {
                // multiple items here just means hash clashes, they are actually different files assuming
                // we have already de-duplicated this folder.
                foreach (FileLengthHash lengthHash in pair.Value)
                {
                    string sourceFile = lengthHash.Path;
                    bool isMetadata = string.Compare(Path.GetExtension(sourceFile), ".xmp", StringComparison.OrdinalIgnoreCase) == 0;
                    bool found = false;

                    if (isMetadata)
                    {
                        // skip merging dangling metadata files.  Metadata is merged when a real matching photograph is found 
                        // and at that point we also merge the metadata associated with these source and target photos.
                        continue; 
                    }

                    if (!this.index1.ContainsKey(lengthHash))
                    {
                        // this file does not exist in our index.
                    }
                    else
                    {
                        // There are items that need to be compared. To reduce the N x M set that needs to
                        // be compared, we promote this item to level 2 and level 3 indexes, both in our index
                        // and in the source index, then we can compare only the matching EntireFileHash subset.
                        sourceIndex.AddLevel2(lengthHash);
                        this.CreateLevel2Index(lengthHash);
                        FileBlockHash blockHash = lengthHash.InnerHash;
                        if (!this.index2.ContainsKey(blockHash))
                        {
                            // this file does not exist in our level 2 index.
                        }
                        else
                        {
                            sourceIndex.AddLevel3(blockHash);
                            this.CreateLevel3Index(blockHash);
                            EntireFileHash sourceFileHash = blockHash.InnerHash;
                            if (!this.index3.ContainsKey(sourceFileHash))
                            {
                                // this file does not exist in our level 3 index.
                            }
                            else
                            {
                                // Ok, now we have the minimal N x M file compare using index 3.
                                foreach (var target in this.index3[sourceFileHash].ToArray())
                                {
                                    if (target.Equals(sourceFileHash) && sourceFileHash.FileEquals(target))
                                    {
                                        if (options.Preview | verbose)
                                        {
                                            Console.WriteLine("    delete {0}", sourceFile);
                                        }
                                        TryMergeMetadata(sourceIndex, lengthHash.Path, target.Path, options);
                                        if (found)
                                        {
                                            throw new Exception("bugbug: still have duplicates in the target index???");
                                        }
                                        if (!options.Preview)
                                        {
                                            SafeDelete(sourceFile);
                                        }
                                        found = true;
                                    }
                                }
                            }
                        }
                    }
                    if (!found)
                    {
                        string target = Path.Combine(dir, Path.GetFileName(lengthHash.Path));
                        if (File.Exists(target))
                        {
                            // interesting, same file name but different contents, so some sort of "merge" operation is needed here.
                            // perhaps one photo was slightly edited, so take the "best" one...
                            Console.WriteLine("### Conflicting target file name: {0}", target);
                            Console.WriteLine("       Skipping source file name: {0}", lengthHash.Path);
                        }
                        else
                        {
                            if (options.Preview | verbose)
                            {
                                Console.WriteLine("copy \"{0}\" \"{1}\"", lengthHash.Path, target);
                            }
                            if (!options.Preview)
                            {
                                File.Copy(lengthHash.Path, target);
                                SafeDelete(lengthHash.Path);
                            }
                        }
                        TryMergeMetadata(sourceIndex, lengthHash.Path, target, options);
                    }
                }
            }

        }

        private EntireFileHash Promote(FileLengthHash hash)
        {
            // found it, make sure this specific file is promoted to level 3 so we have
            // a comparable FileLengthHash object.
            AddLevel2(hash);
            AddLevel3(hash.InnerHash);
            return hash.InnerHash.InnerHash;
        }

        private void TryMergeMetadata(FolderIndex sourceIndex, string source, string target, MergeOptions options)
        {
            string sourceMetadata = Path.ChangeExtension(source, ".xmp");
            string targetMetadata = Path.ChangeExtension(target, ".xmp");

            FileLengthHash sm = sourceIndex.FindFile(sourceMetadata);
            FileLengthHash tm = this.FindFile(targetMetadata);

            if (sm != null && tm != null)
            {
                if (sm.HashEquals(tm))
                {
                    EntireFileHash se = sourceIndex.Promote(sm);
                    EntireFileHash te = this.Promote(tm);
                    if (se.Equals(te) && se.FileEquals(te))
                    {
                        // files match!
                        if (options.Preview || verbose)
                        {
                            Console.WriteLine("    delete {0}", sourceMetadata);
                        }

                        if (!options.Preview)
                        {
                            SafeDelete(sourceMetadata);
                        }
                        return;
                    }
                }

                MergeMetadata(sourceMetadata, targetMetadata, options);                
            }
            else if (sm != null)
            {
                if (options.Preview || verbose)
                {
                    Console.WriteLine("    copy \"{0}\" \"{1}\"", sourceMetadata, targetMetadata);
                    Console.WriteLine("    delete {0}", sourceMetadata);
                }
                if (!options.Preview)
                {
                    File.Copy(sourceMetadata, targetMetadata);
                    SafeDelete(sourceMetadata);
                }
            }
        }

        private void MergeDuplicateMetadata(string source, string target, MergeOptions options)
        {
            string sourceMetadata = Path.ChangeExtension(source, ".xmp");
            string targetMetadata = Path.ChangeExtension(target, ".xmp");

            FileLengthHash sm = this.FindFile(sourceMetadata);
            FileLengthHash tm = this.FindFile(targetMetadata);

            if (sm != null && tm != null)
            {
                if (sm.HashEquals(tm))
                {
                    EntireFileHash se = this.Promote(sm);
                    EntireFileHash te = this.Promote(tm);
                    if (se.Equals(te) && se.FileEquals(te))
                    {
                        // files match!
                        if (options.Preview || verbose)
                        {
                            Console.WriteLine("    delete {0}", sourceMetadata);
                        }
                        if (!options.Preview)
                        {
                            SafeDelete(sourceMetadata);
                        }
                        return;
                    }
                }

                MergeMetadata(sourceMetadata, targetMetadata, options);
            }
            else if (sm != null)
            {
                if (options.Preview || verbose)
                {
                    Console.WriteLine("    copy \"{0}\" \"{1}\"", sourceMetadata, targetMetadata);
                    Console.WriteLine("    delete {0}", sourceMetadata);
                }
                if (!options.Preview)
                {
                    File.Copy(sourceMetadata, targetMetadata);
                    SafeDelete(sourceMetadata);
                }
            }
        }

        private void MergeMetadata(string sourceMetadata, string targetMetadata, MergeOptions options)
        {
            Metadata source = Metadata.Load(sourceMetadata);
            Metadata target = Metadata.Load(targetMetadata);
            if (target.IsSame(source))
            {
                if (options.Preview || verbose)
                {
                    Console.WriteLine("    delete {0}", sourceMetadata);
                }
                if (!options.Preview)
                {
                    SafeDelete(sourceMetadata);
                }
            }
            else
            {
                Console.WriteLine("windiff \"{0}\" \"{1}\"", sourceMetadata, targetMetadata);
            }
        }

        private FileLengthHash FindFile(string path)
        {
            if (File.Exists(path))
            {
                FileLengthHash key = null;
                if (this.reverseIndex.TryGetValue(path, out key))
                {
                    return key;
                }
                // perhaps file was added previously by a merge copy.
                key = new FileLengthHash(path);
                this.AddLevel1(key);
            }
            return null;
        }

        private void SafeDelete(string filename)
        {
            var attrs = File.GetAttributes(filename);
            if ((attrs & FileAttributes.ReadOnly) != 0)
            {
                attrs &= ~FileAttributes.ReadOnly;
                File.SetAttributes(filename, attrs);
            }
            File.Delete(filename);
        }
    }
}
