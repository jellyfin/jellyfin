using System.IO;
using CommandLine;

namespace Jellyfin.Cli.Options
{
    /// <summary>
    /// Class used by CommandLine package when parsing the wizard setup command line arguments.
    /// </summary>
    [Verb("wizard", HelpText="Run the server's initial setup.")]
    public class Wizard
    {
        private string? _password;

        /// <summary>
        /// Gets or sets a value indicating whether to write the configuration.
        /// </summary>
        /// <value>Write configuration.</value>
        [Option("write", Required = false, Default = false, HelpText = "Write configuration.")]
        public bool Write { get; set; }

        /// <summary>
        /// Gets or sets the path to the data directory.
        /// </summary>
        /// <value>The path to the data directory.</value>
        [Option("datadir", Required = false, HelpText = "Path to use for the data folder (database files, etc.).")]
        public string? DataDir { get; set; }

        /// <summary>
        /// Gets or sets the path to the config directory.
        /// </summary>
        /// <value>The path to the config directory.</value>
        [Option("configdir", Required = false, HelpText = "Path to use for configuration data (user settings and pictures).")]
        public string? ConfigDir { get; set; }

        /// <summary>
        /// Gets or sets the path to the cache directory.
        /// </summary>
        /// <value>The path to the cache directory.</value>
        [Option('C', "cachedir", Required = false, HelpText = "Path to use for caching.")]
        public string? CacheDir { get; set; }

        /// <summary>
        /// Gets or sets the path to the log directory.
        /// </summary>
        /// <value>The path to the log directory.</value>
        [Option('l', "logdir", Required = false, HelpText = "Path to use for writing log files.")]
        public string? LogDir { get; set; }

        /// <summary>
        /// Gets or sets the username of the initial admin user.
        /// </summary>
        /// <value>The username.</value>
        [Option("username", Required = false, HelpText = "Username of the initial admin user.")]
        public string? Username { get; set; }

        /// <summary>
        /// Gets or sets the path to the password file containing the password of the initial admin user.
        /// </summary>
        /// <value>The path to the file containing the password.</value>
        [Option("password-file", Required = false, HelpText = "Path to a file containing the initial admin user password.")]
        public string? PasswordFile { get; set; }

        /// <summary>
        /// Gets or sets the password of the initial admin user.
        /// </summary>
        /// <value>The password contained in the <see cref="Wizard.PasswordFile" /> field.</value>
        public string? Password
        {
            get
            {
                if (_password is null && PasswordFile is not null)
                {
                    _password = File.ReadAllText(PasswordFile);
                }

                return _password;
            }

            set
            {
                _password = value;
            }
        }

        /// <summary>
        /// Gets or sets the preferred display language.
        /// </summary>
        /// <value>The preferred display language.</value>
        [Option("preferred-display-language", Required = false, HelpText = "Preferred display language.")]
        public string? PreferredDisplayLanguage { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata language.
        /// </summary>
        /// <value>The preferred metadata language.</value>
        [Option("preferred-metadata-language", Required = false, HelpText = "Preferred metadata language.")]
        public string? PreferredMetadataLanguage { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata country region.
        /// </summary>
        /// <value>The preferred metadata country region.</value>
        [Option("preferred-metadata-country-region", Required = false, HelpText = "Preferred metadata country/region.")]
        public string? PreferredMetadataCountryRegion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether remote connections are allowed or not.
        /// </summary>
        /// <value>If remote connections are allowed or not.</value>
        [Option("enable-remote-access", Required = false, HelpText = "Enable remote connections to the server. Not needed if behind a reverse proxy.")]
        public bool? EnableRemoteAccess { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether automatic port mapping is required.
        /// </summary>
        /// <value>If automatic port mapping is required.</value>
        [Option("enable-automatic-port-mapping", Required = false, HelpText = "Enable automatic port mapping.")]
        public bool? EnableAutomaticPortMapping { get; set; }
    }
}
