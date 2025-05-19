using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GitMegalodon.Services;

namespace GitMegalodon;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private GitService _gitService;
    private GraphicalInterfaceService _graphicalInterfaceService;
    
    public MainWindow()
    {
        InitializeComponent();
        
        _gitService = new GitService();
        _graphicalInterfaceService = new GraphicalInterfaceService();
    }

    private void OpenRepository(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("Opening repository");

        try
        {
            string path = "";
        
            var repoInfo = _gitService.OpenRepository(path);

            if (repoInfo != null)
            {
                
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        
    }
    
    private void CloneRepository(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("Cloning repository");
    }

    private void NewRepository(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("Creating new repository");
    }
    
    private void PushRepository(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("Pushing repository");
    }
    
    private void PullRepository(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("Pulling repository");
    }
    
    private void FetchRepository(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("Fetching repository");
    }
}