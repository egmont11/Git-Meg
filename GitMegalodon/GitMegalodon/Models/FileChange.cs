namespace GitMegalodon.Models
{
    public enum ChangeType
    {
        Added,
        Modified,
        Deleted,
        Renamed,
        Untracked
    }
    
    public class FileChange
    {
        public string Path { get; set; }
        public ChangeType Type { get; set; }
        
        public FileChange(string path, ChangeType type)
        {
            Path = path;
            Type = type;
        }
        
        public override string ToString()
        {
            return $"{GetChangeSymbol()} {Path}";
        }
        
        private string GetChangeSymbol()
        {
            return Type switch
            {
                ChangeType.Added => "A",
                ChangeType.Modified => "M",
                ChangeType.Deleted => "D",
                ChangeType.Renamed => "R",
                ChangeType.Untracked => "?",
                _ => " "
            };
        }
    }
}