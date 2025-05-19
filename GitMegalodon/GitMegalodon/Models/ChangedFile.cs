namespace GitMegalodon.Models;

public class ChangedFile
{
    public string Path { get; set; }
    public string FileName { get; set; }
    public string Extension { get; set; }
    public ChangeType Status { get; set; }  // Added, Modified, Deleted, Renamed, Copied
    public int AddedLines { get; set; }
    public int DeletedLines { get; set; }
    public string OldPath { get; set; }  // Pro přejmenované soubory
    public int ModificationPercentage { get; set; }  // % změn
    public bool IsConflicted { get; set; }
    public string BinaryStatus { get; set; }  // Pro binární soubory
}

public enum ChangeType
{
    Added,
    Modified,
    Deleted,
    Renamed,
    Copied,
    Untracked,
    Conflicted
}