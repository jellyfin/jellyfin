using System;
using System.IO;

namespace MediaBrowser.Common.IO
{
    /// <summary>
    /// Measures progress when reading from a stream or writing to one
    /// </summary>
    public class ProgressStream : Stream
    {
        /// <summary>
        /// Gets the base stream.
        /// </summary>
        /// <value>The base stream.</value>
        public Stream BaseStream { get; private set; }

        /// <summary>
        /// Gets or sets the bytes processed.
        /// </summary>
        /// <value>The bytes processed.</value>
        private long BytesProcessed { get; set; }
        /// <summary>
        /// Gets or sets the length of the write.
        /// </summary>
        /// <value>The length of the write.</value>
        private long WriteLength { get; set; }

        /// <summary>
        /// Gets or sets the length of the read.
        /// </summary>
        /// <value>The length of the read.</value>
        private long? ReadLength { get; set; }

        /// <summary>
        /// Gets or sets the progress action.
        /// </summary>
        /// <value>The progress action.</value>
        private Action<double> ProgressAction { get; set; }

        /// <summary>
        /// Creates the read progress stream.
        /// </summary>
        /// <param name="baseStream">The base stream.</param>
        /// <param name="progressAction">The progress action.</param>
        /// <param name="readLength">Length of the read.</param>
        /// <returns>ProgressStream.</returns>
        public static ProgressStream CreateReadProgressStream(Stream baseStream, Action<double> progressAction, long? readLength = null)
        {
            return new ProgressStream
            {
                BaseStream = baseStream,
                ProgressAction = progressAction,
                ReadLength = readLength
            };
        }

        /// <summary>
        /// Creates the write progress stream.
        /// </summary>
        /// <param name="baseStream">The base stream.</param>
        /// <param name="progressAction">The progress action.</param>
        /// <param name="writeLength">Length of the write.</param>
        /// <returns>ProgressStream.</returns>
        public static ProgressStream CreateWriteProgressStream(Stream baseStream, Action<double> progressAction, long writeLength)
        {
            return new ProgressStream
            {
                BaseStream = baseStream,
                ProgressAction = progressAction,
                WriteLength = writeLength
            };
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <value><c>true</c> if this instance can read; otherwise, <c>false</c>.</value>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead
        {
            get { return BaseStream.CanRead; }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <value><c>true</c> if this instance can seek; otherwise, <c>false</c>.</value>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek
        {
            get { return BaseStream.CanSeek; }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <value><c>true</c> if this instance can write; otherwise, <c>false</c>.</value>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite
        {
            get { return BaseStream.CanWrite; }
        }

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            BaseStream.Flush();
        }

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <value>The length.</value>
        /// <returns>A long value representing the length of the stream in bytes.</returns>
        public override long Length
        {
            get { return BaseStream.Length; }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <value>The position.</value>
        /// <returns>The current position within the stream.</returns>
        public override long Position
        {
            get { return BaseStream.Position; }
            set
            {
                BaseStream.Position = value;
            }
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> - 1) replaced by the bytes read from the current source.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data read from the current stream.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = BaseStream.Read(buffer, offset, count);

            BytesProcessed += read;

            double percent = BytesProcessed;
            percent /= ReadLength ?? BaseStream.Length;
            percent *= 100;

            ProgressAction(percent);

            return read;
        }

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
        /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        public override void SetLength(long value)
        {
            BaseStream.SetLength(value);
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count" /> bytes from <paramref name="buffer" /> to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            BaseStream.Write(buffer, offset, count);

            BytesProcessed += count;

            double percent = BytesProcessed;
            percent /= WriteLength;
            percent *= 100;

            ProgressAction(percent);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.IO.Stream" /> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                BaseStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
