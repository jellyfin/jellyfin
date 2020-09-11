#nullable disable
namespace MediaBrowser.Model.Globalization
{
    /// <summary>
    /// Class CountryInfo.
    /// </summary>
    public class CountryInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the name of the two letter ISO region.
        /// </summary>
        /// <value>The name of the two letter ISO region.</value>
        public string TwoLetterISORegionName { get; set; }

        /// <summary>
        /// Gets or sets the name of the three letter ISO region.
        /// </summary>
        /// <value>The name of the three letter ISO region.</value>
        public string ThreeLetterISORegionName { get; set; }
    }
}
