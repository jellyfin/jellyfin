using MediaBrowser.UI.Controller;
using MediaBrowser.UI.Playback;
using System.Windows;

namespace MediaBrowser.UI.Pages
{
    /// <summary>
    /// Class BaseInternalPlayerPage
    /// </summary>
    public abstract class BaseInternalPlayerPage : BasePage
    {
        /// <summary>
        /// Called when [loaded].
        /// </summary>
        protected override void OnLoaded()
        {
            base.OnLoaded();

            App.Instance.ApplicationWindow.WindowBackgroundContent.Visibility = Visibility.Collapsed;
            App.Instance.ApplicationWindow.PageContent.Visibility = Visibility.Collapsed;

            UIKernel.Instance.PlaybackManager.PlaybackCompleted -= PlaybackManager_PlaybackCompleted;
            UIKernel.Instance.PlaybackManager.PlaybackCompleted += PlaybackManager_PlaybackCompleted;
        }

        /// <summary>
        /// Handles the PlaybackCompleted event of the PlaybackManager control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PlaybackEventArgs" /> instance containing the event data.</param>
        void PlaybackManager_PlaybackCompleted(object sender, PlaybackStopEventArgs e)
        {
            App.Instance.ApplicationWindow.NavigateBack();
        }

        /// <summary>
        /// Called when [unloaded].
        /// </summary>
        protected override void OnUnloaded()
        {
            UIKernel.Instance.PlaybackManager.PlaybackCompleted -= PlaybackManager_PlaybackCompleted;

            base.OnUnloaded();

            App.Instance.ApplicationWindow.PageContent.Visibility = Visibility.Visible;
            App.Instance.ApplicationWindow.WindowBackgroundContent.Visibility = Visibility.Visible;
        }
    }
}
