#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA2227 // Collection properties should be read only

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Extensions.Json.Converters;

namespace Jellyfin.Extensions.Tests.Json.Models
{
    /// <summary>
    /// The generic body <c>List</c> model.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public sealed class GenericBodyListModel<T>
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [JsonConverter(typeof(JsonCommaDelimitedCollectionConverterFactory))]
        public List<T> Value { get; set; } = default!;
    }
}
