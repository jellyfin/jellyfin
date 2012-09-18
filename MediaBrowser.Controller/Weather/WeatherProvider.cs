using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.Weather;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Weather
{
    /// <summary>
    /// Based on http://www.worldweatheronline.com/free-weather-feed.aspx
    /// The classes in this file are a reproduction of the json output, which will then be converted to our weather model classes
    /// </summary>
    [Export(typeof(BaseWeatherProvider))]
    public class WeatherProvider : BaseWeatherProvider
    {
        public override async Task<WeatherInfo> GetWeatherInfoAsync(string zipCode)
        {
            if (string.IsNullOrWhiteSpace(zipCode))
            {
                return null;
            }

            const int numDays = 5;
            const string apiKey = "24902f60f1231941120109";

            string url = "http://free.worldweatheronline.com/feed/weather.ashx?q=" + zipCode + "&format=json&num_of_days=" + numDays + "&key=" + apiKey;

            Logger.LogInfo("Accessing weather from " + url);

            using (Stream stream = await HttpClient.GetStreamAsync(url).ConfigureAwait(false))
            {
                WeatherData data = JsonSerializer.DeserializeFromStream<WeatherResult>(stream).data;

                return GetWeatherInfo(data);
            }
        }

        /// <summary>
        /// Converst the json output to our WeatherInfo model class
        /// </summary>
        private WeatherInfo GetWeatherInfo(WeatherData data)
        {
            var info = new WeatherInfo();

            if (data.current_condition != null)
            {
                if (data.current_condition.Any())
                {
                    info.CurrentWeather = data.current_condition.First().ToWeatherStatus();
                }
            }

            if (data.weather != null)
            {
                info.Forecasts = data.weather.Select(w => w.ToWeatherForecast()).ToArray();
            }

            return info;
        }
    }

    class WeatherResult
    {
        public WeatherData data { get; set; }
    }

    public class WeatherData
    {
        public WeatherCondition[] current_condition { get; set; }
        public DailyWeatherInfo[] weather { get; set; }
    }

    public class WeatherCondition
    {
        public string temp_C { get; set; }
        public string temp_F { get; set; }
        public string humidity { get; set; }
        public string weatherCode { get; set; }

        public WeatherStatus ToWeatherStatus()
        {
            return new WeatherStatus
            {
                TemperatureCelsius = int.Parse(temp_C),
                TemperatureFahrenheit = int.Parse(temp_F),
                Humidity = int.Parse(humidity),
                Condition = DailyWeatherInfo.GetCondition(weatherCode)
            };
        }
    }

    public class DailyWeatherInfo
    {
        public string date { get; set; }
        public string precipMM { get; set; }
        public string tempMaxC { get; set; }
        public string tempMaxF { get; set; }
        public string tempMinC { get; set; }
        public string tempMinF { get; set; }
        public string weatherCode { get; set; }
        public string winddir16Point { get; set; }
        public string winddirDegree { get; set; }
        public string winddirection { get; set; }
        public string windspeedKmph { get; set; }
        public string windspeedMiles { get; set; }

        public WeatherForecast ToWeatherForecast()
        {
            return new WeatherForecast
            {
                Date = DateTime.Parse(date),
                HighTemperatureCelsius = int.Parse(tempMaxC),
                HighTemperatureFahrenheit = int.Parse(tempMaxF),
                LowTemperatureCelsius = int.Parse(tempMinC),
                LowTemperatureFahrenheit = int.Parse(tempMinF),
                Condition = GetCondition(weatherCode)
            };
        }

        public static WeatherConditions GetCondition(string weatherCode)
        {
            switch (weatherCode)
            {
                case "362":
                case "365":
                case "320":
                case "317":
                case "182":
                    return WeatherConditions.Sleet;
                case "338":
                case "335":
                case "332":
                case "329":
                case "326":
                case "323":
                case "377":
                case "374":
                case "371":
                case "368":
                case "395":
                case "392":
                case "350":
                case "227":
                case "179":
                    return WeatherConditions.Snow;
                case "314":
                case "311":
                case "308":
                case "305":
                case "302":
                case "299":
                case "296":
                case "293":
                case "284":
                case "281":
                case "266":
                case "263":
                case "359":
                case "356":
                case "353":
                case "185":
                case "176":
                    return WeatherConditions.Rain;
                case "260":
                case "248":
                    return WeatherConditions.Fog;
                case "389":
                case "386":
                case "200":
                    return WeatherConditions.Thunderstorm;
                case "230":
                    return WeatherConditions.Blizzard;
                case "143":
                    return WeatherConditions.Mist;
                case "122":
                    return WeatherConditions.Overcast;
                case "119":
                    return WeatherConditions.Cloudy;
                case "115":
                    return WeatherConditions.PartlyCloudy;
                default:
                    return WeatherConditions.Sunny;
            }
        }
    }
}
