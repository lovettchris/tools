internal sealed class WingetCreate : Fork
{
    public WingetCreate(string workingDirectory = null)
        : base("wingetcreate.exe", workingDirectory)
    {
    }
}
