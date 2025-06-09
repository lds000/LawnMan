using System;
using System.Globalization;
using System.Windows.Data;

namespace BackyardBoss.Converters
{
    public class PercentToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return (int)Math.Round(doubleValue * 2);
            }
            throw new InvalidCastException("Expected a double value.");

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
