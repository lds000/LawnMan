using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Navigation;
using BackyardBoss.ViewModels;

namespace BackyardBoss.Views
{
    /// <summary>
    /// Interaction logic for WeatherPanelView.xaml
    /// </summary>
    public partial class WeatherPanelView : Window
    {
        public WeatherPanelView()
        {
            InitializeComponent();
            this.DataContext = new WeatherViewModel(); // ✅ Ensures constructor fires

            // Set the radar map URL (RainViewer embed for Boise, ID region)
            string radarUrl = "https://www.rainviewer.com/map.html?loc=43.6150,-116.2023,8&oFa=0&oC=0&oU=0&oCS=1&oF=0&oAP=0&oAR=0&oBR=1&oCL=1&oMM=1&oM=1&oO=0&oS=0&oSM=1";
            RadarWebView2.Source = new Uri(radarUrl);
        }

        private void RadarLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
