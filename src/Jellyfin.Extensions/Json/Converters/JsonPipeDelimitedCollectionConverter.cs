namespace Jellyfin.Extensions.Json.Converters
{
    /// <summary>
    /// Convert Pipe delimited string to array of type.
    /// </summary>
    /// <typeparam name="T">Type to convert to.</typeparam>
    public sealed class JsonPipeDelimitedCollectionConverter<T> : JsonDelimitedCollectionConverter<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonPipeDelimitedCollectionConverter{T}"/> class.
        /// </summary>
        public JsonPipeDelimitedCollectionConverter() : base()
        {
        }

        /// <inheritdoc />
        protected override char Delimiter => '|';
    }
}
