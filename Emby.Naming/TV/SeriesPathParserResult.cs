namespace Emby.Naming.TV
{
    /// <summary>
    /// Holder object for <see cref="SeriesPathParser"/> result.
    /// </summary>
    public class SeriesPathParserResult
    {
        /// <summary>
        /// Gets or sets the name of the series.
        /// </summary>
        /// <value>The name of the series.</value>
        public string? SeriesName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether parsing was successful.
        /// </summary>
        public bool Success { get; set; }
    }
}
