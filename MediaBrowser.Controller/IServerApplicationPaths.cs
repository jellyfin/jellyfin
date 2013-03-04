using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Kernel;

namespace MediaBrowser.Controller
{
    public interface IServerApplicationPaths : IApplicationPaths
    {
        /// <summary>
        /// Gets the path to the base root media directory
        /// </summary>
        /// <value>The root folder path.</value>
        string RootFolderPath { get; }

        /// <summary>
        /// Gets the path to the default user view directory.  Used if no specific user view is defined.
        /// </summary>
        /// <value>The default user views path.</value>
        string DefaultUserViewsPath { get; }

        /// <summary>
        /// Gets the path to localization data.
        /// </summary>
        /// <value>The localization path.</value>
        string LocalizationPath { get; }

        /// <summary>
        /// Gets the path to the Images By Name directory
        /// </summary>
        /// <value>The images by name path.</value>
        string ImagesByNamePath { get; }

        /// <summary>
        /// Gets the path to the People directory
        /// </summary>
        /// <value>The people path.</value>
        string PeoplePath { get; }

        /// <summary>
        /// Gets the path to the Genre directory
        /// </summary>
        /// <value>The genre path.</value>
        string GenrePath { get; }

        /// <summary>
        /// Gets the path to the Studio directory
        /// </summary>
        /// <value>The studio path.</value>
        string StudioPath { get; }

        /// <summary>
        /// Gets the path to the Year directory
        /// </summary>
        /// <value>The year path.</value>
        string YearPath { get; }

        /// <summary>
        /// Gets the path to the General IBN directory
        /// </summary>
        /// <value>The general path.</value>
        string GeneralPath { get; }

        /// <summary>
        /// Gets the path to the Ratings IBN directory
        /// </summary>
        /// <value>The ratings path.</value>
        string RatingsPath { get; }

        /// <summary>
        /// Gets the path to the user configuration directory
        /// </summary>
        /// <value>The user configuration directory path.</value>
        string UserConfigurationDirectoryPath { get; }

        /// <summary>
        /// Gets the FF MPEG stream cache path.
        /// </summary>
        /// <value>The FF MPEG stream cache path.</value>
        string FFMpegStreamCachePath { get; }

        /// <summary>
        /// Gets the folder path to tools
        /// </summary>
        /// <value>The media tools path.</value>
        string MediaToolsPath { get; }
    }
}