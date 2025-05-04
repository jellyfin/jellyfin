using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Jellyfin.Extensions
{
    /// <summary>
    /// Class BaseExtensions.
    /// </summary>
    public static class StreamExtensions
    {
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
    }
}
