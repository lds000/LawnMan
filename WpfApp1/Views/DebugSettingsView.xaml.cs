using System.Windows.Controls;
using BackyardBoss.ViewModels;
using BackyardBoss.Services;

namespace BackyardBoss.Views
{
    public partial class DebugSettingsView : UserControl
    {
        public DebugSettingsView()
        {
            InitializeComponent();
            this.DataContext = new DebugSettingsViewModel(DebugLogger.Settings);
        }
    }
}
