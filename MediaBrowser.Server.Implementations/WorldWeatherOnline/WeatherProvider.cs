using System.Globalization;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Weather;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Weather;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.WorldWeatherOnline
{
    /// <summary>
    /// Based on http://www.worldweatheronline.com/free-weather-feed.aspx
    /// The classes in this file are a reproduction of the json output, which will then be converted to our weather model classes
    /// </summary>
    public class WeatherProvider : IWeatherProvider
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        protected IJsonSerializer JsonSerializer { get; private set; }

        /// <summary>
        /// The _HTTP client
        /// </summary>
        private IHttpClient HttpClient { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherProvider" /> class.
        /// </summary>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">logger</exception>
        public WeatherProvider(IJsonSerializer jsonSerializer, IHttpClient httpClient, ILogger logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }

            JsonSerializer = jsonSerializer;
            HttpClient = httpClient;
            Logger = logger;
        }

        /// <summary>
        /// The _weather semaphore
        /// </summary>
        private readonly SemaphoreSlim _weatherSemaphore = new SemaphoreSlim(10, 10);

        /// <summary>
        /// Gets the weather info async.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{WeatherInfo}.</returns>
        /// <exception cref="System.ArgumentNullException">location</exception>
        public async Task<WeatherInfo> GetWeatherInfoAsync(string location, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentNullException("location");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            const int numDays = 5;
            const string apiKey = "24902f60f1231941120109";

            var url = "http://free.worldweatheronline.com/feed/weather.ashx?q=" + location + "&format=json&num_of_days=" + numDays + "&key=" + apiKey;

            Logger.Info("Accessing weather from " + url);

            using (var stream = await HttpClient.Get(url, _weatherSemaphore, cancellationToken).ConfigureAwait(false))
            {
                var data = JsonSerializer.DeserializeFromStream<WeatherResult>(stream).data;

                return GetWeatherInfo(data);
            }
        }

        /// <summary>
        /// Converst the json output to our WeatherInfo model class
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>WeatherInfo.</returns>
        private WeatherInfo GetWeatherInfo(WeatherData data)
        {
            var info = new WeatherInfo();

            if (data.current_condition != null)
            {
                var condition = data.current_condition.FirstOrDefault();

                if (condition != null)
                {
                    info.CurrentWeather = condition.ToWeatherStatus();
                }
            }

            if (data.weather != null)
            {
                info.Forecasts = data.weather.Select(w => w.ToWeatherForecast()).ToArray();
            }

            return info;
        }
    }

    /// <summary>
    /// Class WeatherResult
    /// </summary>
    class WeatherResult
    {
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public WeatherData data { get; set; }
    }

    /// <summary>
    /// Class WeatherData
    /// </summary>
    public class WeatherData
    {
        /// <summary>
        /// Gets or sets the current_condition.
        /// </summary>
        /// <value>The current_condition.</value>
        public WeatherCondition[] current_condition { get; set; }
        /// <summary>
        /// Gets or sets the weather.
        /// </summary>
        /// <value>The weather.</value>
        public DailyWeatherInfo[] weather { get; set; }
    }

    /// <summary>
    /// Class WeatherCondition
    /// </summary>
    public class WeatherCondition
    {
        /// <summary>
        /// Gets or sets the temp_ C.
        /// </summary>
        /// <value>The temp_ C.</value>
        public string temp_C { get; set; }
        /// <summary>
        /// Gets or sets the temp_ F.
        /// </summary>
        /// <value>The temp_ F.</value>
        public string temp_F { get; set; }
        /// <summary>
        /// Gets or sets the humidity.
        /// </summary>
        /// <value>The humidity.</value>
        public string humidity { get; set; }
        /// <summary>
        /// Gets or sets the weather code.
        /// </summary>
        /// <value>The weather code.</value>
        public string weatherCode { get; set; }

        protected static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
        /// <summary>
        /// To the weather status.
        /// </summary>
        /// <returns>WeatherStatus.</returns>
        public WeatherStatus ToWeatherStatus()
        {
            return new WeatherStatus
            {
                TemperatureCelsius = int.Parse(temp_C, UsCulture),
                TemperatureFahrenheit = int.Parse(temp_F, UsCulture),
                Humidity = int.Parse(humidity, UsCulture),
                Condition = DailyWeatherInfo.GetCondition(weatherCode)
            };
        }
    }

    /// <summary>
    /// Class DailyWeatherInfo
    /// </summary>
    public class DailyWeatherInfo
    {
        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>The date.</value>
        public string date { get; set; }
        /// <summary>
        /// Gets or sets the precip MM.
        /// </summary>
        /// <value>The precip MM.</value>
        public string precipMM { get; set; }
        /// <summary>
        /// Gets or sets the temp max C.
        /// </summary>
        /// <value>The temp max C.</value>
        public string tempMaxC { get; set; }
        /// <summary>
        /// Gets or sets the temp max F.
        /// </summary>
        /// <value>The temp max F.</value>
        public string tempMaxF { get; set; }
        /// <summary>
        /// Gets or sets the temp min C.
        /// </summary>
        /// <value>The temp min C.</value>
        public string tempMinC { get; set; }
        /// <summary>
        /// Gets or sets the temp min F.
        /// </summary>
        /// <value>The temp min F.</value>
        public string tempMinF { get; set; }
        /// <summary>
        /// Gets or sets the weather code.
        /// </summary>
        /// <value>The weather code.</value>
        public string weatherCode { get; set; }
        /// <summary>
        /// Gets or sets the winddir16 point.
        /// </summary>
        /// <value>The winddir16 point.</value>
        public string winddir16Point { get; set; }
        /// <summary>
        /// Gets or sets the winddir degree.
        /// </summary>
        /// <value>The winddir degree.</value>
        public string winddirDegree { get; set; }
        /// <summary>
        /// Gets or sets the winddirection.
        /// </summary>
        /// <value>The winddirection.</value>
        public string winddirection { get; set; }
        /// <summary>
        /// Gets or sets the windspeed KMPH.
        /// </summary>
        /// <value>The windspeed KMPH.</value>
        public string windspeedKmph { get; set; }
        /// <summary>
        /// Gets or sets the windspeed miles.
        /// </summary>
        /// <value>The windspeed miles.</value>
        public string windspeedMiles { get; set; }

        protected static readonly CultureInfo UsCulture = new CultureInfo("en-US");
        
        /// <summary>
        /// To the weather forecast.
        /// </summary>
        /// <returns>WeatherForecast.</returns>
        public WeatherForecast ToWeatherForecast()
        {
            return new WeatherForecast
            {
                Date = DateTime.Parse(date, UsCulture),
                HighTemperatureCelsius = int.Parse(tempMaxC, UsCulture),
                HighTemperatureFahrenheit = int.Parse(tempMaxF, UsCulture),
                LowTemperatureCelsius = int.Parse(tempMinC, UsCulture),
                LowTemperatureFahrenheit = int.Parse(tempMinF, UsCulture),
                Condition = GetCondition(weatherCode)
            };
        }

        /// <summary>
        /// Gets the condition.
        /// </summary>
        /// <param name="weatherCode">The weather code.</param>
        /// <returns>WeatherConditions.</returns>
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