namespace GitMegalodon.Models;

public class Commit
{
    /// základní informace
    public string Message { get; set; }
    public string Description { get; set; }
    public string Hash { get; set; }
    public string ShortHash { get; set; }
    public string Date { get; set; }
    
    /// autor a čas
    public Author Author { get; set; }
    public DateTime CommitTime { get; set; }
    
    /// vztahy a změny
    public List<string> ParentHashes { get; set; }
    public List<string> ChildHashes { get; set; }
    public List<Branch> Branches { get; set; }
    public List<Tag> Tags { get; set; }

    /// Změny
    public List<ChangedFile> ChangedFiles { get; set; }
    public int AddedLines { get; set; }
    public int DeletedLines { get; set; }
    
    /// Grafová reprezentace
    public int XPosition { get; set; }  // Pro vykreslení grafu
    public int YPosition { get; set; }  // Pro vykreslení grafu

}