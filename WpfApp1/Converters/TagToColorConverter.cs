using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BackyardBoss.Converters
{
    public class TagToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int index = -1;

            if (value is int intVal)
            {
                index = intVal;
            }
            else if (value is string s && int.TryParse(s, out int parsed))
            {
                index = parsed;
            }

            int today = CalculateTodayScheduleIndex();
            string key = (index == today) ? "ButtonTodayIndex" : "PrimaryText";

            return Application.Current.Resources[key] as SolidColorBrush;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;

        private int CalculateTodayScheduleIndex()
        {
            var baseDate = new DateTime(2023, 12, 31); // Sunday before Jan 1, 2024
            var today = DateTime.Today;
            var deltaDays = (today - baseDate).Days;
            return deltaDays % 14;
        }
    }
}
