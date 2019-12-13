using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SkiaSharp;

namespace Jellyfin.Drawing.Skia
{
    /// <summary>
    /// Represents errors that occur during interaction with Skia codecs.
    /// </summary>
    [SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "A custom property, CodecResult, is required when creating this exception type.")]
    public class SkiaCodecException : SkiaException
    {
        /// <summary>
        /// Returns the non-successful codec result returned by Skia.
        /// </summary>
        /// <value>The non-successful codec result returned by Skia.</value>
        public SKCodecResult CodecResult { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkiaCodecException" /> class.
        /// </summary>
        /// <param name="result">The non-successful codec result returned by Skia.</param>
        public SkiaCodecException(SKCodecResult result) : base()
        {
            CodecResult = result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkiaCodecException" /> class
        /// with a specified error message.
        /// </summary>
        /// <param name="result">The non-successful codec result returned by Skia.</param>
        /// <param name="message">The message that describes the error.</param>
        public SkiaCodecException(SKCodecResult result, string message)
            : base(message)
        {
            CodecResult = result;
        }

        /// <inheritdoc />
        public override string ToString()
            => string.Format(
                CultureInfo.InvariantCulture,
                "Non-success codec result: {0}\n{1}",
                CodecResult,
                base.ToString());        
    }
}
