using System;
using System.Globalization;
using System.Windows.Data;

namespace MediaBrowser.UI.Converters
{
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var date = (DateTime)value;

            string format = parameter as string;

            if (string.IsNullOrEmpty(format))
            {
                return date.ToString();
            }
            
            if (format.Equals("shorttime", StringComparison.OrdinalIgnoreCase))
            {
                return date.ToShortTimeString();
            }

            return date.ToString(format);
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
