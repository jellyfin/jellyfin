using System.Text.Json;
using System.Text.Json.Serialization;
using MediaBrowser.Common.Json.Converters;

namespace MediaBrowser.Common.Json
{
    /// <summary>
    /// Helper class for having compatible JSON throughout the codebase.
    /// </summary>
    public static class JsonDefaults
    {
        /// <summary>
        /// Gets the default <see cref="JsonSerializerOptions" /> options.
        /// </summary>
        /// <returns>The default <see cref="JsonSerializerOptions" /> options.</returns>
        public static JsonSerializerOptions GetOptions()
        {
            var options = new JsonSerializerOptions()
            {
                ReadCommentHandling = JsonCommentHandling.Disallow,
                WriteIndented = false
            };

            options.Converters.Add(new JsonGuidConverter());
            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }
    }
}
