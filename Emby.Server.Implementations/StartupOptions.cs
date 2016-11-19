using System;
using System.Collections.Generic;
using System.Linq;

namespace Emby.Server.Implementations
{
    public class StartupOptions
    {
        private readonly List<string> _options;

        public StartupOptions(string[] commandLineArgs)
        {
            _options = commandLineArgs.ToList();
        }

        public bool ContainsOption(string option)
        {
            return _options.Contains(option, StringComparer.OrdinalIgnoreCase);
        }

        public string GetOption(string name)
        {
            var index = _options.IndexOf(name);

            if (index != -1)
            {
                return _options.ElementAtOrDefault(index + 1);
            }

            return null;
        }
    }
}
