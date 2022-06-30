using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Plugins
{
    /// <summary>
    /// Interface for PluginPagesManager.
    /// </summary>
    public interface IPluginPagesManager
    {
        /// <summary>
        /// Register an instance of a <see cref="PluginPage"/>.
        /// </summary>
        /// <param name="page">The page to register.</param>
        void RegisterPluginPage(PluginPage page);

        /// <summary>
        /// Get a list of the registered pages.
        /// </summary>
        /// <returns>Array of <see cref="PluginPage"/>.</returns>
        IEnumerable<PluginPage> GetPages();
    }

    /// <summary>
    /// Description for a page a Plugin is providing to the frontend.
    /// Used to populate the hamburger menu with options.
    /// </summary>
    public class PluginPage
    {
        /// <summary>
        /// The ID of the page.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// The URL to the HTML the plugin is serving.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// What the display text should be.
        /// </summary>
        public string? DisplayText { get; set; }

        /// <summary>
        /// What material icon should be displayed in the menu.
        /// </summary>
        public string? Icon { get; set; }
    }
}
