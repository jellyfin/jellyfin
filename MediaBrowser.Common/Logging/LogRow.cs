using System;
using System.Collections.Generic;

namespace MediaBrowser.Common.Logging
{
    public struct LogRow
    {
        const string TimePattern = "h:mm:ss.fff tt d/M/yyyy";
        
        public LogSeverity Severity { get; set; }
        public string Message { get; set; }
        public int ThreadId { get; set; }
        public string ThreadName { get; set; }
        public DateTime Time { get; set; }

        public override string ToString()
        {
            var data = new List<string>();

            data.Add(Time.ToString(TimePattern));

            data.Add(Severity.ToString());

            if (!string.IsNullOrEmpty(Message))
            {
                data.Add(Encode(Message));
            }

            data.Add(ThreadId.ToString());

            if (!string.IsNullOrEmpty(ThreadName))
            {
                data.Add(Encode(ThreadName));
            }

            return string.Join(" , ", data.ToArray());
        }

        private string Encode(string str)
        {
            return (str ?? "").Replace(",", ",,").Replace(Environment.NewLine, " [n] ");
        }
    }
}
