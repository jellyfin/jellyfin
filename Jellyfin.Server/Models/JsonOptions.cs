using System.Text.Json;

namespace Jellyfin.Server.Models
{
    /// <summary>
    /// Json Options.
    /// </summary>
    public static class JsonOptions
    {
        /// <summary>
        /// Base Json Serializer Options.
        /// </summary>
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions();

        /// <summary>
        /// Gets CamelCase json options.
        /// </summary>
        public static JsonSerializerOptions CamelCase
        {
            get
            {
                var options = _jsonOptions;
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
                var options = _jsonOptions;
                options.PropertyNamingPolicy = null;
                return options;
            }
        }
    }
}
