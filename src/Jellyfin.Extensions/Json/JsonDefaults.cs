using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Jellyfin.Extensions.Json.Converters;

namespace Jellyfin.Extensions.Json
{
    /// <summary>
    /// Helper class for having compatible JSON throughout the codebase.
    /// </summary>
    public static class JsonDefaults
    {
        /// <summary>
        /// Pascal case json profile media type.
        /// </summary>
        public const string PascalCaseMediaType = "application/json; profile=\"PascalCase\"";

        /// <summary>
        /// Camel case json profile media type.
        /// </summary>
        public const string CamelCaseMediaType = "application/json; profile=\"CamelCase\"";

        /// <summary>
        /// When changing these options, update
        ///  Jellyfin.Server/Extensions/ApiServiceCollectionExtensions.cs
        ///   -> AddJellyfinApi
        ///    -> AddJsonOptions.
        /// </summary>
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            ReadCommentHandling = JsonCommentHandling.Disallow,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters =
            {
                new JsonGuidConverter(),
                new JsonNullableGuidConverter(),
                new JsonVersionConverter(),
                new JsonFlagEnumConverterFactory(),
                new JsonDefaultStringEnumConverterFactory(),
                new JsonStringEnumConverter(),
                new JsonNullableStructConverterFactory(),
                new JsonDateTimeConverter(),
                new JsonStringConverter()
            },
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        private static readonly JsonSerializerOptions _pascalCaseJsonSerializerOptions = new(_jsonSerializerOptions)
        {
            PropertyNamingPolicy = null
        };

        private static readonly JsonSerializerOptions _camelCaseJsonSerializerOptions = new(_jsonSerializerOptions)
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Gets the default <see cref="JsonSerializerOptions" /> options.
        /// </summary>
        /// <remarks>
        /// The return value must not be modified.
        /// If the defaults must be modified the author must use the copy constructor.
        /// </remarks>
        /// <returns>The default <see cref="JsonSerializerOptions" /> options.</returns>
        public static JsonSerializerOptions Options
            => _jsonSerializerOptions;

        /// <summary>
        /// Gets camelCase json options.
        /// </summary>
        /// <remarks>
        /// The return value must not be modified.
        /// If the defaults must be modified the author must use the copy constructor.
        /// </remarks>
        /// <returns>The camelCase <see cref="JsonSerializerOptions" /> options.</returns>
        public static JsonSerializerOptions CamelCaseOptions
            => _camelCaseJsonSerializerOptions;

        /// <summary>
        /// Gets PascalCase json options.
        /// </summary>
        /// <remarks>
        /// The return value must not be modified.
        /// If the defaults must be modified the author must use the copy constructor.
        /// </remarks>
        /// <returns>The PascalCase <see cref="JsonSerializerOptions" /> options.</returns>
        public static JsonSerializerOptions PascalCaseOptions
            => _pascalCaseJsonSerializerOptions;
    }
}
