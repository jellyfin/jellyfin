using System;
using System.Linq;

namespace Emby.Server.Implementations
{
    public class StartupOptions
    {
        private readonly string[] _options;

        public StartupOptions(string[] commandLineArgs)
        {
            _options = commandLineArgs;
        }

        public bool ContainsOption(string option)
            => _options.Contains(option, StringComparer.OrdinalIgnoreCase);

        public string GetOption(string name)
        {
            int index = Array.IndexOf(_options, name);

            if (index == -1)
            {
                return null;
            }

            return _options.ElementAtOrDefault(index + 1);
        }
    }
}
