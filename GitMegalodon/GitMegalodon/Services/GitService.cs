using System.IO;
using LibGit2Sharp;
using GitMegalodon.Models;

namespace GitMegalodon.Services;

public class GitService
{
    private string _currentRepositoryPath;
    private string _currentBranchName;

    private string _gitUsername;
    private string _gitEmail;
    
    private List<Models.Repository> _repositories;
    
    public GitService()
    {
        
    }

    public Models.Repository? OpenRepository(string path)
    {
        if (Directory.Exists(path))
        {
            var repo = LoadRepository(path);
            return repo;
        }

        return null;
    }
    
    public Models.Repository LoadRepository(string path)
    {
 
        using (var repo = new LibGit2Sharp.Repository(path))
        {
            var repository = new Models.Repository
            {
                Name = Path.GetFileName(path),
                Path = path,
                CurrentBranch = repo.Head.FriendlyName,
                IsLocal = true,
                LastAccessed = DateTime.Now
            };
            
            // Načtení větví
            repository.Branches = repo.Branches
                .Where(b => !b.IsRemote)
                .Select(b => new Models.Branch
                {
                    Name = b.FriendlyName,
                    FullName = b.CanonicalName,
                    IsHead = b.IsCurrentRepositoryHead,
                    LatestCommitHash = b.Tip?.Sha
                }).ToList();
                
            // Načtení vzdálených větví
            repository.RemoteBranches = repo.Branches
                .Where(b => b.IsRemote)
                .Select(b => new Models.RemoteBranch
                {
                    Name = b.FriendlyName,
                    FullName = b.CanonicalName,
                    RemoteName = b.RemoteName,
                    LatestCommitHash = b.Tip?.Sha
                }).ToList();
                
            // Načtení tagů
            repository.Tags = repo.Tags
                .Select(t => new Models.Tag
                {
                    Name = t.FriendlyName,
                    CommitHash = t.Target.Id.Sha
                }).ToList();
                
            // Kontrola stavu repozitáře (změny)
            repository.StagedChanges = new List<Models.ChangedFile>();
            repository.UnstagedChanges = new List<Models.ChangedFile>();
            repository.UntrackedFiles = new List<string>();
            
            foreach (var item in repo.RetrieveStatus())
            {
                if (item.State.HasFlag(FileStatus.NewInIndex) || 
                    item.State.HasFlag(FileStatus.ModifiedInIndex) || 
                    item.State.HasFlag(FileStatus.DeletedFromIndex) || 
                    item.State.HasFlag(FileStatus.RenamedInIndex) || 
                    item.State.HasFlag(FileStatus.TypeChangeInIndex))
                {
                    repository.StagedChanges.Add(new Models.ChangedFile
                    {
                        Path = item.FilePath,
                        FileName = Path.GetFileName(item.FilePath),
                        Extension = Path.GetExtension(item.FilePath),
                        Status = ConvertFileStatus(item.State)
                    });
                }
                else if (item.State.HasFlag(FileStatus.ModifiedInWorkdir) || 
                         item.State.HasFlag(FileStatus.DeletedFromWorkdir) || 
                         item.State.HasFlag(FileStatus.RenamedInWorkdir) || 
                         item.State.HasFlag(FileStatus.TypeChangeInWorkdir))
                {
                    repository.UnstagedChanges.Add(new Models.ChangedFile
                    {
                        Path = item.FilePath,
                        FileName = Path.GetFileName(item.FilePath),
                        Extension = Path.GetExtension(item.FilePath),
                        Status = ConvertFileStatus(item.State)
                    });
                }
                else if (item.State.HasFlag(FileStatus.NewInWorkdir))
                {
                    repository.UntrackedFiles.Add(item.FilePath);
                }
            }
            
            // Načtení vzdálených repozitářů
            var remotes = repo.Network.Remotes.Select(r => new Models.RemoteRepository
            {
                Name = r.Name,
                Url = r.Url,
                FetchRefSpecs = r.FetchRefSpecs.Select(rs => rs.Specification).ToList(),
                PushRefSpecs = r.PushRefSpecs.Select(rs => rs.Specification).ToList()
            }).ToList();
            
            return repository;
        }
    }

    private Models.ChangeType ConvertFileStatus(FileStatus status)
    {
        if (status.HasFlag(FileStatus.NewInIndex) || status.HasFlag(FileStatus.NewInWorkdir))
            return Models.ChangeType.Added;
        if (status.HasFlag(FileStatus.ModifiedInIndex) || status.HasFlag(FileStatus.ModifiedInWorkdir))
            return Models.ChangeType.Modified;
        if (status.HasFlag(FileStatus.DeletedFromIndex) || status.HasFlag(FileStatus.DeletedFromWorkdir))
            return Models.ChangeType.Deleted;
        if (status.HasFlag(FileStatus.RenamedInIndex) || status.HasFlag(FileStatus.RenamedInWorkdir))
            return Models.ChangeType.Renamed;
        if (status.HasFlag(FileStatus.Conflicted))
            return Models.ChangeType.Conflicted;
            
        return Models.ChangeType.Untracked;
    }
}