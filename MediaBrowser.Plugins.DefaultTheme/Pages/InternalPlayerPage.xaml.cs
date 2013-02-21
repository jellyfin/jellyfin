using MediaBrowser.Plugins.DefaultTheme.Resources;
using MediaBrowser.UI.Pages;
using System.Windows;

namespace MediaBrowser.Plugins.DefaultTheme.Pages
{
    /// <summary>
    /// Interaction logic for InternalPlayerPage.xaml
    /// </summary>
    public partial class InternalPlayerPage : BaseInternalPlayerPage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InternalPlayerPage" /> class.
        /// </summary>
        public InternalPlayerPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Called when [loaded].
        /// </summary>
        protected override void OnLoaded()
        {
            base.OnLoaded();

            AppResources.Instance.ClearPageTitle();
            AppResources.Instance.HeaderContent.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Called when [unloaded].
        /// </summary>
        protected override void OnUnloaded()
        {
            base.OnUnloaded();

            AppResources.Instance.HeaderContent.Visibility = Visibility.Visible;
        }
    }
}
