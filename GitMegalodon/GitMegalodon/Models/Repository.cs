namespace GitMegalodon.Models
{
    public class Repository
    {
        // Základní informace
        public string Name { get; set; }
        public string Path { get; set; }
        public string RemoteUrl { get; set; }
        public bool IsLocal { get; set; }
        public DateTime LastAccessed { get; set; }
    
        // Aktuální stav
        public string CurrentBranch { get; set; }
        public List<Branch> Branches { get; set; }
        public List<RemoteBranch> RemoteBranches { get; set; }
        public List<Tag> Tags { get; set; }
    
        // Statistiky
        public int TotalCommits { get; set; }
        public int TotalContributors { get; set; }
        public DateTime FirstCommitDate { get; set; }
        public DateTime LastCommitDate { get; set; }
    
        // Změny
        public List<ChangedFile> StagedChanges { get; set; }
        public List<ChangedFile> UnstagedChanges { get; set; }
        public List<string> UntrackedFiles { get; set; }
    
        // Stash
        public List<Stash> Stashes { get; set; }
    }
}