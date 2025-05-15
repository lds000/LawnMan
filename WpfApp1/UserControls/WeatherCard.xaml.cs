using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            // TODO: Add your logic for when the weather card is clicked
            MessageBox.Show("Weather card clicked — hook up detail view or refresh logic here.", "Weather", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}