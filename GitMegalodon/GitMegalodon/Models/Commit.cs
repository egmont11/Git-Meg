// Add this to GitMegalodon.Models/Commit.cs

using System.Collections.ObjectModel;

namespace GitMegalodon.Models
{
    public class Commit
    {
        public string Hash { get; }
        public string ShortHash => Hash.Substring(0, Math.Min(Hash.Length, 7));
        public string Author { get; }
        public DateTime Date { get; }
        public string Message { get; }
        public ObservableCollection<FileChange> Changes { get; }

        public Commit(string hash, string author, DateTime date, string message)
        {
            Hash = hash;
            Author = author;
            Date = date;
            Message = message;
            Changes = new ObservableCollection<FileChange>();
        }
    }
    public class CommitNode
    {
        public string Hash { get; set; }
        public string Author { get; set; }
        public DateTime Date { get; set; }
        public string Message { get; set; }
        public string RefNames { get; set; }
        public string GraphStructure { get; set; }
        public List<string> ParentHashes { get; } = new List<string>();
        public List<CommitNode> Parents { get; } = new List<CommitNode>();
        public List<CommitNode> Children { get; } = new List<CommitNode>();
    }
}