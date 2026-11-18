
using System;
using System.Text.RegularExpressions;

internal sealed class Git : Fork
{
    public Git(string workingDirectory = null)
        : base("git.exe", workingDirectory)
    {
    }

    const string GitHubUrlPrefix = "git@github.com:";

    internal string GetForkName()
    {
        var url = this.Capture("remote", "get-url", "origin");
        if (url.StartsWith("https://"))
        {
            // e.g. https://github.com/lovettchris/winget-pkgs.git
            var uri = new Uri(url);
            url = uri.AbsolutePath;
        }
        else if (url.StartsWith(GitHubUrlPrefix))
        {
            url = url.Substring(GitHubUrlPrefix.Length);
        }
        else
        {
            throw new Exception("Unexpected git remote URL format: " + url);
        }

        if (url.EndsWith(".git"))
        {
            url = url.Substring(0, url.Length - 4);
        }
        return url;
    }

    internal string GetMainBranchName()
    {
        var mainBranch = this.Capture("remote", "show", "origin");
        var match = Regex.Match(mainBranch, @"HEAD branch: (.+)");
        if (!match.Success)
        {
            throw new Exception("Could not determine main branch name from git remote show origin output.");
        }
        return match.Groups[1].Value;
    }

    internal void CreateBranchForFolder(string folder, string branchName, string comment)
    {
        this.Run(0, "checkout", "-b", branchName);

        // stage the changes
        this.Run(0, "add", folder);

        // commit this change
        this.Run(0, "commit", "-a", "-m", comment);

        // push this branch to remote 
        this.Run(0, "push", "-u", "origin", branchName);
    }

    internal void CreatePullRequest()
    {
        // Now create pull request from this branch.
        var gitForkName = this.GetForkName();
        var url = $"https://github.com/{gitForkName}";
        Console.WriteLine($">>> Please create pull request on {url} ");
        Console.WriteLine("Opening web browser to the right place...");
        this.OpenUrl(url);
    }

    internal string GetUserName()
    {
        var userName = this.Capture("config", "user.email");
        return userName.Split("@")[0];
    }
}
