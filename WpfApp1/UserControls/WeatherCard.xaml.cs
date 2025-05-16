using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media; // <-- Add this for VisualTreeHelper
using BackyardBoss.Views;
using System.Linq;

namespace BackyardBoss.UserControls
{
    public partial class WeatherCard : UserControl
    {
        public WeatherCard()
        {
            InitializeComponent();
            this.Loaded += WeatherCard_Loaded;
        }

        private void WeatherCard_Loaded(object sender, RoutedEventArgs e)
        {
            // Removed SetNextRunFromJson();
        }

        private void WeatherPanel_Click(object sender, MouseButtonEventArgs e)
        {
            var window = new WeatherPanelView();
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }
    }
}