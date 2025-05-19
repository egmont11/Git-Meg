namespace GitMegalodon.Models;

public class SubModule
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Url { get; set; }
    public string CommitHash { get; set; }
    public bool IsInitialized { get; set; }
}