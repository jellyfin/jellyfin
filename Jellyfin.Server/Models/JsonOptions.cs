using System.Text.Json;

namespace Jellyfin.Server.Models
{
    /// <summary>
    /// Json Options.
    /// </summary>
    public static class JsonOptions
    {
        /// <summary>
        /// Gets CamelCase json options.
        /// </summary>
        public static JsonSerializerOptions CamelCase
        {
            get
            {
                var options = DefaultJsonOptions;
                options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                return options;
            }
        }

        /// <summary>
        /// Gets PascalCase json options.
        /// </summary>
        public static JsonSerializerOptions PascalCase
        {
            get
            {
                var options = DefaultJsonOptions;
                options.PropertyNamingPolicy = null;
                return options;
            }
        }

        /// <summary>
        /// Gets base Json Serializer Options.
        /// </summary>
        private static JsonSerializerOptions DefaultJsonOptions => new JsonSerializerOptions();
    }
}
