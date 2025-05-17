using System;
using System.Collections.ObjectModel;
using LibGit2Sharp;

namespace GitMegalodon.Models
{
    public class Repository
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public ObservableCollection<Branch> Branches { get; set; } = new ObservableCollection<Branch>();
        public ObservableCollection<Commit> Commits { get; set; } = new ObservableCollection<Commit>();
        public ObservableCollection<FileChange> StagedChanges { get; set; } = new ObservableCollection<FileChange>();
        public ObservableCollection<FileChange> UnstagedChanges { get; set; } = new ObservableCollection<FileChange>();
        public Branch CurrentBranch { get; set; }

        public Repository(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        }
    }
}