internal sealed class Winget : Fork
{
    public Winget(string workingDirectory = null)
        : base("winget.exe", workingDirectory)
    {
    }

    public void UpdateTool(string name)
    {
        this.Run(null, "update", name);
    }
}
