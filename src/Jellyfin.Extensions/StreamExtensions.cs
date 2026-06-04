using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="Stream"/> class.
    /// </summary>
    public static class StreamExtensions
    {
        private const int StreamComparisonBufferSize = 81920;

        /// <summary>
        /// Reads all lines in the <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">The <see cref="Stream" /> to read from.</param>
        /// <returns>All lines in the stream.</returns>
        public static string[] ReadAllLines(this Stream stream)
            => ReadAllLines(stream, Encoding.UTF8);

        /// <summary>
        /// Reads all lines in the <see cref="Stream" />.
        /// </summary>
        /// <param name="stream">The <see cref="Stream" /> to read from.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <returns>All lines in the stream.</returns>
        public static string[] ReadAllLines(this Stream stream, Encoding encoding)
        {
            using StreamReader reader = new StreamReader(stream, encoding);
            return ReadAllLines(reader).ToArray();
        }

        /// <summary>
        /// Reads all lines in the <see cref="TextReader" />.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader" /> to read from.</param>
        /// <returns>All lines in the stream.</returns>
        public static IEnumerable<string> ReadAllLines(this TextReader reader)
        {
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                yield return line;
            }
        }

        /// <summary>
        /// Reads all lines in the <see cref="TextReader" />.
        /// </summary>
        /// <param name="reader">The <see cref="TextReader" /> to read from.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>All lines in the stream.</returns>
        public static async IAsyncEnumerable<string> ReadAllLinesAsync(this TextReader reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
            {
                yield return line;
            }
        }

        /// <summary>
        /// Determines whether a stream is identical to a file on disk.
        /// </summary>
        /// <param name="stream">The stream to compare.</param>
        /// <param name="path">The file path to compare against.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>True if the stream and file are identical; otherwise false.</returns>
        /// <exception cref="ArgumentException"><paramref name="stream"/> does not support seeking.</exception>
        /// <remarks>
        /// The entire stream is compared against the file from the beginning (the position is reset to 0 on entry)
        /// and restored to its original value after the call.
        /// </remarks>
        public static async Task<bool> IsFileIdenticalAsync(this Stream stream, string path, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentException.ThrowIfNullOrEmpty(path);

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must support seeking.", nameof(stream));
            }

            var originalPosition = stream.Position;
            try
            {
                stream.Position = 0;

                var existingFileStream = new FileStream(
                    path,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: StreamComparisonBufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using (existingFileStream.ConfigureAwait(false))
                {
                    return await stream.IsStreamIdenticalAsync(existingFileStream, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        /// <summary>
        /// Determines whether two streams are identical.
        /// </summary>
        /// <param name="a">The first stream to compare.</param>
        /// <param name="b">The second stream to compare.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>True if the streams are identical; otherwise false.</returns>
        /// <remarks>
        /// Seekable streams are compared from the beginning (their position is reset to 0 on entry).
        /// Non-seekable streams are compared from their current read position. Stream positions are not
        /// restored after the call.
        /// </remarks>
        public static async Task<bool> IsStreamIdenticalAsync(this Stream a, Stream b, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(a);
            ArgumentNullException.ThrowIfNull(b);

            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (a.CanSeek is var aCanSeek && aCanSeek)
            {
                a.Position = 0;
            }

            if (b.CanSeek is var bCanSeek && bCanSeek)
            {
                b.Position = 0;
            }

            if (aCanSeek && bCanSeek && b.Length != a.Length)
            {
                return false;
            }

            // MemoryStreams only unlock a fast path if their underlying buffer is exposed via TryGetBuffer.
            var segmentA = a is MemoryStream streamA && streamA.TryGetBuffer(out var bufA) ? bufA : default;
            var segmentB = b is MemoryStream streamB && streamB.TryGetBuffer(out var bufB) ? bufB : default;

            // Fast path A: both streams expose buffers, compare segments directly
            if (segmentA.Array is not null && segmentB.Array is not null)
            {
                return segmentA.AsSpan().SequenceEqual(segmentB.AsSpan());
            }

            if (segmentB.Array is not null) // && segmentA.Array is null guaranteed by previous check
            {
                // swap so that segmentA is the non-null one, compared to b we need only one fast path B
                (segmentA, b) = (segmentB, a);
            }

            if (segmentA.Array is not null) // either a was non-null, or b was non-null and was swapped there
            {
                // Fast path B: only one stream exposed a buffer, compare against the other chunk-by-chunk
                var bufferB = ArrayPool<byte>.Shared.Rent(StreamComparisonBufferSize);
                try
                {
                    var memoryB = bufferB.AsMemory();
                    int offset = 0;
                    int bytesRead;
                    while ((bytesRead = await b.ReadAtLeastAsync(memoryB, memoryB.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        if (offset + bytesRead > segmentA.Count || !segmentA.AsSpan(offset, bytesRead).SequenceEqual(memoryB.Span[..bytesRead]))
                        {
                            return false;
                        }

                        offset += bytesRead;
                    }

                    return offset == segmentA.Count;
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bufferB);
                }
            }
            else
            {
                var bufferA = ArrayPool<byte>.Shared.Rent(StreamComparisonBufferSize);
                var bufferB = ArrayPool<byte>.Shared.Rent(StreamComparisonBufferSize);
                try
                {
                    var memoryA = bufferA.AsMemory();
                    var memoryB = bufferB.AsMemory();
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var taskA = a.ReadAtLeastAsync(memoryA, memoryA.Length, throwOnEndOfStream: false, cancellationToken).AsTask();
                        var taskB = b.ReadAtLeastAsync(memoryB, memoryB.Length, throwOnEndOfStream: false, cancellationToken).AsTask();
                        await Task.WhenAll(taskA, taskB).ConfigureAwait(false);

                        var bytesReadA = await taskA.ConfigureAwait(false);
                        var bytesReadB = await taskB.ConfigureAwait(false);

                        if (bytesReadA != bytesReadB)
                        {
                            return false;
                        }

                        if (bytesReadA == 0)
                        {
                            return true;
                        }

                        if (!memoryA.Span[..bytesReadA].SequenceEqual(memoryB.Span[..bytesReadB]))
                        {
                            return false;
                        }
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(bufferA);
                    ArrayPool<byte>.Shared.Return(bufferB);
                }
            }
        }
    }
}
