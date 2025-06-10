using BackyardBoss.Data;
using BackyardBoss.Views;
using System.Windows;

namespace BackyardBoss
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Set DataContext to MainWindowViewModel so it is never null
            this.DataContext = new MainWindowViewModel();
            // Load ProgramEditorView into the MainContent control
            //MainContent.Content = new ProgramEditorView();
            // Right after InitializeComponent

            // Move DB generation to Loaded event to ensure DataContext is set
            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm != null)
            {
                //await vm.GenerateSampleSensorDatabaseAsync();
            }
        }
    }
}