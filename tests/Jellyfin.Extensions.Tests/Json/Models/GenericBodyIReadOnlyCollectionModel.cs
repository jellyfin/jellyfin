using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Extensions.Json.Converters;

namespace Jellyfin.Extensions.Tests.Json.Models
{
    /// <summary>
    /// The generic body <c>IReadOnlyCollection</c> model.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public sealed class GenericBodyIReadOnlyCollectionModel<T>
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
        public IReadOnlyCollection<T> Value { get; set; } = default!;
    }
}
