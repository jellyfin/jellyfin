using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.MediaEncoding.Encoder;

/// <summary>
/// Reads an image attachment from a seekable Matroska file without demuxing its video clusters.
/// Returns false for unsupported or malformed layouts so the caller can use FFmpeg instead.
/// </summary>
internal static class MatroskaAttachmentReader
{
    private const ulong SegmentId = 0x18538067;
    private const ulong AttachmentsId = 0x1941A469;
    private const ulong AttachedFileId = 0x61A7;
    private const ulong FileNameId = 0x466E;
    private const ulong FileMimeTypeId = 0x4660;
    private const ulong FileDataId = 0x465C;
    private const long MaxImageSize = 64L * 1024 * 1024;
    private const int MaxElementCount = 100_000;

    /// <summary>
    /// Attempts to copy the named attachment to a new file. The output is deleted on failure.
    /// </summary>
    internal static async Task<bool> TryExtractAsync(string sourcePath, string attachmentName, string outputPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(attachmentName))
        {
            return false;
        }

        await using var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 32768, FileOptions.Asynchronous);
        if (!TryLocateAttachment(input, attachmentName, out var offset, out var length, out var mimeType))
        {
            return false;
        }

        // Jellyfin expects a genuine encoded image rather than an arbitrary Matroska attachment.
        input.Position = offset;
        var signature = new byte[12];
        var signatureLength = await input.ReadAsync(signature, cancellationToken).ConfigureAwait(false);
        if (!IsSupportedImage(mimeType, signature.AsSpan(0, signatureLength)))
        {
            return false;
        }

        input.Position = offset;
        try
        {
            await using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 32768, FileOptions.Asynchronous);
            var buffer = new byte[32768];
            var remaining = length;
            while (remaining > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = await input.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellationToken).ConfigureAwait(false);
                if (count == 0)
                {
                    throw new EndOfStreamException("Matroska attachment ended unexpectedly.");
                }

                await output.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                remaining -= count;
            }

            return true;
        }
        catch
        {
            File.Delete(outputPath);
            throw;
        }
    }

    private static bool IsSupportedImage(string mimeType, ReadOnlySpan<byte> signature)
    {
        if (mimeType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return signature.Length >= 3 && signature[0] == 0xFF && signature[1] == 0xD8 && signature[2] == 0xFF;
        }

        if (mimeType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
        {
            return signature.Length >= 8 && signature[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        }

        if (mimeType.Equals("image/gif", StringComparison.OrdinalIgnoreCase))
        {
            return signature.Length >= 6 && signature[..4].SequenceEqual("GIF8"u8);
        }

        if (mimeType.Equals("image/webp", StringComparison.OrdinalIgnoreCase))
        {
            return signature.Length >= 12 && signature[..4].SequenceEqual("RIFF"u8) && signature.Slice(8, 4).SequenceEqual("WEBP"u8);
        }

        if (mimeType.Equals("image/bmp", StringComparison.OrdinalIgnoreCase) || mimeType.Equals("image/x-ms-bmp", StringComparison.OrdinalIgnoreCase))
        {
            return signature.Length >= 2 && signature[..2].SequenceEqual("BM"u8);
        }

        return false;
    }

    private static bool TryLocateAttachment(Stream stream, string attachmentName, out long offset, out long length, out string mimeType)
    {
        offset = 0;
        length = 0;
        mimeType = string.Empty;

        try
        {
            var remaining = MaxElementCount;
            while (remaining-- > 0 && TryReadHeader(stream, stream.Length, out var id, out var end, out _, allowUnknownSize: true))
            {
                if (id == SegmentId)
                {
                    while (remaining-- > 0 && TryReadHeader(stream, end, out var childId, out var childEnd, out _, allowUnknownSize: false))
                    {
                        if (childId == AttachmentsId && TryFindInAttachments(stream, childEnd, attachmentName, ref remaining, out offset, out length, out mimeType))
                        {
                            return true;
                        }

                        stream.Position = childEnd;
                    }

                    return false;
                }

                stream.Position = end;
            }
        }
        catch (IOException)
        {
            // Invalid/truncated Matroska data is handled by the FFmpeg fallback.
        }
        catch (ArgumentOutOfRangeException)
        {
            // Guard against invalid offsets in malformed containers.
        }

        return false;
    }

    private static bool TryFindInAttachments(Stream stream, long containerEnd, string requestedName, ref int remaining, out long offset, out long length, out string mimeType)
    {
        offset = 0;
        length = 0;
        mimeType = string.Empty;
        while (remaining-- > 0 && TryReadHeader(stream, containerEnd, out var id, out var end, out _, allowUnknownSize: false))
        {
            if (id == AttachedFileId && TryMatchAttachedFile(stream, end, requestedName, ref remaining, out offset, out length, out mimeType))
            {
                return true;
            }

            stream.Position = end;
        }

        return false;
    }

    private static bool TryMatchAttachedFile(Stream stream, long entryEnd, string requestedName, ref int remaining, out long offset, out long length, out string mimeType)
    {
        offset = 0;
        length = 0;
        mimeType = string.Empty;
        string? fileName = null;
        while (remaining-- > 0 && TryReadHeader(stream, entryEnd, out var id, out var end, out var size, allowUnknownSize: false))
        {
            if (id == FileNameId && size is > 0 and <= 4096)
            {
                var data = new byte[(int)size];
                stream.ReadExactly(data);
                fileName = Encoding.UTF8.GetString(data).TrimEnd('\0');
            }
            else if (id == FileMimeTypeId && size is > 0 and <= 128)
            {
                var data = new byte[(int)size];
                stream.ReadExactly(data);
                mimeType = Encoding.ASCII.GetString(data).TrimEnd('\0');
            }
            else if (id == FileDataId && size is > 0 and <= MaxImageSize)
            {
                offset = stream.Position;
                length = size;
            }

            stream.Position = end;
        }

        if (fileName is not null && fileName.Equals(requestedName, StringComparison.OrdinalIgnoreCase) && length > 0 && mimeType.Length > 0)
        {
            return true;
        }

        offset = 0;
        length = 0;
        return false;
    }

    private static bool TryReadHeader(Stream stream, long containerEnd, out ulong id, out long end, out long size, bool allowUnknownSize)
    {
        id = 0;
        end = 0;
        size = 0;
        if (stream.Position >= containerEnd)
        {
            return false;
        }

        var first = stream.ReadByte();
        if (first <= 0)
        {
            return false;
        }

        var idMask = 0x80;
        var idWidth = 1;
        while ((first & idMask) == 0 && idWidth <= 4)
        {
            idMask >>= 1;
            idWidth++;
        }

        if (idWidth > 4)
        {
            return false;
        }

        id = (ulong)first;
        for (var i = 1; i < idWidth; i++)
        {
            var next = stream.ReadByte();
            if (next < 0)
            {
                return false;
            }

            id = (id << 8) | (byte)next;
        }

        var sizeFirst = stream.ReadByte();
        if (sizeFirst <= 0)
        {
            return false;
        }

        var sizeMask = 0x80;
        var sizeWidth = 1;
        while ((sizeFirst & sizeMask) == 0 && sizeWidth <= 8)
        {
            sizeMask >>= 1;
            sizeWidth++;
        }

        if (sizeWidth > 8)
        {
            return false;
        }

        ulong value = (ulong)(sizeFirst & (sizeMask - 1));
        for (var i = 1; i < sizeWidth; i++)
        {
            var next = stream.ReadByte();
            if (next < 0)
            {
                return false;
            }

            value = (value << 8) | (byte)next;
        }

        var unknownSize = value == ((1UL << (sizeWidth * 7)) - 1);
        if (unknownSize && allowUnknownSize && id == SegmentId)
        {
            end = containerEnd;
        }
        else if (unknownSize || value > (ulong)(containerEnd - stream.Position))
        {
            return false;
        }
        else
        {
            end = stream.Position + (long)value;
        }

        size = end - stream.Position;
        return true;
    }
}
