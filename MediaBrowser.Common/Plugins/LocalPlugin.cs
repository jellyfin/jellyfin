using System;
using System.Collections.Generic;
using System.Globalization;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Local plugin class.
    /// </summary>
    public class LocalPlugin : IEquatable<LocalPlugin>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalPlugin"/> class.
        /// </summary>
        /// <param name="id">The plugin id.</param>
        /// <param name="name">The plugin name.</param>
        /// <param name="version">The plugin version.</param>
        /// <param name="path">The plugin path.</param>
        public LocalPlugin(Guid id, string name, Version version, string path)
        {
            Id = id;
            Name = name;
            Version = version;
            Path = path;
            DllFiles = new List<string>();
        }

        /// <summary>
        /// Gets the plugin id.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the plugin name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the plugin version.
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Gets the plugin path.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the list of dll files for this plugin.
        /// </summary>
        public List<string> DllFiles { get; }

        /// <summary>
        /// == operator.
        /// </summary>
        /// <param name="left">Left item.</param>
        /// <param name="right">Right item.</param>
        /// <returns>Comparison result.</returns>
        public static bool operator ==(LocalPlugin left, LocalPlugin right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// != operator.
        /// </summary>
        /// <param name="left">Left item.</param>
        /// <param name="right">Right item.</param>
        /// <returns>Comparison result.</returns>
        public static bool operator !=(LocalPlugin left, LocalPlugin right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Compare two <see cref="LocalPlugin"/>.
        /// </summary>
        /// <param name="a">The first item.</param>
        /// <param name="b">The second item.</param>
        /// <returns>Comparison result.</returns>
        public static int Compare(LocalPlugin a, LocalPlugin b)
        {
            var compare = string.Compare(a.Name, b.Name, true, CultureInfo.InvariantCulture);

            // Id is not equal but name is.
            if (a.Id != b.Id && compare == 0)
            {
                compare = a.Id.CompareTo(b.Id);
            }

            return compare == 0 ? a.Version.CompareTo(b.Version) : compare;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj != null && obj is LocalPlugin other && this.Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Name.GetHashCode(StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool Equals(LocalPlugin? other)
        {
            // Do not use == or != for comparison as this class overrides the operators.
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            return string.Equals(Name, other?.Name, StringComparison.OrdinalIgnoreCase)
                   && Equals(Id, other?.Id);
        }
    }
}
