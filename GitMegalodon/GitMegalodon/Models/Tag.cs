namespace GitMegalodon.Models;

public class Tag
{
    public string Name { get; set; }
    public string CommitHash { get; set; }
    public bool IsAnnotated { get; set; }
    public string Message { get; set; }  // Pouze pro anotované tagy
    public Author Tagger { get; set; }   // Pouze pro anotované tagy
    public DateTime Date { get; set; }   // Pouze pro anotované tagy
}