using System;
using System.IO;
using System.Text;

namespace MediaBrowser.Common.Logging
{
    /// <summary>
    /// Provides a Logger that can write to any Stream
    /// </summary>
    public class StreamLogger : BaseLogger
    {
        private Stream Stream { get; set; }

        public StreamLogger(Stream stream)
            : base()
        {
            Stream = stream;
        }

        protected override void LogEntry(LogRow row)
        {
            byte[] bytes = new UTF8Encoding().GetBytes(row.ToString() + Environment.NewLine);
            Stream.Write(bytes, 0, bytes.Length);
            Stream.Flush();
        }

        public override void Dispose()
        {
            base.Dispose();
            Stream.Dispose();
        }
    }
}
