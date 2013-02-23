using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Net;
using MediaBrowser.UI;
using MediaBrowser.UI.Controller;
using MediaBrowser.UI.Controls;
using MediaBrowser.UI.Playback;
using MediaBrowser.UI.Playback.InternalPlayer;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MediaBrowser.Plugins.DefaultTheme.Resources
{
    /// <summary>
    /// Class AppResources
    /// </summary>
    public partial class AppResources : ResourceDictionary
    {
        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static AppResources Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppResources" /> class.
        /// </summary>
        public AppResources()
        {
            InitializeComponent();

            Instance = this;

            UIKernel.Instance.PlaybackManager.PlaybackStarted += PlaybackManager_PlaybackStarted;
            UIKernel.Instance.PlaybackManager.PlaybackCompleted += PlaybackManager_PlaybackCompleted;
        }

        /// <summary>
        /// Handles the Click event of the NowPlayingButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void NowPlaying_Click(object sender, RoutedEventArgs e)
        {
            App.Instance.NavigateToInternalPlayerPage();
        }

        /// <summary>
        /// Handles the PlaybackCompleted event of the PlaybackManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PlaybackStopEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void PlaybackManager_PlaybackCompleted(object sender, PlaybackStopEventArgs e)
        {
            App.Instance.ApplicationWindow.Dispatcher.Invoke(() => NowPlayingButton.Visibility = Visibility.Collapsed);
        }

        /// <summary>
        /// Handles the PlaybackStarted event of the PlaybackManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PlaybackEventArgs" /> instance containing the event data.</param>
        void PlaybackManager_PlaybackStarted(object sender, PlaybackEventArgs e)
        {
            if (e.Player is BaseInternalMediaPlayer)
            {
                App.Instance.ApplicationWindow.Dispatcher.Invoke(() => NowPlayingButton.Visibility = Visibility.Visible);
            }
        }

        /// <summary>
        /// Weathers the button click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void WeatherButtonClick(object sender, RoutedEventArgs e)
        {
            App.Instance.DisplayWeather();
        }

        /// <summary>
        /// Settingses the button click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            App.Instance.NavigateToSettingsPage();
        }

        /// <summary>
        /// This is a common element that appears on every page.
        /// </summary>
        /// <value>The view button.</value>
        public Button ViewButton
        {
            get
            {
                return TreeHelper.FindChild<Button>(App.Instance.ApplicationWindow, "ViewButton");
            }
        }

        /// <summary>
        /// Gets the now playing button.
        /// </summary>
        /// <value>The now playing button.</value>
        private Button NowPlayingButton
        {
            get
            {
                return TreeHelper.FindChild<Button>(App.Instance.ApplicationWindow, "NowPlayingButton");
            }
        }

        /// <summary>
        /// This is a common element that appears on every page.
        /// </summary>
        /// <value>The page title panel.</value>
        public StackPanel PageTitlePanel
        {
            get
            {
                return TreeHelper.FindChild<StackPanel>(App.Instance.ApplicationWindow, "PageTitlePanel");
            }
        }

        /// <summary>
        /// Gets the content of the header.
        /// </summary>
        /// <value>The content of the header.</value>
        public StackPanel HeaderContent
        {
            get
            {
                return TreeHelper.FindChild<StackPanel>(App.Instance.ApplicationWindow, "HeaderContent");
            }
        }
        
        /// <summary>
        /// Sets the default page title.
        /// </summary>
        public void SetDefaultPageTitle()
        {
            var img = new Image { };
            img.SetResourceReference(Image.StyleProperty, "MBLogoImageWhite");

            SetPageTitle(img);
        }

        /// <summary>
        /// Clears the page title.
        /// </summary>
        public void ClearPageTitle()
        {
            PageTitlePanel.Children.Clear();
        }

        /// <summary>
        /// Sets the page title.
        /// </summary>
        /// <param name="item">The item.</param>
        public async Task SetPageTitle(BaseItemDto item)
        {
            if (item.HasLogo || !string.IsNullOrEmpty(item.ParentLogoItemId))
            {
                var url = App.Instance.ApiClient.GetLogoImageUrl(item, new ImageOptions
                    {
                        Quality = 100
                    });

                try
                {
                    var image = await App.Instance.GetRemoteImageAsync(url);

                    image.SetResourceReference(Image.StyleProperty, "ItemLogo");
                    SetPageTitle(image);
                }
                catch (HttpException)
                {
                    SetPageTitleText(item);
                }
            }
            else
            {
                SetPageTitleText(item);
            }
        }

        /// <summary>
        /// Sets the page title text.
        /// </summary>
        /// <param name="item">The item.</param>
        private void SetPageTitleText(BaseItemDto item)
        {
            SetPageTitle(item.SeriesName ?? item.Album ?? item.Name);
        }

        /// <summary>
        /// Sets the page title.
        /// </summary>
        /// <param name="title">The title.</param>
        public void SetPageTitle(string title)
        {
            var textblock = new TextBlock { Text = title, Margin = new Thickness(0, 10, 0, 0) };
            textblock.SetResourceReference(TextBlock.StyleProperty, "Heading2TextBlockStyle");

            SetPageTitle(textblock);
        }

        /// <summary>
        /// Sets the page title.
        /// </summary>
        /// <param name="element">The element.</param>
        public void SetPageTitle(UIElement element)
        {
            var panel = PageTitlePanel;

            panel.Children.Clear();
            panel.Children.Add(element);
        }
    }
}
