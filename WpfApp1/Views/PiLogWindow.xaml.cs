using System.Collections.ObjectModel;
using System.Windows;

namespace BackyardBoss.Views
{
    public partial class PiLogWindow : Window
    {
        public PiLogWindow(ObservableCollection<string> log)
        {
            InitializeComponent();
            LogItemsControl.ItemsSource = log;
        }
    }
}
