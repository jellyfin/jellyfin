using System;
using System.Buffers;
using System.Buffers.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Converts a GUID object or value to/from JSON.
    /// </summary>
    public class JsonInt32Converter : JsonConverter<int>
    {
        /// <inheritdoc />
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            static void ThrowFormatException() => throw new FormatException("Invalid format for an integer.");
            ReadOnlySpan<byte> span = stackalloc byte[0];

            if (reader.HasValueSequence)
            {
                long sequenceLength = reader.ValueSequence.Length;
                Span<byte> stackSpan = stackalloc byte[(int)sequenceLength];
                reader.ValueSequence.CopyTo(stackSpan);
                span = stackSpan;
            }
            else
            {
                span = reader.ValueSpan;
            }

            if (!Utf8Parser.TryParse(span, out int number, out _))
            {
                ThrowFormatException();
            }

            return number;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            static void ThrowInvalidOperationException() => throw new InvalidOperationException();
            Span<byte> span = stackalloc byte[16];
            if (Utf8Formatter.TryFormat(value, span, out int bytesWritten))
            {
                writer.WriteStringValue(span.Slice(0, bytesWritten));
            }

            ThrowInvalidOperationException();
        }
    }
}
