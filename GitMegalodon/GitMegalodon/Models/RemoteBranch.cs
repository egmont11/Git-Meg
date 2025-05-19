namespace GitMegalodon.Models;

public class RemoteBranch
{
    public string Name { get; set; }
    public string FullName { get; set; }
    public string RemoteName { get; set; }
    public string LatestCommitHash { get; set; }
}