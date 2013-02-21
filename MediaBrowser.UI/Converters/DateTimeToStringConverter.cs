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

            // If a theme asks for this, they know it's only going to work if the current culture is en-us
            if (format.Equals("timesuffixlower", StringComparison.OrdinalIgnoreCase))
            {
                if (CultureInfo.CurrentCulture.Name.Equals("en-US", StringComparison.OrdinalIgnoreCase))
                {
                    var time = date.ToString("t");
                    var values = time.Split(' ');
                    return values[values.Length - 1].ToLower();
                }
                return string.Empty;
            }

            return date.ToString(format);
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
