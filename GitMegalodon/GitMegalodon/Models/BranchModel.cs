namespace GitMegalodon.Models
{
    public class Branch
    {
        public string Name { get; set; }
        public bool IsRemote { get; set; }
        public bool IsCurrent { get; set; }
        
        public Branch(string name, bool isRemote, bool isCurrent)
        {
            Name = name;
            IsRemote = isRemote;
            IsCurrent = isCurrent;
        }
        
        public override string ToString()
        {
            return Name;
        }
    }
}