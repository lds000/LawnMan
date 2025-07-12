using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace BackyardBoss.Converters
{
    public class ZoneStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string status = value as string;
            return status switch
            {
                "Running" => Brushes.Green,
                "Watering" => Brushes.Green,
                "Soak" => Brushes.Brown,
                "Idle" => Brushes.LightGray,
                _ => Brushes.Black
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
