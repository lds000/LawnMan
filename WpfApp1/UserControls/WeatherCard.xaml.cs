using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BackyardBoss.Views;

namespace BackyardBoss.UserControls
{
    public partial class WeatherCard : UserControl
    {
        public WeatherCard()
        {
            InitializeComponent();
        }

        private void WeatherPanel_Click(object sender, MouseButtonEventArgs e)
        {
            // Open the WeatherPanelView window with the current WeatherViewModel as DataContext
            var window = new WeatherPanelView();
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }
    }
}