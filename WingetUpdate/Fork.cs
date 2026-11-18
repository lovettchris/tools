using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

internal abstract class Fork
{
    private readonly string executablePath;
    private readonly string workingDirectory;

    protected Fork(string executableName, string workingDirectory = null)
    {
        this.executablePath = FindExecutableOnPath(executableName);
        this.workingDirectory = workingDirectory;
    }

    public string Capture(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = this.executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        if (!string.IsNullOrWhiteSpace(this.workingDirectory))
        {
            startInfo.WorkingDirectory = this.workingDirectory;
        }
        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        using var process = new Process { StartInfo = startInfo };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command '{this.executablePath} {string.Join(" ", arguments)}' failed with exit code {process.ExitCode}. Error: {error}");
        }
        return output.Trim();
    }

    public int Run(int? expectedExitCode, params string[] arguments)
    {
        Console.WriteLine($"Running command {this.executablePath} {string.Join(" ", arguments)}");
        var startInfo = new ProcessStartInfo
        {
            FileName = this.executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (!string.IsNullOrWhiteSpace(this.workingDirectory))
        {
            startInfo.WorkingDirectory = this.workingDirectory;
        }

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                Console.Out.WriteLine(e.Data);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                Console.Error.WriteLine(e.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (expectedExitCode.HasValue && process.ExitCode != expectedExitCode.Value)
        {
            string cmd = $"{this.executablePath} {string.Join(" ", arguments)}";
            throw new InvalidOperationException($"{cmd} expected exit code {expectedExitCode.Value} but got {process.ExitCode}.");
        }

        return process.ExitCode;
    }

    private static string FindExecutableOnPath(string executableName)
    {
        string path = Environment.GetEnvironmentVariable("PATH");
        if (path is not null)
        {
            foreach (string pathEntry in path.Split(Path.PathSeparator))
            {
                string directory = pathEntry.Trim().Trim('"');
                string executablePath = Path.Combine(
                    string.IsNullOrEmpty(directory) ? Environment.CurrentDirectory : directory,
                    executableName);

                if (File.Exists(executablePath))
                {
                    return Path.GetFullPath(executablePath);
                }
            }
        }

        throw new FileNotFoundException($"{executableName} was not found in the PATH environment variable.", executableName);
    }

    public void OpenUrl(string url)
    {
        var dir = Directory.GetCurrentDirectory();
        // todo: support showing embedded pack:// resources in a popup page (could be useful for help content).
        int rc = ShellExecute(IntPtr.Zero, "open", url, null, dir, SW_SHOWNORMAL);
    }

    public const int SW_SHOWNORMAL = 1;

    [DllImport("Shell32.dll", EntryPoint = "ShellExecuteA",
        SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall)]
    public static extern int ShellExecute(IntPtr handle, string verb, string file,
        string args, string dir, int show);

}
