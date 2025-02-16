namespace Jellyfin.Extensions.Json.Converters
{
    /// <summary>
    /// Convert comma delimited string to collection of type.
    /// </summary>
    /// <typeparam name="T">Type to convert to.</typeparam>
    public sealed class JsonCommaDelimitedCollectionConverter<T> : JsonDelimitedCollectionConverter<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonCommaDelimitedCollectionConverter{T}"/> class.
        /// </summary>
        public JsonCommaDelimitedCollectionConverter() : base()
        {
        }

        /// <inheritdoc />
        protected override char Delimiter => ',';
    }
}
