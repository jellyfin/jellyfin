using MediaBrowser.Model.Dto;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MediaBrowser.UI.Converters
{
    public class LastSeenTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var user = value as UserDto;

            if (user != null)
            {
                if (user.LastActivityDate.HasValue)
                {
                    DateTime date = user.LastActivityDate.Value.ToLocalTime();

                    return "Last seen " + GetRelativeTimeText(date);
                }
            }

            return null;
        }

        private static string GetRelativeTimeText(DateTime date)
        {
            TimeSpan ts = DateTime.Now - date;

            const int second = 1;
            const int minute = 60 * second;
            const int hour = 60 * minute;
            const int day = 24 * hour;
            const int month = 30 * day;

            int delta = System.Convert.ToInt32(ts.TotalSeconds);

            if (delta < 0)
            {
                return "not yet";
            }
            if (delta < 1 * minute)
            {
                return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";
            }
            if (delta < 2 * minute)
            {
                return "a minute ago";
            }
            if (delta < 45 * minute)
            {
                return ts.Minutes + " minutes ago";
            }
            if (delta < 90 * minute)
            {
                return "an hour ago";
            }
            if (delta < 24 * hour)
            {
                return ts.Hours == 1 ? "an hour ago" : ts.Hours + " hours ago";
            }
            if (delta < 48 * hour)
            {
                return "yesterday";
            }
            if (delta < 30 * day)
            {
                return ts.Days + " days ago";
            }
            if (delta < 12 * month)
            {
                int months = System.Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "one month ago" : months + " months ago";
            }

            int years = System.Convert.ToInt32(Math.Floor((double)ts.Days / 365));
            return years <= 1 ? "one year ago" : years + " years ago";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
