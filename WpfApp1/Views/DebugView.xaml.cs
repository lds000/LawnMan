using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BackyardBoss.ViewModels;

namespace BackyardBoss.Views
{
    public partial class DebugView : UserControl
    {
        public DebugView()
        {
            InitializeComponent();
            this.DataContext = DebugViewModel.Current; // Use singleton instance
        }

        private void DebugSettingsView_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void CopyDetailsToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is BackyardBoss.ViewModels.DebugViewModel vm && vm.FilteredDebugItems != null)
            {
                var lines = vm.FilteredDebugItems.Select(item =>
                    $"{item.Timestamp:yyyy-MM-dd HH:mm:ss}\t{item.Source}\t{item.Message}\t{item.Details}");
                var text = string.Join("\n", lines);
                Clipboard.SetText(text);
            }
        }
    }
}
