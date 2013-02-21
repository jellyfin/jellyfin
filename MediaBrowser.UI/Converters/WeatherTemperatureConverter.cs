using MediaBrowser.Model.Weather;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MediaBrowser.UI.Converters
{
    public class WeatherTemperatureConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var weather = value as WeatherInfo;

            if (weather != null && weather.CurrentWeather != null)
            {
                if (App.Instance.ServerConfiguration.WeatherUnit == WeatherUnits.Celsius)
                {
                    return weather.CurrentWeather.TemperatureCelsius + "°C";
                }

                return weather.CurrentWeather.TemperatureFahrenheit + "°F";
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
