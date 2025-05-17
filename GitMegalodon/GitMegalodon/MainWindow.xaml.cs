using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using GitMegalodon.Models;
using Path = System.IO.Path;

namespace GitMegalodon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Windows 11 DWM API
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_MICA_EFFECT = 1029;

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

            // Apply the customization after window is loaded
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                IntPtr windowHandle = new WindowInteropHelper(this).Handle;

                // Check if we're running on Windows 11 or later
                if (IsWindows11OrLater())
                {
                    // Enable Mica effect
                    int micaValue = 1; // 1 for enable, 0 for disable
                    DwmSetWindowAttribute(windowHandle, DWMWA_MICA_EFFECT, ref micaValue, sizeof(int));

                    // Optional: Use dark title bar
                    int darkModeValue = 1; // 1 for dark mode, 0 for light mode
                    DwmSetWindowAttribute(windowHandle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkModeValue, sizeof(int));
                }
            }
            catch (Exception ex)
            {
                // Just log the error but don't stop the application
                Debug.WriteLine($"Error applying window effects: {ex.Message}");
            }
        }

        private bool IsWindows11OrLater()
        {
            try
            {
                // Windows 11 is version 10.0.22000 or higher
                var os = Environment.OSVersion;
                return os.Platform == PlatformID.Win32NT && 
                       (os.Version.Major > 10 || (os.Version.Major == 10 && os.Version.Build >= 22000));
            }
            catch
            {
                return false;
            }
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
            return Directory.Exists(System.IO.Path.Combine(path, ".git"));
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
            try
            {
                // Use a more reliable format string with clearer separators for parsing
                string output = await RunGitCommandAsync("log --all --format=\"%H§%P§%an§%at§%s§%D\" --date=raw -n 100");
                var commits = ParseGitLogOutput(output);
                
                // Display the commits in both the ListView and the graph
                DisplayCommitTree(commits);
                
                // Update the traditional commit list
                CommitsList.ItemsSource = commits.Select(c => new Commit(
                    c.Hash, 
                    c.Author, 
                    c.Date, 
                    c.Message
                ));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading commit history: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<CommitNode> ParseGitLogOutput(string output)
        {
            var commits = new List<CommitNode>();
            var commitDict = new Dictionary<string, CommitNode>();
            
            // Split on new lines and process each commit
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) 
                    continue;
                
                // Use a special character that's unlikely to be in commit data
                string[] parts = line.Split('§');
                if (parts.Length < 5) 
                    continue;
                
                string hash = parts[0];
                string parentHashes = parts[1];
                string author = parts[2];
                
                // Safely parse the timestamp
                DateTime date;
                if (long.TryParse(parts[3], out long timestamp))
                {
                    date = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                }
                else
                {
                    date = DateTime.Now; // Fallback if parsing fails
                }
                
                string message = parts[4];
                string refNames = parts.Length > 5 ? parts[5] : "";
                
                var commitNode = new CommitNode
                {
                    Hash = hash,
                    Author = author,
                    Date = date,
                    Message = message,
                    RefNames = refNames,
                    GraphStructure = "" // We're not using the graph data from git log output
                };
                
                // Add parent hashes
                if (!string.IsNullOrEmpty(parentHashes))
                {
                    foreach (var parentHash in parentHashes.Split(' '))
                    {
                        if (!string.IsNullOrEmpty(parentHash))
                            commitNode.ParentHashes.Add(parentHash);
                    }
                }
                
                commits.Add(commitNode);
                commitDict[hash] = commitNode;
            }
            
            // Link parents and children
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
                        string filePath = System.IO.Path.Combine(CurrentRepository.Path, change.Path);
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
            
            string[] lines = output.Split('\n');
            
            foreach (string line in lines)
            {
                // Skip empty lines
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                // Parse the graph structure from the beginning of the line
                int dataStart = line.IndexOf(';');
                if (dataStart <= 0) 
                {
                    // Try to find the hash at the beginning of the line
                    var match = Regex.Match(line, @"[*|\\/_ ]+([a-f0-9]{40})");
                    if (match.Success)
                    {
                        dataStart = match.Index + match.Length;
                    }
                    else
                    {
                        continue; // Skip lines we can't parse
                    }
                }
                
                // Get the graph structure part (everything before the hash)
                string graphPart = line.Substring(0, dataStart);
                
                // Split the commit data
                string[] parts = line.Substring(dataStart + 1).Split(';');
                
                if (parts.Length < 5) continue;
                
                string hash = parts[0];
                string parentHashes = parts[1];
                string author = parts[2];
                
                // Safely parse the timestamp
                DateTime date;
                if (long.TryParse(parts[3], out long timestamp))
                {
                    date = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                }
                else
                {
                    date = DateTime.Now; // Fallback if parsing fails
                }
                
                string message = parts[4];
                string refNames = parts.Length > 5 ? parts[5] : "";
                
                var commitNode = new CommitNode
                {
                    Hash = hash,
                    Author = author,
                    Date = date,
                    Message = message,
                    RefNames = refNames,
                    GraphStructure = graphPart
                };
                
                // Add parent hashes
                if (!string.IsNullOrEmpty(parentHashes))
                {
                    foreach (var parentHash in parentHashes.Split(' '))
                    {
                        if (!string.IsNullOrEmpty(parentHash))
                            commitNode.ParentHashes.Add(parentHash);
                    }
                }
                
                commits.Add(commitNode);
                commitDict[hash] = commitNode;
            }
            
            // Link parents and children
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


        private void DisplayCommitTree(List<CommitNode> commits)
        {
            // Clear the canvas
            CommitGraphCanvas.Children.Clear();

            if (commits == null || commits.Count == 0)
                return;

            // Configure visual parameters
            double rowHeight = 30;
            double columnWidth = 20;
            double commitRadius = 5;

            // Calculate positions for all commits
            var commitPositions = CalculateCommitPositions(commits, columnWidth, rowHeight);

            // Draw connections first (so they appear behind the commits)
            foreach (var commit in commits)
            {
                Point commitPoint = commitPositions[commit.Hash];

                // Draw lines to each parent
                foreach (var parent in commit.Parents)
                {
                    if (commitPositions.TryGetValue(parent.Hash, out Point parentPoint))
                    {
                        if (parentPoint.Y > commitPoint.Y) // Direct parent
                        {
                            // Draw a straight line
                            Line line = new Line
                            {
                                X1 = commitPoint.X,
                                Y1 = commitPoint.Y + commitRadius,
                                X2 = parentPoint.X,
                                Y2 = parentPoint.Y - commitRadius,
                                Stroke = GetBranchColor(commit),
                                StrokeThickness = 2
                            };
                            CommitGraphCanvas.Children.Add(line);
                        }
                        else // Branch connection
                        {
                            // Create a path with curves for branch lines
                            PathFigure figure = new PathFigure
                            {
                                StartPoint = new Point(commitPoint.X, commitPoint.Y + commitRadius)
                            };

                            // Determine if we need to route around other commits
                            if (parentPoint.X != commitPoint.X)
                            {
                                // Add curve segments
                                figure.Segments.Add(
                                    new LineSegment(new Point(commitPoint.X, commitPoint.Y + rowHeight / 2), true));
                                figure.Segments.Add(
                                    new LineSegment(new Point(parentPoint.X, commitPoint.Y + rowHeight / 2), true));
                                figure.Segments.Add(
                                    new LineSegment(new Point(parentPoint.X, parentPoint.Y - commitRadius), true));
                            }
                            else
                            {
                                figure.Segments.Add(
                                    new LineSegment(new Point(parentPoint.X, parentPoint.Y - commitRadius), true));
                            }

                            PathGeometry geometry = new PathGeometry();
                            geometry.Figures.Add(figure);

                            System.Windows.Shapes.Path path = new System.Windows.Shapes.Path
                            {
                                Data = geometry,
                                Stroke = GetBranchColor(parent),
                                StrokeThickness = 2
                            };

                            CommitGraphCanvas.Children.Add(path);
                        }
                    }
                }
            }

            // Draw commit dots and labels
            foreach (var commit in commits)
            {
                Point point = commitPositions[commit.Hash];

                // Draw commit circle
                Ellipse commitDot = new Ellipse
                {
                    Width = commitRadius * 2,
                    Height = commitRadius * 2,
                    Fill = GetBranchColor(commit),
                    Stroke = Brushes.White,
                    StrokeThickness = 1
                };

                Canvas.SetLeft(commitDot, point.X - commitRadius);
                Canvas.SetTop(commitDot, point.Y - commitRadius);
                CommitGraphCanvas.Children.Add(commitDot);

                // Add branch/tag labels if present
                if (!string.IsNullOrEmpty(commit.RefNames))
                {
                    var refLabels = commit.RefNames.Split(',');
                    double labelOffset = 5;

                    foreach (var refLabel in refLabels)
                    {
                        bool isTag = refLabel.Trim().StartsWith("tag:");
                        bool isHead = refLabel.Trim().Contains("HEAD");

                        Border labelBorder = new Border
                        {
                            Background = isTag ? Brushes.Yellow : isHead ? Brushes.Green : GetBranchColor(commit),
                            BorderBrush = Brushes.White,
                            BorderThickness = new Thickness(1),
                            CornerRadius = new CornerRadius(8),
                            Padding = new Thickness(5, 2, 5, 2)
                        };

                        TextBlock label = new TextBlock
                        {
                            Text = refLabel.Trim(),
                            Foreground = isTag || isHead ? Brushes.Black : Brushes.White,
                            FontSize = 10
                        };

                        labelBorder.Child = label;

                        Canvas.SetLeft(labelBorder, point.X + commitRadius + 5);
                        Canvas.SetTop(labelBorder, point.Y - 10 + labelOffset);
                        CommitGraphCanvas.Children.Add(labelBorder);

                        labelOffset += 20; // Offset for multiple labels
                    }
                }
            }

            // Set canvas size
            CommitGraphCanvas.Width = 1000;
            CommitGraphCanvas.Height = commits.Count * rowHeight + 50;
        }

        private Dictionary<string, Point> CalculateCommitPositions(List<CommitNode> commits, double columnWidth,
            double rowHeight)
        {
            Dictionary<string, Point> positions = new Dictionary<string, Point>();
            Dictionary<string, int> branchLanes = new Dictionary<string, int>();
            int maxLane = 0;

            // First pass: Assign vertical positions by commit order
            for (int i = 0; i < commits.Count; i++)
            {
                var commit = commits[i];
                int y = (i + 1) * (int)rowHeight;

                // Get or assign a lane for this commit's branch
                string branchKey = GetBranchKey(commit);
                if (!branchLanes.TryGetValue(branchKey, out int lane))
                {
                    lane = maxLane++;
                    branchLanes[branchKey] = lane;
                }

                int x = 100 + (lane * (int)columnWidth);
                positions[commit.Hash] = new Point(x, y);
            }

            // Second pass: Adjust horizontal positions for merges and branches
            foreach (var commit in commits)
            {
                // For merge commits, center them between the parent branches
                if (commit.Parents.Count > 1)
                {
                    var parentsInView = commit.Parents.Where(p => positions.ContainsKey(p.Hash)).ToList();
                    if (parentsInView.Count > 0)
                    {
                        double avgX = parentsInView.Average(p => positions[p.Hash].X);
                        var position = positions[commit.Hash];
                        positions[commit.Hash] = new Point(avgX, position.Y);
                    }
                }
            }

            return positions;
        }
        private string GetBranchKey(CommitNode commit)
        {
            // This is a simplified approach - in a real implementation, you would
            // determine the branch based on commit's relationship to branches
            if (!string.IsNullOrEmpty(commit.RefNames))
            {
                if (commit.RefNames.Contains("master")) return "master";
                if (commit.RefNames.Contains("main")) return "main";
        
                // Extract first branch name
                var match = Regex.Match(commit.RefNames, @"branch:([^\s,]+)");
                if (match.Success)
                    return match.Groups[1].Value;
            }
    
            // If we have parents, we can try to infer the branch
            if (commit.Parents.Count > 0 && commit.Children.Count <= 1)
            {
                // Stay on same branch as parent if possible
                return GetBranchKey(commit.Parents[0]);
            }
    
            // Fallback
            return commit.Hash.Substring(0, 7);
        }
        private Brush GetBranchColor(CommitNode commit)
        {
            // Map branch names to colors
            string branchKey = GetBranchKey(commit);
    
            // Simple hash-based color selection from a predefined set
            string[] colors =
            {
                "#3A86FF", "#FF006E", "#FB5607", "#FFBE0B",
                "#8338EC", "#06D6A0", "#118AB2", "#EF476F"
            };
    
            int colorIndex = Math.Abs(branchKey.GetHashCode()) % colors.Length;
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colors[colorIndex]));
        }
    }

    public class Repository
    {
        public string Path { get; }
        public string Name { get; }
    
        public ObservableCollection<Branch> Branches { get; } = new ObservableCollection<Branch>();
        public Branch CurrentBranch { get; set; }
    
        public ObservableCollection<FileChange> StagedChanges { get; } = new ObservableCollection<FileChange>();
        public ObservableCollection<FileChange> UnstagedChanges { get; } = new ObservableCollection<FileChange>();
    
        public Repository(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        
            // If the repository path ends with ".git", get the parent directory name
            if (Name.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                Name = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path));
            }
        }
    }
}
