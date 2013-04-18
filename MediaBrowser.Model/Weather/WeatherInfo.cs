
namespace MediaBrowser.Model.Weather
{
    /// <summary>
    /// Class WeatherInfo
    /// </summary>
    public class WeatherInfo
    {
        /// <summary>
        /// Gets or sets the current weather.
        /// </summary>
        /// <value>The current weather.</value>
        public WeatherStatus CurrentWeather { get; set; }

        /// <summary>
        /// Gets or sets the forecasts.
        /// </summary>
        /// <value>The forecasts.</value>
        public WeatherForecast[] Forecasts { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WeatherInfo"/> class.
        /// </summary>
        public WeatherInfo()
        {
            Forecasts = new WeatherForecast[] {};
        }
    }
}
