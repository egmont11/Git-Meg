namespace GitMegalodon.Models;

public class Stash
{
    public int Index { get; set; }
    public string Name { get; set; }
    public string Message { get; set; }
    public string BranchName { get; set; }
    public Author Author { get; set; }
    public DateTime Date { get; set; }
    public List<ChangedFile> Changes { get; set; }
}