using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace GitMegalodon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> RecentRepositories { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();
            RecentRepositories = new ObservableCollection<string>();
            DataContext = this;
            
            // TODO: Load recent repositories from settings
        }

        private void OpenRepository_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select a Git repository folder"
            };

            if (dialog.ShowDialog() == true) // returns bool? in WPF dialogs
            {
                if (IsGitRepository(dialog.FolderName))
                {
                    LoadRepository(dialog.FolderName);
                }
                else
                {
                    MessageBox.Show("The selected folder is not a Git repository.",
                        "Invalid Repository", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool IsGitRepository(string path)
        {
            // Check if the folder contains a .git directory
            return Directory.Exists(Path.Combine(path, ".git"));
        }

        private void LoadRepository(string repositoryPath)
        {
            // TODO: Add repository to recent list
            // TODO: Load repository data (branches, commits, etc.)
            
            // For now, just add to recent repositories if not already there
            if (!RecentRepositories.Contains(repositoryPath))
            {
                RecentRepositories.Add(repositoryPath);
                // TODO: Save to settings
            }

            // Load repository info
            LoadBranches(repositoryPath);
            LoadCommitHistory(repositoryPath);
            LoadChanges(repositoryPath);
        }

        private void LoadBranches(string repositoryPath)
        {
            // TODO: Run git command to get branches
            // Example: git branch -a
        }

        private void LoadCommitHistory(string repositoryPath)
        {
            // TODO: Run git command to get commit history
            // Example: git log --pretty=format:"%H|%an|%ad|%s" --date=iso
        }

        private void LoadChanges(string repositoryPath)
        {
            // TODO: Run git command to get changes
            // Example: git status -s
        }

        private async Task<string> RunGitCommand(string repositoryPath, string arguments)
        {
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
                    WorkingDirectory = repositoryPath
                }
            };

            try
            {
                process.Start();
                // Read the output
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                return output;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing Git command: {ex.Message}", 
                    "Git Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }
    }
}