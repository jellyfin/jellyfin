using MediaBrowser.Model.Themes;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Themes
{
    public interface IAppThemeManager
    {
        /// <summary>
        /// Gets the themes.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <returns>IEnumerable{AppThemeInfo}.</returns>
        IEnumerable<AppThemeInfo> GetThemes(string applicationName);

        /// <summary>
        /// Gets the theme.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="name">The name.</param>
        /// <returns>AppTheme.</returns>
        AppTheme GetTheme(string applicationName, string name);

        /// <summary>
        /// Saves the theme.
        /// </summary>
        /// <param name="theme">The theme.</param>
        void SaveTheme(AppTheme theme);

        /// <summary>
        /// Gets the image image information.
        /// </summary>
        /// <param name="applicationName">Name of the application.</param>
        /// <param name="themeName">Name of the theme.</param>
        /// <param name="imageName">Name of the image.</param>
        /// <returns>InternalThemeImage.</returns>
        InternalThemeImage GetImageImageInfo(string applicationName, string themeName, string imageName);
    }
}
