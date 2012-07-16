using System;

namespace MediaBrowser.Model.Logging
{
    [Flags]
    public enum LogSeverity
    {
        None = 0,
        Debug = 1,
        Info = 2,
        Warning = 4,
        Error = 8
    }
}
