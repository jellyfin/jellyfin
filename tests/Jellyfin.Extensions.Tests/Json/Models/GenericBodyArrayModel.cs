using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Jellyfin.Extensions.Json.Converters;

namespace Jellyfin.Extensions.Tests.Json.Models
{
    /// <summary>
    /// The generic body model.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public sealed class GenericBodyArrayModel<T>
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:Properties should not return arrays", MessageId = "Value", Justification = "Imported from ServiceStack")]
        [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
        public T[] Value { get; set; } = default!;
    }
}
