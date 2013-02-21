using System;
using ProtoBuf;

namespace MediaBrowser.Model.Weather
{
    /// <summary>
    /// Represents a weather forecase for a specific date
    /// </summary>
    [ProtoContract]
    public class WeatherForecast
    {
        [ProtoMember(1)]
        public DateTime Date { get; set; }

        [ProtoMember(2)]
        public int HighTemperatureFahrenheit { get; set; }

        [ProtoMember(3)]
        public int LowTemperatureFahrenheit { get; set; }

        [ProtoMember(4)]
        public int HighTemperatureCelsius { get; set; }

        [ProtoMember(5)]
        public int LowTemperatureCelsius { get; set; }

        [ProtoMember(6)]
        public WeatherConditions Condition { get; set; }
    }
}
