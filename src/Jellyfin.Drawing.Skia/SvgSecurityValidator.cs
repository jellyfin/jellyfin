using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;

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
    /// <param name="logger">The logger.</param>
    /// <returns><c>true</c> if the document is free of external references; otherwise <c>false</c>.</returns>
    public static bool IsSafe(string path, ILogger logger)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return IsSafe(stream, logger);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Unable to read SVG {Path} for validation, refusing to render", path);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unable to read SVG {Path} for validation, refusing to render", path);
            return false;
        }
    }

    /// <summary>
    /// Determines whether the SVG in the given stream is safe to rasterize.
    /// </summary>
    /// <param name="stream">The stream containing the SVG document.</param>
    /// <param name="logger">The logger.</param>
    /// <returns><c>true</c> if the document is free of external references; otherwise <c>false</c>.</returns>
    public static bool IsSafe(Stream stream, ILogger logger)
        => IsSafe(stream, logger, 0);

    private static bool IsSafe(Stream stream, ILogger logger, int depth)
    {
        try
        {
            using var reader = XmlReader.Create(stream, _scanSettings);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.DocumentType:
                        var subset = reader.Value;
                        if (!string.IsNullOrEmpty(subset)
                            && (subset.Contains("SYSTEM", StringComparison.OrdinalIgnoreCase)
                                || subset.Contains("PUBLIC", StringComparison.OrdinalIgnoreCase)))
                        {
                            logger.LogWarning("Refusing to render SVG declaring an external DTD entity");
                            return false;
                        }

                        break;

                    case XmlNodeType.Element when reader.HasAttributes:
                        for (var i = 0; i < reader.AttributeCount; i++)
                        {
                            reader.MoveToAttribute(i);
                            var value = reader.Value;
                            if (reader.LocalName.Equals("href", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!IsReferenceSafe(value, logger, depth))
                                {
                                    logger.LogWarning("Refusing to render SVG referencing external resource via href");
                                    return false;
                                }
                            }
                            else if (HasUnsafeCssReference(value, logger, depth))
                            {
                                logger.LogWarning("Refusing to render SVG referencing external resource via style/url()");
                                return false;
                            }
                        }

                        reader.MoveToElement();
                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        if (HasUnsafeCssReference(reader.Value, logger, depth))
                        {
                            logger.LogWarning("Refusing to render SVG referencing external resource in style block");
                            return false;
                        }

                        break;
                }
            }

            return true;
        }
        catch (XmlException ex)
        {
            // Malformed markup, a forbidden DTD construct or an unresolved external entity: refuse to render.
            logger.LogWarning(ex, "Refusing to render SVG that could not be safely parsed");
            return false;
        }
    }

    private static bool IsReferenceSafe(string? value, ILogger logger, int depth)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0 || trimmed[0] == '#')
        {
            return true;
        }

        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return IsDataUriSafe(trimmed, logger, depth);
        }

        return false;
    }

    private static bool IsDataUriSafe(string dataUri, ILogger logger, int depth)
    {
        // "data:[<mediatype>][;base64],<payload>" (mirrors Svg.Model's data URI parsing).
        var comma = dataUri.IndexOf(',', StringComparison.Ordinal);
        if (comma < 0)
        {
            return false;
        }

        var header = dataUri[5..comma];
        var segments = header.Split(';');
        var mediaType = segments.Length > 0 && segments[0].Contains('/', StringComparison.Ordinal)
            ? segments[0].Trim()
            : "text/plain";

        // Only "image/svg+xml" is re-parsed as SVG by the renderer; any other type is treated as raster data.
        if (!mediaType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (depth >= MaxDataUriDepth)
        {
            logger.LogWarning("Refusing to render SVG with nested data URIs exceeding the allowed depth");
            return false;
        }

        var isBase64 = segments.Length > 0 && segments[^1].Trim().Equals("base64", StringComparison.OrdinalIgnoreCase);
        try
        {
            var payload = dataUri[(comma + 1)..];
            byte[] bytes = isBase64
                ? Convert.FromBase64String(payload.Trim())
                : Encoding.UTF8.GetBytes(Uri.UnescapeDataString(payload));

            if (bytes.Length > 2 && bytes[0] == 0x1F && bytes[1] == 0x8B)
            {
                bytes = Decompress(bytes);
            }

            using var ms = new MemoryStream(bytes, false);
            return IsSafe(ms, logger, depth + 1);
        }
        catch (FormatException ex)
        {
            logger.LogWarning(ex, "Refusing to render SVG with an undecodable data URI");
            return false;
        }
        catch (InvalidDataException ex)
        {
            logger.LogWarning(ex, "Refusing to render SVG with an invalid compressed data URI");
            return false;
        }
    }

    private static byte[] Decompress(byte[] compressed)
    {
        using var input = new MemoryStream(compressed, false);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        var buffer = new byte[81920];
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

        return output.ToArray();
    }

    private static bool HasUnsafeCssReference(string? value, ILogger logger, int depth)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var span = value.AsSpan();
        var index = 0;
        while (true)
        {
            var found = span[index..].IndexOf("url(", StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                break;
            }

            var start = index + found + 4;
            var close = span[start..].IndexOf(')');
            if (close < 0)
            {
                break;
            }

            var target = span.Slice(start, close).Trim();
            target = target.Trim('\'');
            target = target.Trim('"').Trim();
            if (!IsReferenceSafe(target.ToString(), logger, depth))
            {
                return true;
            }

            index = start + close + 1;
            if (index >= span.Length)
            {
                break;
            }
        }

        // Handle the bare "@import '...';" form (the "@import url(...)" form is covered above).
        index = 0;
        while (true)
        {
            var found = span[index..].IndexOf("@import", StringComparison.OrdinalIgnoreCase);
            if (found < 0)
            {
                break;
            }

            var rest = span[(index + found + 7)..];
            var quote = rest.IndexOfAny('\'', '"');
            if (quote >= 0)
            {
                var afterQuote = rest[(quote + 1)..];
                var end = afterQuote.IndexOfAny('\'', '"');
                if (end >= 0 && !IsReferenceSafe(afterQuote[..end].Trim().ToString(), logger, depth))
                {
                    return true;
                }
            }

            index = index + found + 7;
            if (index >= span.Length)
            {
                break;
            }
        }

        return false;
    }
}
