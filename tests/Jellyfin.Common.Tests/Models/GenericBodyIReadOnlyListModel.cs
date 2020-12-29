using System.Collections.Generic;
using System.Text.Json.Serialization;
using MediaBrowser.Common.Json.Converters;

namespace Jellyfin.Common.Tests.Models
{
    /// <summary>
    /// The generic body <c>IReadOnlyList</c> model.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public class GenericBodyIReadOnlyListModel<T>
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [JsonConverter(typeof(JsonCommaDelimitedArrayConverterFactory))]
        public IReadOnlyList<T> Value { get; set; } = default!;
    }
}
