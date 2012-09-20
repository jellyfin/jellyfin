using MediaBrowser.Model.Weather;
using System;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Windows.Data;

namespace MediaBrowser.Plugins.DefaultTheme.Converters
{
    [PartNotDiscoverable]
    public class WeatherImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var weather = value as WeatherInfo;

            if (weather != null)
            {
                switch (weather.CurrentWeather.Condition)
                {
                    case WeatherConditions.Thunderstorm:
                        return "../Images/Weather/Thunder.png";
                    case WeatherConditions.Overcast:
                        return "../Images/Weather/Overcast.png";
                    case WeatherConditions.Mist:
                    case WeatherConditions.Sleet:
                    case WeatherConditions.Rain:
                        return "../Images/Weather/Rain.png";
                    case WeatherConditions.Blizzard:
                    case WeatherConditions.Snow:
                        return "../Images/Weather/Snow.png";
                    default:
                        return "../Images/Weather/Sunny.png";
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
