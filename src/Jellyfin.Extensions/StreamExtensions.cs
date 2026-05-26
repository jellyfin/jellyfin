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
    /// Class BaseExtensions.
    /// </summary>
    public static class StreamExtensions
    {
        private const int StreamComparisonBufferSize = 65536;

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
        public static async Task<bool> IsFileIdenticalAsync(this Stream stream, string path, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentException.ThrowIfNullOrEmpty(path);

            if (!stream.CanSeek)
            {
                return false;
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
        public static async Task<bool> IsStreamIdenticalAsync(this Stream a, Stream b, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(a);
            ArgumentNullException.ThrowIfNull(b);

            if (b.Length != a.Length)
            {
                return false;
            }

            var bufferA = ArrayPool<byte>.Shared.Rent(StreamComparisonBufferSize);
            var bufferB = ArrayPool<byte>.Shared.Rent(StreamComparisonBufferSize);
            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var bytesReadA = await a.ReadAsync(bufferA.AsMemory(), cancellationToken).ConfigureAwait(false);
                    var bytesReadB = await b.ReadAsync(bufferB.AsMemory(), cancellationToken).ConfigureAwait(false);

                    if (bytesReadA != bytesReadB)
                    {
                        return false;
                    }

                    if (bytesReadA == 0)
                    {
                        return true;
                    }

                    if (!bufferA.AsSpan(0, bytesReadA).SequenceEqual(bufferB.AsSpan(0, bytesReadB)))
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
