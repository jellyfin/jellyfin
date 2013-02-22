using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.UI.Controller;
using MediaBrowser.UI.Controls;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MediaBrowser.UI.Extensions;

namespace MediaBrowser.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : BaseWindow, IDisposable
    {
        /// <summary>
        /// Gets or sets the mouse idle timer.
        /// </summary>
        /// <value>The mouse idle timer.</value>
        private Timer MouseIdleTimer { get; set; }
        /// <summary>
        /// Gets or sets the backdrop timer.
        /// </summary>
        /// <value>The backdrop timer.</value>
        private Timer BackdropTimer { get; set; }
        /// <summary>
        /// Gets or sets the current backdrops.
        /// </summary>
        /// <value>The current backdrops.</value>
        private string[] CurrentBackdrops { get; set; }

        /// <summary>
        /// The _current backdrop index
        /// </summary>
        private int _currentBackdropIndex;
        /// <summary>
        /// Gets or sets the index of the current backdrop.
        /// </summary>
        /// <value>The index of the current backdrop.</value>
        public int CurrentBackdropIndex
        {
            get { return _currentBackdropIndex; }
            set
            {
                _currentBackdropIndex = value;
                OnPropertyChanged("CurrentBackdropIndex");
                Dispatcher.InvokeAsync(OnBackdropIndexChanged);
            }
        }

        /// <summary>
        /// The _is mouse idle
        /// </summary>
        private bool _isMouseIdle = true;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is mouse idle.
        /// </summary>
        /// <value><c>true</c> if this instance is mouse idle; otherwise, <c>false</c>.</value>
        public bool IsMouseIdle
        {
            get { return _isMouseIdle; }
            set
            {
                _isMouseIdle = value;

                Dispatcher.InvokeAsync(() => Cursor = value ? Cursors.None : Cursors.Arrow);

                OnPropertyChanged("IsMouseIdle");
            }
        }

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        public MainWindow(ILogger logger)
            : base()
        {
            _logger = logger;

            InitializeComponent();
        }

        /// <summary>
        /// Called when [loaded].
        /// </summary>
        protected override void OnLoaded()
        {
            base.OnLoaded();

            DragBar.MouseDown += DragableGridMouseDown;

            DataContext = App.Instance;
        }

        /// <summary>
        /// Loads the initial UI.
        /// </summary>
        /// <returns>Task.</returns>
        internal Task LoadInitialUI()
        {
            return LoadInitialPage();
        }

        /// <summary>
        /// Called when [backdrop index changed].
        /// </summary>
        private async void OnBackdropIndexChanged()
        {
            var currentBackdropIndex = CurrentBackdropIndex;

            if (currentBackdropIndex == -1  )
            {
                // Setting this to null doesn't seem to clear out the content
                // Have to check it for null or get startup errors
                if (BackdropContainer.Content != null)
                {
                    BackdropContainer.Content = new FrameworkElement();
                }
                return;
            }

            try
            {
                var bitmap = await App.Instance.GetRemoteBitmapAsync(CurrentBackdrops[currentBackdropIndex]);

                var img = new Image
                {
                    Source = bitmap
                };

                img.SetResourceReference(StyleProperty, "BackdropImage");

                BackdropContainer.Content = img;
            }
            catch (HttpException)
            {
                if (currentBackdropIndex == 0)
                {
                    BackdropContainer.Content = new FrameworkElement();
                }
            }
        }

        /// <summary>
        /// Loads the initial page.
        /// </summary>
        /// <returns>Task.</returns>
        private Task LoadInitialPage()
        {
            return App.Instance.LogoutUser();
        }

        /// <summary>
        /// Dragables the grid mouse down.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MouseButtonEventArgs" /> instance containing the event data.</param>
        private void DragableGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
            else if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Gets the page frame.
        /// </summary>
        /// <value>The page frame.</value>
        private TransitionFrame PageFrame
        {
            get
            {
                // Finding the grid that is generated by the ControlTemplate of the Button
                return TreeHelper.FindChild<TransitionFrame>(PageContent, "PageFrame");
            }
        }

        /// <summary>
        /// Navigates the specified page.
        /// </summary>
        /// <param name="page">The page.</param>
        internal void Navigate(Page page)
        {
            _logger.Info("Navigating to " + page.GetType().Name);
            
            Dispatcher.InvokeAsync(() => PageFrame.NavigateWithTransition(page));
        }

        /// <summary>
        /// Sets the backdrop based on a BaseItemDto
        /// </summary>
        /// <param name="item">The item.</param>
        public void SetBackdrops(BaseItemDto item)
        {
            var urls = App.Instance.ApiClient.GetBackdropImageUrls(item, new ImageOptions
            {
                MaxWidth = Convert.ToInt32(SystemParameters.VirtualScreenWidth),
                MaxHeight = Convert.ToInt32(SystemParameters.VirtualScreenHeight)
            });

            SetBackdrops(urls);
        }

        /// <summary>
        /// Sets the backdrop based on a list of image files
        /// </summary>
        /// <param name="backdrops">The backdrops.</param>
        public void SetBackdrops(string[] backdrops)
        {
            // Don't reload the same backdrops
            if (CurrentBackdrops != null && backdrops.SequenceEqual(CurrentBackdrops))
            {
                return;
            }

            DisposeBackdropTimer();
            CurrentBackdrops = backdrops;

            if (backdrops == null || backdrops.Length == 0)
            {
                CurrentBackdropIndex = -1;

                // Setting this to null doesn't seem to clear out the content
                // Have to check it for null or get startup errors
                if (BackdropContainer.Content != null)
                {
                    BackdropContainer.Content = new FrameworkElement();
                }
                return;
            }

            CurrentBackdropIndex = 0;

            // We only need the timer if there's more than one backdrop
            if (backdrops != null && backdrops.Length > 1)
            {
                BackdropTimer = new Timer(state =>
                {
                    // Don't display backdrops during video playback
                    if (UIKernel.Instance.PlaybackManager.ActivePlayers.Any(p => p.CurrentMedia.IsVideo))
                    {
                        return;
                    }

                    var index = CurrentBackdropIndex + 1;

                    if (index >= backdrops.Length)
                    {
                        index = 0;
                    }

                    CurrentBackdropIndex = index;

                }, null, 5000, 5000);
            }
        }

        /// <summary>
        /// Disposes the backdrop timer.
        /// </summary>
        public void DisposeBackdropTimer()
        {
            if (BackdropTimer != null)
            {
                BackdropTimer.Dispose();
            }
        }

        /// <summary>
        /// Disposes the mouse idle timer.
        /// </summary>
        public void DisposeMouseIdleTimer()
        {
            if (MouseIdleTimer != null)
            {
                MouseIdleTimer.Dispose();
            }
        }

        /// <summary>
        /// Clears the backdrops.
        /// </summary>
        public void ClearBackdrops()
        {
            SetBackdrops(new string[] { });
        }

        /// <summary>
        /// Navigates the back.
        /// </summary>
        public void NavigateBack()
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (PageFrame.NavigationService.CanGoBack)
                {
                    PageFrame.GoBackWithTransition();
                }
            });
        }

        /// <summary>
        /// Navigates the forward.
        /// </summary>
        public void NavigateForward()
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (PageFrame.NavigationService.CanGoForward)
                {
                    PageFrame.GoForwardWithTransition();
                }
            });
        }

        /// <summary>
        /// Called when [browser back].
        /// </summary>
        protected override void OnBrowserBack()
        {
            base.OnBrowserBack();

            NavigateBack();
        }

        /// <summary>
        /// Called when [browser forward].
        /// </summary>
        protected override void OnBrowserForward()
        {
            base.OnBrowserForward();

            NavigateForward();
        }

        /// <summary>
        /// Shows the control bar then starts a timer to hide it
        /// </summary>
        private void StartMouseIdleTimer()
        {
            IsMouseIdle = false;

            const int duration = 4000;

            // Start the timer if it's null, otherwise reset it
            if (MouseIdleTimer == null)
            {
                MouseIdleTimer = new Timer(MouseIdleTimerCallback, null, duration, Timeout.Infinite);
            }
            else
            {
                MouseIdleTimer.Change(duration, Timeout.Infinite);
            }
        }

        /// <summary>
        /// This is the Timer callback method to hide the control bar
        /// </summary>
        /// <param name="stateInfo">The state info.</param>
        private void MouseIdleTimerCallback(object stateInfo)
        {
            IsMouseIdle = true;

            if (MouseIdleTimer != null)
            {
                MouseIdleTimer.Dispose();
                MouseIdleTimer = null;
            }
        }

        /// <summary>
        /// The _last mouse move point
        /// </summary>
        private Point _lastMouseMovePoint;

        /// <summary>
        /// Handles OnMouseMove to show the control box
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // Store the last position for comparison purposes
            // Even if the mouse is not moving this event will fire as elements are showing and hiding
            var pos = e.GetPosition(this);

            if (pos == _lastMouseMovePoint)
            {
                return;
            }

            _lastMouseMovePoint = pos;

            StartMouseIdleTimer();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            DisposeBackdropTimer();
            DisposeMouseIdleTimer();
        }

        /// <summary>
        /// Shows a notification message that will disappear on it's own
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="icon">The icon.</param>
        public void ShowNotificationMessage(string text, string caption = null, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            var control = new NotificationMessage
            {
                Caption = caption,
                Text = text,
                MessageBoxImage = icon
            };

            mainGrid.Children.Add(control);

            Dispatcher.InvokeWithDelay(() => mainGrid.Children.Remove(control), 5000);
        }

        /// <summary>
        /// Shows a notification message that will disappear on it's own
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="icon">The icon.</param>
        public void ShowNotificationMessage(UIElement text, string caption = null, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            var control = new NotificationMessage
            {
                Caption = caption,
                TextContent = text,
                MessageBoxImage = icon
            };

            mainGrid.Children.Add(control);

            Dispatcher.InvokeWithDelay(() => mainGrid.Children.Remove(control), 5000);
        }

        /// <summary>
        /// Shows a modal message box and asynchronously returns a MessageBoxResult
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="button">The button.</param>
        /// <param name="icon">The icon.</param>
        /// <returns>MessageBoxResult.</returns>
        public MessageBoxResult ShowModalMessage(string text, string caption = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            var win = new ModalWindow
            {
                Caption = caption,
                Button = button,
                MessageBoxImage = icon,
                Text = text
            };

            win.ShowModal(this);

            return win.MessageBoxResult;
        }

        /// <summary>
        /// Shows a modal message box and asynchronously returns a MessageBoxResult
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="caption">The caption.</param>
        /// <param name="button">The button.</param>
        /// <param name="icon">The icon.</param>
        /// <returns>MessageBoxResult.</returns>
        public MessageBoxResult ShowModalMessage(UIElement text, string caption = null, MessageBoxButton button = MessageBoxButton.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            var win = new ModalWindow
            {
                Caption = caption,
                Button = button,
                MessageBoxImage = icon,
                TextContent = text
            };

            win.ShowModal(this);

            return win.MessageBoxResult;
        }
    }
}
