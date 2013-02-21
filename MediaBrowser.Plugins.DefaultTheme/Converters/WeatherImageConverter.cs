using MediaBrowser.Model.Weather;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MediaBrowser.Plugins.DefaultTheme.Converters
{
    /// <summary>
    /// Generates a weather image based on the current forecast
    /// </summary>
    public class WeatherImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var weather = value as WeatherInfo;

            if (weather != null && weather.CurrentWeather != null)
            {
                switch (weather.CurrentWeather.Condition)
                {
                    case WeatherConditions.Thunderstorm:
                        return "../Resources/Images/Weather/Thunder.png";
                    case WeatherConditions.Overcast:
                        return "../Resources/Images/Weather/Overcast.png";
                    case WeatherConditions.Mist:
                    case WeatherConditions.Sleet:
                    case WeatherConditions.Rain:
                        return "../Resources/Images/Weather/Rain.png";
                    case WeatherConditions.Blizzard:
                    case WeatherConditions.Snow:
                        return "../Resources/Images/Weather/Snow.png";
                    case WeatherConditions.Cloudy:
                        return "../Resources/Images/Weather/Cloudy.png";
                    case WeatherConditions.PartlyCloudy:
                        return "../Resources/Images/Weather/PartlyCloudy.png";
                    default:
                        return "../Resources/Images/Weather/Sunny.png";
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
