using ProtoBuf;

namespace MediaBrowser.Model.Weather
{
    /// <summary>
    /// Class WeatherInfo
    /// </summary>
    [ProtoContract]
    public class WeatherInfo
    {
        /// <summary>
        /// Gets or sets the current weather.
        /// </summary>
        /// <value>The current weather.</value>
        [ProtoMember(1)]
        public WeatherStatus CurrentWeather { get; set; }

        /// <summary>
        /// Gets or sets the forecasts.
        /// </summary>
        /// <value>The forecasts.</value>
        [ProtoMember(2)]
        public WeatherForecast[] Forecasts { get; set; }
    }
}
