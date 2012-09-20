using MediaBrowser.Common.Kernel;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;

namespace MediaBrowser.Common.Logging
{
    [Export(typeof(BaseLogger))]
    public class TraceFileLogger : BaseLogger
    {
        private TraceListener Listener { get; set; }

        public override void Initialize(IKernel kernel)
        {
            DateTime now = DateTime.Now;

            string logFilePath = Path.Combine(kernel.ApplicationPaths.LogDirectoryPath, "log-" + now.ToString("dMyyyy") + "-" + now.Ticks + ".log");

            Listener = new TextWriterTraceListener(logFilePath);
            Trace.Listeners.Add(Listener);
            Trace.AutoFlush = true;
        }

        public override void Dispose()
        {
            base.Dispose();

            Trace.Listeners.Remove(Listener);
            Listener.Dispose();
        }

        public override void LogEntry(LogRow row)
        {
            Trace.WriteLine(row.ToString());
        }
    }
}
