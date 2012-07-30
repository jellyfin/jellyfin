using System;
using System.Text;

namespace MediaBrowser.Logging
{
    public struct LogRow
    {
        const string TimePattern = "h:mm:ss.fff tt d/M/yyyy";
        
        public LogSeverity Severity { get; set; }
        public string Message { get; set; }
        public string Category { get; set; }
        public int ThreadId { get; set; }
        public string ThreadName { get; set; }
        public DateTime Time { get; set; }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Time.ToString(TimePattern))
                .Append(" , ")
                .Append(Enum.GetName(typeof(LogSeverity), Severity))
                .Append(" , ")
                .Append(Encode(Message))
                .Append(" , ")
                .Append(Encode(Category))
                .Append(" , ")
                .Append(ThreadId)
                .Append(" , ")
                .Append(Encode(ThreadName));

            return builder.ToString();
        }

        private string Encode(string str)
        {
            return (str ?? "").Replace(",", ",,").Replace(Environment.NewLine, " [n] ");
        }
    }
}
