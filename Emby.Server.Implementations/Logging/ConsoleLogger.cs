using System;
using MediaBrowser.Model.Logging;

namespace Emby.Server.Implementations.Logging
{
    public class ConsoleLogger : IConsoleLogger
    {
        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
