using System;

namespace MediaBrowser.Model.Weather
{
    /// <summary>
    /// Represents a weather forecast for a specific date
    /// </summary>
    public class WeatherForecast
    {
        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>The date.</value>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the high temperature fahrenheit.
        /// </summary>
        /// <value>The high temperature fahrenheit.</value>
        public int HighTemperatureFahrenheit { get; set; }

        /// <summary>
        /// Gets or sets the low temperature fahrenheit.
        /// </summary>
        /// <value>The low temperature fahrenheit.</value>
        public int LowTemperatureFahrenheit { get; set; }

        /// <summary>
        /// Gets or sets the high temperature celsius.
        /// </summary>
        /// <value>The high temperature celsius.</value>
        public int HighTemperatureCelsius { get; set; }

        /// <summary>
        /// Gets or sets the low temperature celsius.
        /// </summary>
        /// <value>The low temperature celsius.</value>
        public int LowTemperatureCelsius { get; set; }

        /// <summary>
        /// Gets or sets the condition.
        /// </summary>
        /// <value>The condition.</value>
        public WeatherConditions Condition { get; set; }
    }
}
