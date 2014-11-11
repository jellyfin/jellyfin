using MediaBrowser.Controller.Configuration;
using System;

namespace MediaBrowser.Server.Startup.Common.Migrations
{
    public class RenameXbmcOptions
    {
        private readonly IServerConfigurationManager _config;

        public RenameXbmcOptions(IServerConfigurationManager config)
        {
            _config = config;
        }

        public void Run()
        {
            var changed = false;

            foreach (var option in _config.Configuration.MetadataOptions)
            {
                if (Migrate(option.DisabledMetadataSavers))
                {
                    changed = true;
                }
                if (Migrate(option.LocalMetadataReaderOrder))
                {
                    changed = true;
                }
            }

            if (changed)
            {
                _config.SaveConfiguration();
            }
        }

        private bool Migrate(string[] options)
        {
            var changed = false;

            if (options != null)
            {
                for (var i = 0; i < options.Length; i++)
                {
                    if (string.Equals(options[i], "Xbmc Nfo", StringComparison.OrdinalIgnoreCase))
                    {
                        options[i] = "Nfo";
                        changed = true;
                    }
                }
            }

            return changed;
        }
    }
}
