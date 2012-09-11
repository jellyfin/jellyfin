using System.Diagnostics;

namespace MediaBrowser.Common.Logging
{
    public class TraceLogger : BaseLogger
    {
        protected override void LogEntry(LogRow row)
        {
            Trace.WriteLine(row.ToString());
        }
    }
}
