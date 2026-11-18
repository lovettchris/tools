using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace GitTool
{
    internal class Program
    {
        private string wingetTarget;
        private string wingetSource;
        private string organization;
        private string applicationName;
        private Command command;
        private Version publishVersion;

        private enum Command
        {
            None,
            Prune,
            Publish
        }

        static void Main(string[] args)
        {
            var program = new Program();
            if (!program.ParseCommandLine(args))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(program.wingetTarget))
            {
                Console.Error.WriteLine("The --target argument is required and must point to your local clone of your winget-pkgs fork, and the specific product manifest folder you want changed.");
                return;
            }

            program.applicationName = Path.GetFileName(program.wingetTarget);
            program.organization = Path.GetFileName(Path.GetDirectoryName(program.wingetTarget));
            var manifests = program.wingetTarget;
            while (Path.GetFileName(manifests) != "manifests")
            {
                manifests = Path.GetDirectoryName(manifests);
                if (string.IsNullOrEmpty(manifests))
                {
                    Console.WriteLine("Could not find 'manifests' parent folder in this repo");
                    return;
                }
            }
            program.wingetSource = Path.GetDirectoryName(manifests);

            if (Version.TryParse(program.applicationName, out Version version))
            {
                Console.Error.WriteLine($"The --target argument must point to the product manifest folder, not a version folder. The provided path '{program.wingetTarget}' ends with a version folder name.");
                return;
            }

            // if the directory does not exist, print error
            if (!Directory.Exists(program.wingetTarget))
            {
                Console.Error.WriteLine($"The --target directory '{program.wingetTarget}' does not exist.");
                return;
            }

            var saved = Directory.GetCurrentDirectory();

            try
            {
                Directory.SetCurrentDirectory(program.wingetTarget);

                // check if there are any unstaged git changes in this directory, if so, print error and exit
                Git git = new Git();
                try
                {
                    git.Run(0, "diff", "--quiet");
                } 
                catch
                {
                    Console.Error.WriteLine($"The --target directory has unstaged git changes. Please commit or stash them before running this tool.");
                    return;
                }

                // use git command to lookup the name of this local fork
                var gitForkName = git.GetForkName();
                var mainName = git.GetMainBranchName();


                // Sync the local fork with the upstream winget-pkgs repo, so we are working with the latest version of the manifest files.
                Gh gh = new Gh();
                gh.Run(0, "repo", "sync", gitForkName, "--branch", mainName);

                // Checkout "master" branch and pull the latest changes
                git.Run(0, "checkout", mainName);
                git.Run(0, "pull");

                // Now run the desired command.
                switch (program.command)
                {
                    case Command.Prune:
                        program.Prune();
                        break;
                    case Command.Publish:
                        program.Publish(program.publishVersion!);
                        break;
                }
            } 
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                Directory.SetCurrentDirectory(saved);
            }
        }

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--target":
                        if (++i >= args.Length)
                        {
                            PrintUsage();
                            return false;
                        }

                        this.wingetTarget = args[i];
                        break;
                    case "--prune":
                        this.command = Command.Prune;
                        break;
                    case "--publish":
                        if (++i >= args.Length || !Version.TryParse(args[i], out Version version))
                        {
                            PrintUsage();
                            return false;
                        }

                        this.command = Command.Publish;
                        this.publishVersion = version;
                        break;
                    case "--help":
                        PrintUsage();
                        return false;
                    default:
                        PrintUsage();
                        return false;
                }
            }

            return true;
        }

        private void Prune()
        {
            // Find the oldest version folder name in the wingetTarget directory
            // and if there is more than one version folder, delete the oldest one.
            var versionFolders = Directory.GetDirectories(".")
                .Where(dir => Version.TryParse(Path.GetFileName(dir), out _))
                .Select(dir => new { Path = Path.GetFileName(dir), Version = Version.Parse(Path.GetFileName(dir)) })
                .OrderBy(v => v.Version)
                .ToList();

            string toRemove = null;

            if (versionFolders.Count > 1)
            {
                toRemove = versionFolders.First().Path;
                Console.WriteLine($"Pruning oldest version folder: {toRemove}");
                Directory.Delete(versionFolders.First().Path, recursive: true);
            }
            else
            {
                Console.WriteLine("Nothing to prune");
                return;
            }

            // Now turn this change into a commit and push it to the remote repository as a new branch, and then create a pull request for it.
            var git = new Git();
            var userName = git.GetUserName();
            var branchName = $"{userName}/{this.applicationName}_remove_{toRemove}";
            var comment = $"Remove {this.applicationName} version {toRemove}";
            git.CreateBranchForFolder(toRemove, branchName, comment);

            // Now create pull request from this branch.
            git.CreatePullRequest();
        }

        private void Publish(Version version)
        {
            var versionFolder = Path.Combine(this.wingetTarget, version.ToString());
            if (Directory.Exists(versionFolder))
            {
                Console.WriteLine($"Version {version} folder already exists.");
                return;
            }

            // find previous version and copy the manifest files from that version folder to the new version folder.
            var versionFolders = Directory.GetDirectories(".")
                .Where(dir => Version.TryParse(Path.GetFileName(dir), out _))
                .Select(dir => new { Path = Path.GetFileName(dir), Version = Version.Parse(Path.GetFileName(dir)) })
                .OrderBy(v => v.Version)
                .ToList();

            string toCopy = null;
            Version previousVersion = null;
            List<string> manifests = new List<string>();

            if (versionFolders.Count > 1)
            {
                toCopy = versionFolders.Last().Path;
                previousVersion = versionFolders.Last().Version;
                Console.WriteLine($"Copying manifest files from version folder: {toCopy} to new version folder: {version}");
                Directory.CreateDirectory(versionFolder);
                foreach (var file in Directory.GetFiles(toCopy))
                {
                    var destFile = Path.Combine(versionFolder, Path.GetFileName(file));
                    File.Copy(file, destFile);
                    manifests.Add(destFile);
                }
            }
            else
            {
                Console.WriteLine("Did not find any previous versions, so we have no templates for the manifests.");
                return;
            }

            Directory.SetCurrentDirectory(versionFolder);

            var winget = new Winget();
            winget.UpdateTool("wingetcreate");

            var wingetCreate = new WingetCreate();

            // find the *.Installer.yaml file in the manifests list
            var installerManifest = manifests.FirstOrDefault(m => m.EndsWith(".Installer.yaml", StringComparison.OrdinalIgnoreCase));
            if (installerManifest is null)
            {
                Console.WriteLine("Could not find an installer manifest file in the previous version folder.");
                return;
            }

            var content = File.ReadAllText(installerManifest);

            var installerUrls = new List<string>();
            // extract the lines starting with "InstallerUrls: from the installer manifest file, and compute new versions for wingetcreate update command.
            foreach (var line in content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("InstallerUrl:", StringComparison.OrdinalIgnoreCase))
                {
                    var url = trimmed.Substring(trimmed.IndexOf(':') + 1).Trim();
                    url = url.Replace(previousVersion.ToString(), version.ToString());
                    if (this.ValidateUrl(url))
                    {
                        Console.WriteLine($"Found InstallerUrl: {url}");
                    }
                    else
                    {
                        Console.WriteLine($"Invalid InstallerUrl: {url}");
                        return;
                    }
                    installerUrls.Add(url);
                }
            }

            if (installerUrls.Count == 0)
            {
                Console.WriteLine("Could not find any InstallerUrl strings in the manifest"); ;
                return;
            }

            // Now run the wingetcreate update command with the new version and new urls.
            var args = new List<string>(new[] {
                        "update",
                        this.organization + "." + this.applicationName,
                        "--version", version.ToString(),
                        "-o", this.wingetSource,
                        "-u"});
            args.AddRange(installerUrls);
            wingetCreate.Run(0, args.ToArray());

            // Validate the new manifests.
            wingetCreate.Run(0, "validate", ".");

            // Check we can install it.
            wingetCreate.Run(0, "install", "-m", ".");

            // Now turn this change into a commit and push it to the remote repository as a new branch, and then create a pull request for it.
            var git = new Git();
            var userName = git.GetUserName();
            var branchName = $"{userName}/{this.applicationName}_{version}";
            var comment = $"Add {this.applicationName} version {version}";
            git.CreateBranchForFolder(version.ToString(), branchName, comment);

            // Now create pull request from this branch.
            git.CreatePullRequest();
        }

        private bool ValidateUrl(string url)
        {
            // see if we can get httphead on this url
            var client = new HttpClient();
            try
            {
                var response = client.Send(new HttpRequestMessage(HttpMethod.Head, url));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private void PrintUsage()
        {
            Console.WriteLine("Usage: GitTool --target <path> [--prune] [--publish <version>]");
        }
    }
}
