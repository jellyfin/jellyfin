#nullable disable

#pragma warning disable CS1591

using MediaBrowser.Common.Configuration;

namespace MediaBrowser.Controller
{
    public interface IServerApplicationPaths : IApplicationPaths
    {
        /// <summary>
        /// Gets the path to the base root media directory.
        /// </summary>
        /// <value>The root folder path.</value>
        string RootFolderPath { get; }

        /// <summary>
        /// Gets the path to the default user view directory.  Used if no specific user view is defined.
        /// </summary>
        /// <value>The default user views path.</value>
        string DefaultUserViewsPath { get; }

        /// <summary>
        /// Gets the path to the People directory.
        /// </summary>
        /// <value>The people path.</value>
        string PeoplePath { get; }

        /// <summary>
        /// Gets the path to the Genre directory.
        /// </summary>
        /// <value>The genre path.</value>
        string GenrePath { get; }

        /// <summary>
        /// Gets the music genre path.
        /// </summary>
        /// <value>The music genre path.</value>
        string MusicGenrePath { get; }

        /// <summary>
        /// Gets the path to the Studio directory.
        /// </summary>
        /// <value>The studio path.</value>
        string StudioPath { get; }

        /// <summary>
        /// Gets the path to the Year directory.
        /// </summary>
        /// <value>The year path.</value>
        string YearPath { get; }

        /// <summary>
        /// Gets the path to the user configuration directory.
        /// </summary>
        /// <value>The user configuration directory path.</value>
        string UserConfigurationDirectoryPath { get; }

        /// <summary>
        /// Gets the default internal metadata path.
        /// </summary>
        string DefaultInternalMetadataPath { get; }

        /// <summary>
        /// Gets the internal metadata path, either a custom path or the default.
        /// </summary>
        /// <value>The internal metadata path.</value>
        string InternalMetadataPath { get; }

        /// <summary>
        /// Gets the virtual internal metadata path, either a custom path or the default.
        /// </summary>
        /// <value>The virtual internal metadata path.</value>
        string VirtualInternalMetadataPath { get; }

        /// <summary>
        /// Gets the path to the artists directory.
        /// </summary>
        /// <value>The artists path.</value>
        string ArtistsPath { get; }
    }
}
