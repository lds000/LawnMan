using System;
using System.Globalization;
using System.Windows.Data;

namespace BackyardBoss.Converters
{
    public class MinuteToHeightConverter : IValueConverter
    {
        public double Scale { get; set; } = 3.0;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double minutes)
                return minutes * Scale;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
