﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MergePhotos
{
    class Program
    {
        bool verbose;
        string source;
        string target;
        MergeOptions options = new MergeOptions() { Preview = true };
        FolderIndex sourceIndex;
        FolderIndex targetIndex;

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: MergePhotos [options] source_dir [target_dir]");
            Console.WriteLine("Find duplicates in given source dir and delete them.  If target dir is also provided then");
            Console.WriteLine("it merges source into target.  It also correctly merges *.xmp metadata files.");
            Console.WriteLine("Options:");
            Console.WriteLine("    -v    verbose output");
            Console.WriteLine("    -f    actually do it!");
        }

        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }
            p.Run();
        }

        bool ParseCommandLine(string[] args)
        {

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "h":
                        case "?":
                        case "help":
                            return false;
                        case "v":
                        case "verbose":
                            verbose = true;
                            break;
                        case "f":
                            options.Preview = false;
                            break;
                        default:
                            WriteError("Unexpected argument: " + arg);
                            return false;
                    }
                }
                else 
                {
                    try
                    {
                        var path = Path.GetFullPath(arg);
                        if (!Directory.Exists(path))
                        {
                            WriteError("Directory not found: " + path);
                            return false;
                        }
                        if (source == null)
                        {
                            source = path;
                        }
                        else if (target == null)
                        {
                            target = path;
                        }
                        else
                        {
                            WriteError("Too many directories provided");
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteError("Error with directory: {0}\n{1}", arg, ex.Message);
                        return false;
                    }

                }
            }

            if (source == null )
            {
                WriteError("Please provide source folder");
                return false;
            }


            return true;
        }

        private static void WriteError(string format, params string[] args)
        {
            var saved = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(format, args);
            } 
            finally
            {
                Console.ForegroundColor = saved;
            }
        }

        void Run()
        {
            Stopwatch watch = new Stopwatch();

            sourceIndex = new FolderIndex(source, verbose);
            if (target != null)
            {
                targetIndex = new FolderIndex(target, verbose);
            }

            watch.Start();
            if (targetIndex != null)
            {
                foreach (var dups in targetIndex.FindDuplicates())
                {
                    targetIndex.PickDuplicate("target", dups, options);
                    Console.WriteLine();
                }
            }

            foreach(var dups in sourceIndex.FindDuplicates())
            {
                sourceIndex.PickDuplicate("source", dups, options);
                Console.WriteLine();
            }

            watch.Stop();

            Console.WriteLine("Checked duplicates in {0:N3} seconds", (double)watch.ElapsedMilliseconds / 1000.0);

            if (targetIndex != null)
            {
                watch.Reset();
                watch.Start();

                targetIndex.Merge(sourceIndex, options);

                Console.WriteLine("Merging folders in {0:N3} seconds", (double)watch.ElapsedMilliseconds / 1000.0);
            }

            Console.WriteLine();
        }


    }
}
