using ProtoBuf;

namespace MediaBrowser.Model.Weather
{
    [ProtoContract]
    public class WeatherInfo
    {
        [ProtoMember(1)]
        public WeatherStatus CurrentWeather { get; set; }

        [ProtoMember(2)]
        public WeatherForecast[] Forecasts { get; set; }
    }
}
