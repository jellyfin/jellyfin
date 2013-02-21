using ProtoBuf;

namespace MediaBrowser.Model.Weather
{
    /// <summary>
    /// Represents the current weather status
    /// </summary>
    [ProtoContract]
    public class WeatherStatus
    {
        [ProtoMember(1)]
        public int TemperatureFahrenheit { get; set; }

        [ProtoMember(2)]
        public int TemperatureCelsius { get; set; }

        [ProtoMember(3)]
        public int Humidity { get; set; }

        [ProtoMember(4)]
        public WeatherConditions Condition { get; set; }
    }

    public enum WeatherConditions
    {
        Sunny,
        PartlyCloudy,
        Cloudy,
        Overcast,
        Mist,
        Snow,
        Rain,
        Sleet,
        Fog,
        Blizzard,
        Thunderstorm
    }
}
