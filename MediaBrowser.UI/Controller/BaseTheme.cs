using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MediaBrowser.UI.Controller
{
    /// <summary>
    /// Class BaseTheme
    /// </summary>
    public abstract class BaseTheme : IDisposable
    {
        /// <summary>
        /// Gets the global resources.
        /// </summary>
        /// <returns>IEnumerable{ResourceDictionary}.</returns>
        public abstract IEnumerable<ResourceDictionary> GetGlobalResources();

        /// <summary>
        /// Gets the list page.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Page.</returns>
        public abstract Page GetListPage(BaseItemDto item);
        /// <summary>
        /// Gets the detail page.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Page.</returns>
        public abstract Page GetDetailPage(BaseItemDto item);
        /// <summary>
        /// Gets the home page.
        /// </summary>
        /// <returns>Page.</returns>
        public abstract Page GetHomePage();
        /// <summary>
        /// Gets the login page.
        /// </summary>
        /// <returns>Page.</returns>
        public abstract Page GetLoginPage();
        /// <summary>
        /// Gets the internal player page.
        /// </summary>
        /// <returns>Page.</returns>
        public abstract Page GetInternalPlayerPage();

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Displays the weather.
        /// </summary>
        public abstract void DisplayWeather();
    }
}
