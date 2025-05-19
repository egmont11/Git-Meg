namespace GitMegalodon.Models;

public class Diff
{
    public string FilePath { get; set; }
    public List<DiffHunk> Hunks { get; set; }
    public string OldFilePath { get; set; }  // Pro přejmenované soubory
    public bool IsBinaryFile { get; set; }
}

public class DiffHunk
{
    public int OldStartLine { get; set; }
    public int OldLineCount { get; set; }
    public int NewStartLine { get; set; }
    public int NewLineCount { get; set; }
    public string Header { get; set; }
    public List<DiffLine> Lines { get; set; }
}

public class DiffLine
{
    public DiffLineType Type { get; set; }
    public string Content { get; set; }
    public int OldLineNumber { get; set; }  // -1 pokud není součástí starého souboru
    public int NewLineNumber { get; set; }  // -1 pokud není součástí nového souboru
}

public enum DiffLineType
{
    Context,
    Addition,
    Deletion
}