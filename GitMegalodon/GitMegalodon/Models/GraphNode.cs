namespace GitMegalodon.Models;

public class GraphNode
{
    public Commit Commit { get; set; }
    public List<Branch> Branches { get; set; }
    public List<Tag> Tags { get; set; }
    public int RowIndex { get; set; }
    public int ColumnIndex { get; set; }
    public List<GraphConnection> Connections { get; set; }
}

public class GraphConnection
{
    public int StartRow { get; set; }
    public int StartColumn { get; set; }
    public int EndRow { get; set; }
    public int EndColumn { get; set; }
    public string Color { get; set; }
}