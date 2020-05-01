#pragma warning disable CS1591

namespace MediaBrowser.Model.IO
{
    public interface IShortcutHandler
    {
        /// <summary>
        /// Gets the extension.
        /// </summary>
        /// <value>The extension.</value>
        string Extension { get; }

        /// <summary>
        /// Resolves the specified shortcut path.
        /// </summary>
        /// <param name="shortcutPath">The shortcut path.</param>
        /// <returns>System.String.</returns>
        string Resolve(string shortcutPath);

        /// <summary>
        /// Creates the specified shortcut path.
        /// </summary>
        /// <param name="shortcutPath">The shortcut path.</param>
        /// <param name="targetPath">The target path.</param>
        /// <returns>System.String.</returns>
        void Create(string shortcutPath, string targetPath);
    }
}
