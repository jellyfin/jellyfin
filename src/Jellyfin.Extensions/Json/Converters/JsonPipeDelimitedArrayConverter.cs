namespace Jellyfin.Extensions.Json.Converters
{
    /// <summary>
    /// Convert Pipe delimited string to array of type.
    /// </summary>
    /// <typeparam name="T">Type to convert to.</typeparam>
    public sealed class JsonPipeDelimitedArrayConverter<T> : JsonDelimitedArrayConverter<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonPipeDelimitedArrayConverter{T}"/> class.
        /// </summary>
        public JsonPipeDelimitedArrayConverter() : base()
        {
        }

        /// <inheritdoc />
        protected override char Delimiter => '|';
    }
}
