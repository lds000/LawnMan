using System.Windows;
using BackyardBoss.ViewModels;
using BackyardBoss.Services;

namespace WpfApp1
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Ensure DebugViewModel.Current is initialized before any debug logs
            _ = new DebugViewModel();
            DebugLogger.LogFileIO("Test log from App.xaml.cs startup");
            DebugLogger.LogCurrentIssue("hello world.");
            base.OnStartup(e);
        }
    }
}
