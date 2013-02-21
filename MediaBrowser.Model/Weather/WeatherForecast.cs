using System;
using ProtoBuf;

namespace MediaBrowser.Model.Weather
{
    /// <summary>
    /// Represents a weather forecast for a specific date
    /// </summary>
    [ProtoContract]
    public class WeatherForecast
    {
        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>The date.</value>
        [ProtoMember(1)]
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the high temperature fahrenheit.
        /// </summary>
        /// <value>The high temperature fahrenheit.</value>
        [ProtoMember(2)]
        public int HighTemperatureFahrenheit { get; set; }

        /// <summary>
        /// Gets or sets the low temperature fahrenheit.
        /// </summary>
        /// <value>The low temperature fahrenheit.</value>
        [ProtoMember(3)]
        public int LowTemperatureFahrenheit { get; set; }

        /// <summary>
        /// Gets or sets the high temperature celsius.
        /// </summary>
        /// <value>The high temperature celsius.</value>
        [ProtoMember(4)]
        public int HighTemperatureCelsius { get; set; }

        /// <summary>
        /// Gets or sets the low temperature celsius.
        /// </summary>
        /// <value>The low temperature celsius.</value>
        [ProtoMember(5)]
        public int LowTemperatureCelsius { get; set; }

        /// <summary>
        /// Gets or sets the condition.
        /// </summary>
        /// <value>The condition.</value>
        [ProtoMember(6)]
        public WeatherConditions Condition { get; set; }
    }
}
