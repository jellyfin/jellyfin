using System;
using System.Collections.Generic;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Local plugin class.
    /// </summary>
    public class LocalPlugin : IEquatable<LocalPlugin>
    {
        private readonly bool _supported;
        private Version? _version;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalPlugin"/> class.
        /// </summary>
        /// <param name="path">The plugin path.</param>
        /// <param name="isSupported"><b>True</b> if Jellyfin supports this version of the plugin.</param>
        /// <param name="manifest">The manifest record for this plugin, or null if one does not exist.</param>
        public LocalPlugin(string path, bool isSupported, PluginManifest manifest)
        {
            Path = path;
            DllFiles = Array.Empty<string>();
            _supported = isSupported;
            Manifest = manifest;
        }

        /// <summary>
        /// Gets the plugin id.
        /// </summary>
        public Guid Id => Manifest.Id;

        /// <summary>
        /// Gets the plugin name.
        /// </summary>
        public string Name => Manifest.Name;

        /// <summary>
        /// Gets the plugin version.
        /// </summary>
        public Version Version
        {
            get
            {
                if (_version is null)
                {
                    _version = Version.Parse(Manifest.Version);
                }

                return _version;
            }
        }

        /// <summary>
        /// Gets the plugin path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets or sets the list of dll files for this plugin.
        /// </summary>
        public IReadOnlyList<string> DllFiles { get; set; }

        /// <summary>
        /// Gets or sets the instance of this plugin.
        /// </summary>
        public IPlugin? Instance { get; set; }

        /// <summary>
        /// Gets a value indicating whether Jellyfin supports this version of the plugin, and it's enabled.
        /// </summary>
        public bool IsEnabledAndSupported => _supported && Manifest.Status >= PluginStatus.Active;

        /// <summary>
        /// Gets a value indicating whether the plugin has a manifest.
        /// </summary>
        public PluginManifest Manifest { get; }

        /// <summary>
        /// Compare two <see cref="LocalPlugin"/>.
        /// </summary>
        /// <param name="a">The first item.</param>
        /// <param name="b">The second item.</param>
        /// <returns>Comparison result.</returns>
        public static int Compare(LocalPlugin a, LocalPlugin b)
        {
            if (a is null || b is null)
            {
                throw new ArgumentNullException(a is null ? nameof(a) : nameof(b));
            }

            var compare = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);

            // Id is not equal but name is.
            if (!a.Id.Equals(b.Id) && compare == 0)
            {
                compare = a.Id.CompareTo(b.Id);
            }

            return compare == 0 ? a.Version.CompareTo(b.Version) : compare;
        }

        /// <summary>
        /// Returns the plugin information.
        /// </summary>
        /// <returns>A <see cref="PluginInfo"/> instance containing the information.</returns>
        public PluginInfo GetPluginInfo()
        {
            var inst = Instance?.GetPluginInfo() ?? new PluginInfo(Manifest.Name, Version, Manifest.Description, Manifest.Id, true);
            inst.Status = Manifest.Status;
            inst.HasImage = !string.IsNullOrEmpty(Manifest.ImagePath);
            return inst;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is LocalPlugin other && this.Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Name.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool Equals(LocalPlugin? other)
        {
            if (other is null)
            {
                return false;
            }

            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) && Id.Equals(other.Id) && Version.Equals(other.Version);
        }
    }
}
