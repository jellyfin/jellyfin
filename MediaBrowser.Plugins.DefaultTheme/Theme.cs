using MediaBrowser.Model.Dto;
using MediaBrowser.Plugins.DefaultTheme.Pages;
using MediaBrowser.Plugins.DefaultTheme.Resources;
using MediaBrowser.UI;
using MediaBrowser.UI.Controller;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MediaBrowser.Plugins.DefaultTheme
{
    /// <summary>
    /// Class Theme
    /// </summary>
    class Theme : BaseTheme
    {
        /// <summary>
        /// Gets the detail page.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Page.</returns>
        public override Page GetDetailPage(BaseItemDto item)
        {
            return new DetailPage(item.Id);
        }

        /// <summary>
        /// Gets the list page.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>Page.</returns>
        public override Page GetListPage(BaseItemDto item)
        {
            return new ListPage(item.Id);
        }

        /// <summary>
        /// Gets the home page.
        /// </summary>
        /// <returns>Page.</returns>
        public override Page GetHomePage()
        {
            return new HomePage();
        }

        /// <summary>
        /// Displays the weather.
        /// </summary>
        public override void DisplayWeather()
        {
            App.Instance.Navigate(new WeatherPage());
        }

        /// <summary>
        /// Gets the login page.
        /// </summary>
        /// <returns>Page.</returns>
        public override Page GetLoginPage()
        {
            return new LoginPage();
        }

        /// <summary>
        /// Gets the internal player page.
        /// </summary>
        /// <returns>Page.</returns>
        public override Page GetInternalPlayerPage()
        {
            return new InternalPlayerPage();
        }

        /// <summary>
        /// Gets the global resources.
        /// </summary>
        /// <returns>IEnumerable{ResourceDictionary}.</returns>
        public override IEnumerable<ResourceDictionary> GetGlobalResources()
        {
            return new[] { new AppResources() };
        }
    }
}
