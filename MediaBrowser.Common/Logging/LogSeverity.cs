using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Logging
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
