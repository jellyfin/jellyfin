using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace Emby.Server.Core.Logging
{
    public class ConsoleLogger : IConsoleLogger
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
