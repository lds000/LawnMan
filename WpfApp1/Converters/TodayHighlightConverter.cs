using System;
using System.Globalization;
using System.Windows.Data;

namespace BackyardBoss.Converters
{
    public class TodayHighlightConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 &&
                values[0] is object tagValue &&
                values[1] is int todayIndex &&
                int.TryParse(tagValue.ToString(), out int index))
            {
                return index == todayIndex;
            }

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
