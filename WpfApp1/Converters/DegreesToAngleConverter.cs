using System;
using System.Globalization;
using System.Windows.Data;

namespace BackyardBoss.Converters
{
    public class DegreesToAngleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return 0.0;
            if (double.TryParse(value.ToString(), out double deg))
                return deg;
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
