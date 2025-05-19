using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Collections.Generic;

namespace BackyardBoss.Converters
{
    /// <summary>
    /// Converts a color string (e.g., "green", "red", "yellow") or a zone name (with LedColors dictionary) to a SolidColorBrush for LED display.
    /// </summary>
    public class ColorStringToBrushConverter : IMultiValueConverter, IValueConverter
    {
        // For MultiBinding: [0]=zone name, [1]=LedColors dictionary
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is string zoneName && values[1] is IDictionary<string, string> ledColors)
            {
                if (ledColors.TryGetValue(zoneName, out var colorStr))
                {
                    return ColorStringToBrush(colorStr);
                }
                return Brushes.Transparent;
            }
            return Brushes.Transparent;
        }

        // For single value (system LED)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string colorString)
            {
                return ColorStringToBrush(colorString);
            }
            return Brushes.Transparent;
        }

        private static SolidColorBrush ColorStringToBrush(string colorStr)
        {
            switch (colorStr?.ToLowerInvariant())
            {
                case "green": return Brushes.LimeGreen;
                case "red": return Brushes.Red;
                case "yellow": return Brushes.Gold;
                case "blue": return Brushes.DodgerBlue;
                case "orange": return Brushes.Orange;
                case "gray": return Brushes.Gray;
                default:
                    try { return (SolidColorBrush)new BrushConverter().ConvertFromString(colorStr); }
                    catch { return Brushes.Transparent; }
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
