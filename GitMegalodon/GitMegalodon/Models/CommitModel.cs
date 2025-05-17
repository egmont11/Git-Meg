// Add this to GitMegalodon.Models/Commit.cs

using System.Collections.ObjectModel;

namespace GitMegalodon.Models
{
    public class Commit
    {
        public string Hash { get; }
        public string Author { get; }
        public DateTime Date { get; } // This property is missing or not accessible
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
}