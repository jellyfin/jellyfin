namespace Jellyfin.Extensions.Json.Converters
{
    /// <summary>
    /// Convert comma delimited string to array of type.
    /// </summary>
    /// <typeparam name="T">Type to convert to.</typeparam>
    public sealed class JsonCommaDelimitedArrayConverter<T> : JsonDelimitedArrayConverter<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonCommaDelimitedArrayConverter{T}"/> class.
        /// </summary>
        public JsonCommaDelimitedArrayConverter() : base()
        {
        }

        /// <inheritdoc />
        protected override char Delimiter => ',';
    }
}
