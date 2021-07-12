using System;
using System.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Updates;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Migration to initialize system configuration with the default plugin repository.
    /// </summary>
    public class RemoveSSDPSettings : IMigrationRoutine
    {
        private readonly IServerApplicationPaths _paths;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveSSDPSettings"/> class.
        /// </summary>
        /// <param name="paths">Instance of the <see cref="IServerApplicationPaths"/> interface.</param>
        public RemoveSSDPSettings(IServerApplicationPaths paths)
        {
            _paths = paths;
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("02280C08-535E-48F0-90C7-79082363164D");

        /// <inheritdoc/>
        public string Name => "RemoveSSDPSettings";

        /// <inheritdoc/>
        public bool PerformOnNewInstall => false;

        /// <inheritdoc/>
        public void Perform()
        {
            // rename dlna.xml to dlna.bak
            var sourcePath = Path.Combine(_paths.ConfigurationDirectoryPath, "dlna.");
            var source = sourcePath + "xml";
            if (File.Exists(source))
            {
                try
                {
                    var currentFile = new FileInfo(source);
                    currentFile.MoveTo(sourcePath + ".bak");
                }
                catch
                {
                }
            }
        }
    }
}
