using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using GitMegalodon.Models;
using Branch = GitMegalodon.Models.Branch;
using Commit = GitMegalodon.Models.Commit;
using Repository = GitMegalodon.Models.Repository;
using Path = System.IO.Path;
using CommitNode = GitMegalodon.Models.CommitNode;

namespace GitMegalodon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> RecentRepositories { get; set; }
        private Repository CurrentRepository { get; set; }
        private AppSettings Settings { get; set; }

        // Predefined colors for branches
        private static readonly Brush[] _branchColors = new Brush[]
        {
            Brushes.DodgerBlue,
            Brushes.OrangeRed,
            Brushes.MediumSeaGreen,
            Brushes.Purple,
            Brushes.Gold,
            Brushes.HotPink,
            Brushes.Teal,
            Brushes.Firebrick
        };

        public MainWindow()
        {
            InitializeComponent();
            RecentRepositories = new ObservableCollection<string>();
            DataContext = this;

            // Load settings
            Settings = AppSettings.Load();
            LoadRecentRepositories();
        }

        private void LoadRecentRepositories()
        {
            RecentRepositories.Clear();
            foreach (var repo in Settings.RecentRepositories)
            {
                if (!string.IsNullOrEmpty(repo) && Directory.Exists(repo))
                {
                    RecentRepositories.Add(repo);
                }
            }
        }

        private void SaveRecentRepositories()
        {
            Settings.RecentRepositories = RecentRepositories.ToList();
            Settings.Save();
        }

        private async void OpenRepository_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                Title = "Select a Git repository folder",
                IsFolderPicker = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (IsGitRepository(dialog.FileName))
                {
                    await LoadRepositoryAsync(dialog.FileName);
                }
                else
                {
                    MessageBox.Show("The selected folder is not a Git repository.",
                        "Invalid Repository", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloneRepository_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement repository cloning
            MessageBox.Show("Repository cloning is not yet implemented.", "Not Implemented",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CreateRepository_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement repository creation
            MessageBox.Show("Repository creation is not yet implemented.", "Not Implemented",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void RecentReposList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecentReposList.SelectedItem is string path)
            {
                await LoadRepositoryAsync(path);
            }
        }

        private bool IsGitRepository(string path)
        {
            // Check if the folder contains a .git directory
            return Directory.Exists(Path.Combine(path, ".git"));
        }

        private async Task LoadRepositoryAsync(string repositoryPath)
        {
            try
            {
                // Show loading indicator or status
                Mouse.OverrideCursor = Cursors.Wait;

                // Validate repository path before processing
                if (!Directory.Exists(repositoryPath))
                {
                    MessageBox.Show($"Repository path does not exist: {repositoryPath}",
                        "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (!IsGitRepository(repositoryPath))
                {
                    MessageBox.Show($"Not a valid Git repository: {repositoryPath}",
                        "Invalid Repository", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Create repository model
                CurrentRepository = new Repository(repositoryPath);

                // Add to recent repositories if not already there
                if (!RecentRepositories.Contains(repositoryPath))
                {
                    RecentRepositories.Add(repositoryPath);
                    SaveRecentRepositories();
                }

                // Load repository info
                await LoadBranchesAsync();
                await LoadCommitHistoryAsync();
                await LoadChangesAsync();

                // Update window title
                Title = $"GitMegalodon - {CurrentRepository.Name}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading repository: {ex.Message}\n\nStack trace: {ex.StackTrace}",
                    "Repository Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async Task LoadBranchesAsync()
        {
            if (CurrentRepository == null) return;

            BranchComboBox.Items.Clear();
            CurrentRepository.Branches.Clear();

            // Get local branches
            string localBranchesOutput = await RunGitCommandAsync("branch");

            foreach (var line in localBranchesOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmedLine = line.Trim();
                bool isCurrent = trimmedLine.StartsWith("*");
                string branchName = isCurrent ? trimmedLine.Substring(1).Trim() : trimmedLine;

                var branch = new Branch(branchName, false, isCurrent);
                CurrentRepository.Branches.Add(branch);
                BranchComboBox.Items.Add(branch);

                if (isCurrent)
                {
                    CurrentRepository.CurrentBranch = branch;
                    BranchComboBox.SelectedItem = branch;
                }
            }

            // Get remote branches
            string remoteBranchesOutput = await RunGitCommandAsync("branch -r");

            foreach (var line in remoteBranchesOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string branchName = line.Trim();
                if (!string.IsNullOrWhiteSpace(branchName))
                {
                    var branch = new Branch(branchName, true, false);
                    CurrentRepository.Branches.Add(branch);
                    // Don't add remote branches to the combobox for now
                }
            }
        }

        private async Task LoadCommitHistoryAsync()
        {
            if (CurrentRepository == null) return;

            CommitsList.Items.Clear();
            CurrentRepository.Commits.Clear();

            // Get commit history
            string output = await RunGitCommandAsync("log --pretty=format:\"%H|%an|%ad|%s\" --date=iso -n 50");

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parts = line.Split('|');
                if (parts.Length >= 4)
                {
                    string hash = parts[0];
                    string author = parts[1];
                    DateTime date = DateTime.Parse(parts[2]);
                    string message = parts[3];

                    var commit = new Commit(hash, author, date, message);
                    CurrentRepository.Commits.Add(commit);
                    CommitsList.Items.Add(commit);
                }
            }
        }

        private async Task LoadChangesAsync()
        {
            if (CurrentRepository == null) return;

            StagedChangesList.Items.Clear();
            UnstagedChangesList.Items.Clear();
            CurrentRepository.StagedChanges.Clear();
            CurrentRepository.UnstagedChanges.Clear();

            // Get staged changes
            string stagedOutput = await RunGitCommandAsync("diff --name-status --staged");
            ParseChanges(stagedOutput, CurrentRepository.StagedChanges);

            // Get unstaged changes
            string unstagedOutput = await RunGitCommandAsync("diff --name-status");
            ParseChanges(unstagedOutput, CurrentRepository.UnstagedChanges);

            // Get untracked files
            string untrackedOutput = await RunGitCommandAsync("ls-files --others --exclude-standard");
            foreach (var line in untrackedOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var change = new FileChange(line.Trim(), ChangeType.Untracked);
                    CurrentRepository.UnstagedChanges.Add(change);
                    UnstagedChangesList.Items.Add(change);
                }
            }

            // Update UI
            foreach (var change in CurrentRepository.StagedChanges)
            {
                StagedChangesList.Items.Add(change);
            }

            foreach (var change in CurrentRepository.UnstagedChanges)
            {
                UnstagedChangesList.Items.Add(change);
            }
        }

        private void ParseChanges(string output, ObservableCollection<FileChange> changes)
        {
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                char status = line[0];
                string path = line.Substring(1).Trim();

                ChangeType changeType = status switch
                {
                    'A' => ChangeType.Added,
                    'M' => ChangeType.Modified,
                    'D' => ChangeType.Deleted,
                    'R' => ChangeType.Renamed,
                    _ => ChangeType.Modified
                };

                var change = new FileChange(path, changeType);
                changes.Add(change);
            }
        }

        private async Task<string> RunGitCommandAsync(string arguments)
        {
            if (CurrentRepository == null)
                return string.Empty;

            // Create process to run git command
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = CurrentRepository.Path
                }
            };

            try
            {
                process.Start();
                // Read the output
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    string error = await process.StandardError.ReadToEndAsync();
                    throw new Exception($"Git Error (exit code {process.ExitCode}): {error}");
                }

                return output;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing Git command: {ex.Message}",
                    "Git Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }

        private async void CommitsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CommitsList.SelectedItem is Commit commit)
            {
                CommitTitle.Text = commit.Message;
                CommitAuthor.Text = $"Author: {commit.Author}";
                CommitDate.Text = $"Date: {commit.Date:yyyy-MM-dd HH:mm:ss}";
                CommitMessageText.Text = commit.Message;

                // Load files for this commit
                await LoadCommitFilesAsync(commit);
            }
        }

        private async Task LoadCommitFilesAsync(Commit commit)
        {
            CommitFilesList.Items.Clear();
            commit.Changes.Clear();

            string output = await RunGitCommandAsync($"show --name-status {commit.Hash}");

            // Parse output to get file changes
            var fileLines = output.Split('\n').Skip(4).ToList(); // Skip commit info lines
            bool inFileSection = false;

            foreach (var line in fileLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (Regex.IsMatch(line, @"^[AMDRT]\s"))
                {
                    inFileSection = true;
                    string[] parts = line.Trim().Split(new[] { ' ', '\t' }, 2);
                    if (parts.Length >= 2)
                    {
                        char status = parts[0][0];
                        string path = parts[1];

                        ChangeType changeType = status switch
                        {
                            'A' => ChangeType.Added,
                            'M' => ChangeType.Modified,
                            'D' => ChangeType.Deleted,
                            'R' => ChangeType.Renamed,
                            _ => ChangeType.Modified
                        };

                        var change = new FileChange(path, changeType);
                        commit.Changes.Add(change);
                        CommitFilesList.Items.Add(change);
                    }
                }
                else if (inFileSection)
                {
                    // We've moved past the file section
                    break;
                }
            }
        }

        private async void BranchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BranchComboBox.SelectedItem is Branch branch)
            {
                // If the selected branch is not the current branch, switch to it
                if (!branch.IsCurrent)
                {
                    bool result = await SwitchBranchAsync(branch.Name);
                    if (result)
                    {
                        await LoadCommitHistoryAsync();
                        await LoadChangesAsync();
                    }
                }
            }
        }

        private async Task<bool> SwitchBranchAsync(string branchName)
        {
            try
            {
                // Check if there are uncommitted changes
                string statusOutput = await RunGitCommandAsync("status --porcelain");
                if (!string.IsNullOrWhiteSpace(statusOutput))
                {
                    var result = MessageBox.Show(
                        "You have uncommitted changes. Do you want to stash them before switching branches?",
                        "Uncommitted Changes",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        await RunGitCommandAsync("stash save \"Auto-stash before switching branches\"");
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return false;
                    }
                }

                // Switch branch
                await RunGitCommandAsync($"checkout {branchName}");

                // Reload branches
                await LoadBranchesAsync();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error switching branch: {ex.Message}",
                    "Branch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async void Pull_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                string output = await RunGitCommandAsync("pull");
                await LoadCommitHistoryAsync();
                await LoadChangesAsync();
                MessageBox.Show($"Pull completed successfully.\n\n{output}", "Pull Successful", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during pull: {ex.Message}", "Pull Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void Push_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                string output = await RunGitCommandAsync("push");
                MessageBox.Show($"Push completed successfully.\n\n{output}", "Push Successful", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during push: {ex.Message}", "Push Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void Fetch_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                await RunGitCommandAsync("fetch");
                await LoadBranchesAsync();
                MessageBox.Show("Fetch completed successfully.", "Fetch Successful", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during fetch: {ex.Message}", "Fetch Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void StageAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RunGitCommandAsync("add -A");
                await LoadChangesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error staging files: {ex.Message}", "Stage Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void UnstageAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RunGitCommandAsync("reset");
                await LoadChangesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error unstaging files: {ex.Message}", "Unstage Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void RefreshChanges_Click(object sender, RoutedEventArgs e)
        {
            await LoadChangesAsync();
        }

        private async void StagedChangesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StagedChangesList.SelectedItem is FileChange change)
            {
                // Show diff for selected file
                string diff = await RunGitCommandAsync($"diff --staged -- \"{change.Path}\"");
                DiffView.Text = diff;
            }
        }

        private async void UnstagedChangesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UnstagedChangesList.SelectedItem is FileChange change)
            {
                // Show diff for selected file
                if (change.Type == ChangeType.Untracked)
                {
                    // For untracked files, show the file content if it's text
                    try
                    {
                        string filePath = Path.Combine(CurrentRepository.Path, change.Path);
                        if (File.Exists(filePath))
                        {
                            // Simple check to see if it's a binary file
                            if (IsBinaryFile(filePath))
                            {
                                DiffView.Text = "[Binary file not shown]";
                            }
                            else
                            {
                                DiffView.Text = File.ReadAllText(filePath);
                            }
                        }
                        else
                        {
                            DiffView.Text = "[File not found]";
                        }
                    }
                    catch
                    {
                        DiffView.Text = "[Error reading file]";
                    }
                }
                else
                {
                    // For modified files, show the diff
                    string diff = await RunGitCommandAsync($"diff -- \"{change.Path}\"");
                    DiffView.Text = diff;
                }
            }
        }

        private bool IsBinaryFile(string filePath)
        {
            // A simple check - read the first 8KB and check for null bytes
            try
            {
                byte[] buffer = new byte[8192];
                using (FileStream fs = File.OpenRead(filePath))
                {
                    int bytesRead = fs.Read(buffer, 0, buffer.Length);
                    for (int i = 0; i < bytesRead; i++)
                    {
                        if (buffer[i] == 0) return true;
                    }
                }

                return false;
            }
            catch
            {
                return true; // Assume binary if we can't read it
            }
        }

        private async void Commit_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentRepository == null) return;

            // Check if there are staged changes
            if (StagedChangesList.Items.Count == 0)
            {
                MessageBox.Show("No changes staged for commit. Stage some changes first.",
                    "No Staged Changes", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get commit message
            string message = CommitMessageInput.Text;
            if (string.IsNullOrWhiteSpace(message) || message == "Enter commit message...")
            {
                MessageBox.Show("Please enter a commit message.",
                    "Missing Commit Message", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Create commit
                await RunGitCommandAsync($"commit -m \"{message}\"");

                // Clear commit message
                CommitMessageInput.Text = "Enter commit message...";

                // Refresh changes and commit history
                await LoadChangesAsync();
                await LoadCommitHistoryAsync();

                MessageBox.Show("Commit created successfully.", "Commit Successful",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating commit: {ex.Message}",
                    "Commit Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private List<CommitNode> ParseGitLogGraphOutput(string output)
        {
            var commits = new List<CommitNode>();
            var commitDict = new Dictionary<string, CommitNode>();
            var lines = output.Split('\n');
            
            foreach (var line in lines)
            {
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                // Extract the graph structure part (asterisks and pipes)
                int commitIndex = line.IndexOf("commit ", StringComparison.OrdinalIgnoreCase);
                if (commitIndex < 0) continue;
                
                string graphPart = line.Substring(0, commitIndex);
                string dataPart = line.Substring(commitIndex);
                
                // Parse the commit data part
                // Format: "commit HASH|PARENT_HASHES|AUTHOR|DATE|MESSAGE|REF_NAMES"
                string[] parts = dataPart.Split('|');
                if (parts.Length < 5) continue;

                string hash = parts[0].Replace("commit ", "").Trim();
                string parentHashes = parts.Length > 1 ? parts[1].Trim() : "";
                string author = parts.Length > 2 ? parts[2].Trim() : "";
                
                // Parse date
                DateTime date = DateTime.Now;
                if (parts.Length > 3 && !string.IsNullOrEmpty(parts[3]))
                {
                    DateTime.TryParse(parts[3].Trim(), out date);
                }
                        
                string message = parts.Length > 4 ? parts[4].Trim() : "";
                string refNames = parts.Length > 5 ? parts[5].Trim() : "";
                
                // Create commit node
                var commit = new CommitNode
                {
                    Hash = hash,
                    Author = author,
                    Date = date,
                    Message = message,
                    RefNames = refNames,
                    GraphStructure = graphPart
                };
                
                // Parse parent hashes
                if (!string.IsNullOrWhiteSpace(parentHashes))
                {
                    foreach (var parentHash in parentHashes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        commit.ParentHashes.Add(parentHash.Trim());
                    }
                }
                
                commits.Add(commit);
                commitDict[hash] = commit;
            }
            
            // Set up parent-child relationships
            foreach (var commit in commits)
            {
                foreach (var parentHash in commit.ParentHashes)
                {
                    if (commitDict.TryGetValue(parentHash, out CommitNode parent))
                    {
                        commit.Parents.Add(parent);
                        parent.Children.Add(commit);
                    }
                }
            }
            
            return commits;
        }

    void DisplayCommitTree(List<CommitNode> commits)
    {
        // Clear previous visualization
        CommitGraphCanvas.Children.Clear();
        
        if (commits == null || commits.Count == 0)
            return;
        
        // Add CommitNode to ListView for selection
        CommitsList.ItemsSource = commits;
        
        // Define layout properties
        const double nodeWidth = 15;
        const double nodeHeight = 15;
        const double horizontalSpacing = 20;
        const double rowHeight = 30;
        
        // Calculate positions for each commit based on their relationships
        Dictionary<string, Point> nodePositions = CalculateCommitPositions(commits, horizontalSpacing, rowHeight);
        
        // Draw the connecting lines first (so they appear behind the nodes)
        foreach (var commit in commits)
        {
            if (!nodePositions.TryGetValue(commit.Hash, out Point position))
                continue;
                
            foreach (var parent in commit.Parents)
            {
                if (!nodePositions.TryGetValue(parent.Hash, out Point parentPosition))
                    continue;
                    
                // Create line from this commit to its parent
                var line = new Line
                {
                    X1 = position.X + nodeWidth / 2,
                    Y1 = position.Y + nodeHeight / 2,
                    X2 = parentPosition.X + nodeWidth / 2,
                    Y2 = parentPosition.Y + nodeHeight / 2,
                    Stroke = GetBranchColor(commit),
                    StrokeThickness = 2
                };
                
                CommitGraphCanvas.Children.Add(line);
            }
        }
        
        // Draw commit nodes
        foreach (var commit in commits)
        {
            if (!nodePositions.TryGetValue(commit.Hash, out Point position))
                continue;
                
            // Create an ellipse for each commit
            var ellipse = new Ellipse
            {
                Width = nodeWidth,
                Height = nodeHeight,
                Fill = GetBranchColor(commit),
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            
            Canvas.SetLeft(ellipse, position.X);
            Canvas.SetTop(ellipse, position.Y);
            
            // Add tooltip with commit info
            ellipse.ToolTip = $"{commit.Hash.Substring(0, 7)}: {commit.Message}";
            
            // Add to canvas
            CommitGraphCanvas.Children.Add(ellipse);
            
            // Add branch/tag labels if any
            if (!string.IsNullOrEmpty(commit.RefNames))
            {
                var label = new TextBlock
                {
                    Text = commit.RefNames.Replace("(", "").Replace(")", ""),
                    Foreground = Brushes.White,
                    Background = Brushes.DarkBlue,
                    Padding = new Thickness(3),
                    FontSize = 11
                };
                
                Canvas.SetLeft(label, position.X + nodeWidth + 5);
                Canvas.SetTop(label, position.Y - nodeHeight / 2);
                CommitGraphCanvas.Children.Add(label);
            }
        }
    }

    Dictionary<string, Point> CalculateCommitPositions(List<CommitNode> commits, double horizontalSpacing, double rowHeight)
    {
        var positions = new Dictionary<string, Point>();
        var branchLanes = new Dictionary<string, int>(); // Maps branch names to their horizontal lanes
        var nextLaneIndex = 0;
        
        // Sort commits by date, newest first
        var sortedCommits = commits.OrderByDescending(c => c.Date).ToList();
        
        // For each commit, assign to a row by index and a column based on branch
        for (int i = 0; i < sortedCommits.Count; i++)
        {
            var commit = sortedCommits[i];
            
            // Determine the branch lane (column) for this commit
            int lane;
            string branchKey = GetBranchKey(commit);
            
            if (!branchLanes.TryGetValue(branchKey, out lane))
            {
                lane = nextLaneIndex++;
                branchLanes[branchKey] = lane;
            }
            
            // Calculate X,Y position
            double x = 20 + (lane * horizontalSpacing);
            double y = 20 + (i * rowHeight);
            
            positions[commit.Hash] = new Point(x, y);
        }
        
        return positions;
    }

    string GetBranchKey(CommitNode commit)
    {
        // This is a simplified approach - a real implementation would be more sophisticated
        // Ideally, we'd track branches through the commit graph
        
        // If the commit has branch names, use that
        if (!string.IsNullOrEmpty(commit.RefNames))
            return commit.RefNames;
            
        // If it has parents, use the first parent's branch
        if (commit.Parents.Count > 0)
            return commit.Parents[0].Hash;
            
        // Fallback to using the commit hash itself
        return commit.Hash;
    }

    Brush GetBranchColor(CommitNode commit)
    {
        // Simplified branch coloring - in a real implementation,
        // would use commit.RefNames or branch analysis to determine colors
        
        // Use a hash code based on the branch key to pick a color
        string branchKey = GetBranchKey(commit);
        int colorIndex = Math.Abs(branchKey.GetHashCode()) % _branchColors.Length;
        
        return _branchColors[colorIndex];
    }
    }
}