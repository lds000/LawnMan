using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace BackyardBoss.Converters
{
    public class SingleToCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return null;
            var list = (IList)Activator.CreateInstance(typeof(System.Collections.ObjectModel.ObservableCollection<>).MakeGenericType(value.GetType()))!;
            list.Add(value);
            return list;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
