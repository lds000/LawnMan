using System;
using System.Globalization;
using System.Windows.Data;

namespace BackyardBoss.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;
            string parameterString = parameter.ToString();
            if (parameterString == null)
                return false;
            return value.ToString().Equals(parameterString, StringComparison.InvariantCultureIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter == null)
                return null;
            return Enum.Parse(targetType, parameter.ToString());
        }
    }
}
