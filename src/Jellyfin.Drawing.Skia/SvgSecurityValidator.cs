using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

[assembly: InternalsVisibleTo("Jellyfin.Drawing.Skia.Tests")]

namespace Jellyfin.Drawing.Skia;

/// <summary>
/// Validates that an SVG document does not reference external resources before it is rasterized.
/// </summary>
internal static class SvgSecurityValidator
{
    // Guards against a chain of nested data:image/svg+xml payloads.
    private const int MaxDataUriDepth = 4;

    // Upper bound for a decompressed svgz payload carried inside a data URI, to guard against decompression bombs.
    private const int MaxDecompressedBytes = 16 * 1024 * 1024;

    private const int DecompressBufferSize = 81920;

    private static readonly XmlReaderSettings _scanSettings = new()
    {
        DtdProcessing = DtdProcessing.Parse,
        XmlResolver = null,
        MaxCharactersFromEntities = 1024 * 1024,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
        IgnoreWhitespace = true,
        CloseInput = false
    };

    /// <summary>
    /// Determines whether the SVG at the given path is safe to rasterize, i.e. contains no references
    /// to external resources.
    /// </summary>
    /// <param name="path">The path to the SVG file.</param>
    /// <param name="reason">When this method returns <c>false</c>, the reason the document was rejected.</param>
    /// <returns><c>true</c> if the document is free of external references; otherwise <c>false</c>.</returns>
    public static bool IsSafe(string path, [NotNullWhen(false)] out string? reason)
    {
        try
        {
            using var stream = File.OpenRead(path);
            reason = Validate(stream, 0);
        }
        catch (IOException ex)
        {
            reason = "Unable to read the file for validation: " + ex.Message;
        }
        catch (UnauthorizedAccessException ex)
        {
            reason = "Unable to read the file for validation: " + ex.Message;
        }

        return reason is null;
    }

    /// <summary>
    /// Determines whether the SVG in the given stream is safe to rasterize.
    /// </summary>
    /// <param name="stream">The stream containing the SVG document.</param>
    /// <param name="reason">When this method returns <c>false</c>, the reason the document was rejected.</param>
    /// <returns><c>true</c> if the document is free of external references; otherwise <c>false</c>.</returns>
    public static bool IsSafe(Stream stream, [NotNullWhen(false)] out string? reason)
    {
        reason = Validate(stream, 0);
        return reason is null;
    }

    private static string? Validate(Stream stream, int depth)
    {
        try
        {
            using var reader = XmlReader.Create(stream, _scanSettings);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.DocumentType:
                        {
                            var subset = reader.Value;
                            if (!string.IsNullOrEmpty(subset)
                                && (subset.Contains("SYSTEM", StringComparison.OrdinalIgnoreCase)
                                    || subset.Contains("PUBLIC", StringComparison.OrdinalIgnoreCase)))
                            {
                                return "The document declares an external DTD entity";
                            }

                            break;
                        }

                    case XmlNodeType.Element when reader.HasAttributes:
                        {
                            for (var i = 0; i < reader.AttributeCount; i++)
                            {
                                reader.MoveToAttribute(i);
                                var isHref = reader.LocalName.Equals("href", StringComparison.OrdinalIgnoreCase);
                                var reason = isHref
                                    ? ValidateReference(reader.Value, depth, "href")
                                    : ValidateCss(reader.Value, depth);
                                if (reason is not null)
                                {
                                    return reason;
                                }
                            }

                            reader.MoveToElement();
                            break;
                        }

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        {
                            var reason = ValidateCss(reader.Value, depth);
                            if (reason is not null)
                            {
                                return reason;
                            }

                            break;
                        }
                }
            }

            return null;
        }
        catch (XmlException ex)
        {
            // Malformed markup, a forbidden DTD construct or an unresolved external entity: refuse to render.
            return "The document could not be safely parsed: " + ex.Message;
        }
    }

    private static string? ValidateReference(ReadOnlySpan<char> value, int depth, string context)
    {
        var trimmed = value.Trim();
        if (trimmed.IsEmpty || trimmed[0] == '#')
        {
            return null;
        }

        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return ValidateDataUri(trimmed, depth, context);
        }

        return "An external resource is referenced via " + context;
    }

    private static string? ValidateDataUri(ReadOnlySpan<char> dataUri, int depth, string context)
    {
        // "data:[<mediatype>][;base64],<payload>" (mirrors Svg.Model's data URI parsing).
        var comma = dataUri.IndexOf(',');
        if (comma < 0)
        {
            return "A malformed data URI is referenced via " + context;
        }

        var header = dataUri[5..comma];
        var firstSeparator = header.IndexOf(';');
        var mediaType = (firstSeparator < 0 ? header : header[..firstSeparator]).Trim();

        // Only "image/svg+xml" is re-parsed as SVG by the renderer; any other type is treated as raster data.
        if (!mediaType.Contains('/') || !mediaType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (depth >= MaxDataUriDepth)
        {
            return "Nested data URIs exceed the allowed depth";
        }

        var lastSeparator = header.LastIndexOf(';');
        var isBase64 = lastSeparator >= 0
            && header[(lastSeparator + 1)..].Trim().Equals("base64", StringComparison.OrdinalIgnoreCase);

        var payload = dataUri[(comma + 1)..].Trim();
        byte[]? buffer = null;
        try
        {
            int length;
            if (isBase64)
            {
                buffer = ArrayPool<byte>.Shared.Rent((payload.Length / 4 * 3) + 3);
                if (!Convert.TryFromBase64Chars(payload, buffer, out length))
                {
                    return "An undecodable data URI is referenced via " + context;
                }
            }
            else
            {
                var unescaped = Uri.UnescapeDataString(payload.ToString());
                buffer = ArrayPool<byte>.Shared.Rent(Encoding.UTF8.GetMaxByteCount(unescaped.Length));
                length = Encoding.UTF8.GetBytes(unescaped, buffer);
            }

            if (length > 2 && buffer[0] == 0x1F && buffer[1] == 0x8B)
            {
                using var decompressed = Decompress(buffer, length);
                return Validate(decompressed, depth + 1);
            }

            using var stream = new MemoryStream(buffer, 0, length, false);
            return Validate(stream, depth + 1);
        }
        catch (FormatException ex)
        {
            return "An undecodable data URI is referenced via " + context + ": " + ex.Message;
        }
        catch (InvalidDataException ex)
        {
            return "An invalid compressed data URI is referenced via " + context + ": " + ex.Message;
        }
        finally
        {
            if (buffer is not null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }

    private static MemoryStream Decompress(byte[] compressed, int length)
    {
        using var input = new MemoryStream(compressed, 0, length, false);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        var output = new MemoryStream();
        var buffer = ArrayPool<byte>.Shared.Rent(DecompressBufferSize);
        try
        {
            var total = 0;
            int read;
            while ((read = gzip.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += read;
                if (total > MaxDecompressedBytes)
                {
                    throw new InvalidDataException("Compressed data URI exceeds the allowed size");
                }

                output.Write(buffer, 0, read);
            }
        }
        catch
        {
            output.Dispose();
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        output.Position = 0;
        return output;
    }

    private static string? ValidateCss(ReadOnlySpan<char> value, int depth)
    {
        if (value.IsEmpty)
        {
            return null;
        }

        var index = 0;
        while (true)
        {
            var found = value[index..].IndexOf("url(", StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                break;
            }

            var start = index + found + 4;
            var close = value[start..].IndexOf(')');
            if (close < 0)
            {
                break;
            }

            var target = value.Slice(start, close).Trim();
            target = target.Trim('\'');
            target = target.Trim('"').Trim();
            var reason = ValidateReference(target, depth, "url()");
            if (reason is not null)
            {
                return reason;
            }

            index = start + close + 1;
            if (index >= value.Length)
            {
                break;
            }
        }

        // Handle the bare "@import '...';" form (the "@import url(...)" form is covered above).
        index = 0;
        while (true)
        {
            var found = value[index..].IndexOf("@import", StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                break;
            }

            var rest = value[(index + found + 7)..];
            var quote = rest.IndexOfAny('\'', '"');
            if (quote >= 0)
            {
                var afterQuote = rest[(quote + 1)..];
                var end = afterQuote.IndexOfAny('\'', '"');
                if (end >= 0)
                {
                    var reason = ValidateReference(afterQuote[..end], depth, "@import");
                    if (reason is not null)
                    {
                        return reason;
                    }
                }
            }

            index = index + found + 7;
            if (index >= value.Length)
            {
                break;
            }
        }

        return null;
    }
}
