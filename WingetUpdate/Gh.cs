internal sealed class Gh : Fork
{
    public Gh(string workingDirectory = null)
        : base("gh.exe", workingDirectory)
    {
    }
}
