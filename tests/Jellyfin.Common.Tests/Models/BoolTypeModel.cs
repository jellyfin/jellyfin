using System.Text.Json.Serialization;
using MediaBrowser.Common.Json.Converters;

namespace Jellyfin.Common.Tests.Models
{
    /// <summary>
    /// The bool type model.
    /// </summary>
    public class BoolTypeModel
    {
        /// <summary>
        /// Gets or sets a value indicating whether the value is true or false.
        /// </summary>
        [JsonConverter(typeof(JsonBoolNumberConverter))]
        public bool Value { get; set; }
    }
}