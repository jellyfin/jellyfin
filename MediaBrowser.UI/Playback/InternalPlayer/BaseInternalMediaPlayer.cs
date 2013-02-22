using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.UI.Configuration;
using System.Collections.Generic;
using System.Windows;

namespace MediaBrowser.UI.Playback.InternalPlayer
{
    /// <summary>
    /// Class BaseInternalMediaPlayer
    /// </summary>
    public abstract class BaseInternalMediaPlayer : BaseMediaPlayer
    {
        protected BaseInternalMediaPlayer(ILogger logger) : base(logger)
        {
        }

        /// <summary>
        /// Ensures the media player created.
        /// </summary>
        protected abstract void EnsureMediaPlayerCreated();

        /// <summary>
        /// Plays the internal.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="options">The options.</param>
        /// <param name="playerConfiguration">The player configuration.</param>
        protected override void PlayInternal(List<BaseItemDto> items, PlayOptions options, PlayerConfiguration playerConfiguration)
        {
            App.Instance.ApplicationWindow.Dispatcher.Invoke(() =>
            {
                App.Instance.ApplicationWindow.BackdropContainer.Visibility = Visibility.Collapsed;
                App.Instance.ApplicationWindow.WindowBackgroundContent.SetResourceReference(FrameworkElement.StyleProperty, "WindowBackgroundContentDuringPlayback");
            });

            App.Instance.NavigateToInternalPlayerPage();
        }

        /// <summary>
        /// Called when [player stopped internal].
        /// </summary>
        protected override void OnPlayerStoppedInternal()
        {
            App.Instance.ApplicationWindow.Dispatcher.Invoke(() =>
            {
                App.Instance.ApplicationWindow.BackdropContainer.Visibility = Visibility.Visible;
                App.Instance.ApplicationWindow.WindowBackgroundContent.SetResourceReference(FrameworkElement.StyleProperty, "WindowBackgroundContent");
            });
            
            base.OnPlayerStoppedInternal();
        }
    }
}
