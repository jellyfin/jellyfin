using System;

namespace MediaBrowser.Common.Plugins
{
    /// <summary>
    /// Interface IUIPlugin
    /// </summary>
    public interface IUIPlugin : IPlugin
    {
        /// <summary>
        /// Gets the minimum required UI version.
        /// </summary>
        /// <value>The minimum required UI version.</value>
        Version MinimumRequiredUIVersion { get; }
    }
}
