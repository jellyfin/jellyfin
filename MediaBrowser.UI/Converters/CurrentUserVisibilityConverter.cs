using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MediaBrowser.UI.Converters
{
    public class CurrentUserVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (App.Instance.ServerConfiguration == null)
            {
                return Visibility.Collapsed;
            }

            return value == null ? Visibility.Collapsed : Visibility.Visible;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
