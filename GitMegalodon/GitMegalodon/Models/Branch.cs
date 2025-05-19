namespace GitMegalodon.Models;

public class Branch
{
    public string Name { get; set; }
    public string FullName { get; set; }
    public string UpstreamBranchName { get; set; }
    public string LatestCommitHash { get; set; }
    public bool IsRemote { get; set; }
    public bool IsHead { get; set; }
    public int AheadCount { get; set; }  // Kolik commitů je před vzdálenou větví
    public int BehindCount { get; set; } // Kolik commitů je za vzdálenou větví
    public string Color { get; set; }    // Pro vizuální odlišení větví v grafu
}