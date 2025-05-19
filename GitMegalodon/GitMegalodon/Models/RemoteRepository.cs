namespace GitMegalodon.Models;

public class RemoteRepository
{
    public string Name { get; set; }
    public string Url { get; set; }
    public bool IsPushUrl { get; set; }
    public List<string> FetchRefSpecs { get; set; }
    public List<string> PushRefSpecs { get; set; }
}