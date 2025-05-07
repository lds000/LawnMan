using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BackyardBoss.Converters
{
    public class AdjustmentVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double seasonalAdjustment && Math.Abs(seasonalAdjustment - 1.0) < 0.001)
            {
                return Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
