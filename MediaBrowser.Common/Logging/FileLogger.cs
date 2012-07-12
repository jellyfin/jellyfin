using System;
using System.IO;
using System.Text;

namespace MediaBrowser.Common.Logging
{
    public class FileLogger : BaseLogger, IDisposable
    {
        private string LogDirectory { get; set; }
        private string CurrentLogFile { get; set; }

        private FileStream FileStream { get; set; }

        public FileLogger(string logDirectory)
        {
            LogDirectory = logDirectory;
        }

        private void EnsureStream()
        {
            if (FileStream == null)
            {
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                DateTime now = DateTime.Now;

                CurrentLogFile = Path.Combine(LogDirectory, now.ToString("dMyyyy") + "-" + now.Ticks + ".log");

                FileStream = new FileStream(CurrentLogFile, FileMode.Append, FileAccess.Write, FileShare.Read);
            }
        }

        protected override void LogEntry(LogRow row)
        {
            EnsureStream();

            byte[] bytes = new UTF8Encoding().GetBytes(row.ToString() + Environment.NewLine);

            FileStream.Write(bytes, 0, bytes.Length);

            FileStream.Flush();
        }

        public void Dispose()
        {
            if (FileStream != null)
            {
                FileStream.Dispose();
            }
        }
    }
}
