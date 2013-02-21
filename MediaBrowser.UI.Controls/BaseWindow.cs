using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// Provides a base class for all Windows
    /// </summary>
    public abstract class BaseWindow : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="info">The info.</param>
        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        /// <summary>
        /// The _content scale
        /// </summary>
        private double _contentScale = 1;
        /// <summary>
        /// Gets the content scale.
        /// </summary>
        /// <value>The content scale.</value>
        public double ContentScale
        {
            get { return _contentScale; }
            private set
            {
                _contentScale = value;
                OnPropertyChanged("ContentScale");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseWindow" /> class.
        /// </summary>
        protected BaseWindow()
            : base()
        {
            SizeChanged += MainWindow_SizeChanged;
            Loaded += BaseWindowLoaded;
        }

        /// <summary>
        /// Bases the window loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void BaseWindowLoaded(object sender, RoutedEventArgs e)
        {
            OnLoaded();
        }

        /// <summary>
        /// Called when [loaded].
        /// </summary>
        protected virtual void OnLoaded()
        {
            MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }

        /// <summary>
        /// Handles the SizeChanged event of the MainWindow control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SizeChangedEventArgs" /> instance containing the event data.</param>
        void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ContentScale = e.NewSize.Height / 1080;
        }

        /// <summary>
        /// Called when [browser back].
        /// </summary>
        protected virtual void OnBrowserBack()
        {
            
        }

        /// <summary>
        /// Called when [browser forward].
        /// </summary>
        protected virtual void OnBrowserForward()
        {

        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Keyboard.PreviewKeyDown" /> attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.KeyEventArgs" /> that contains the event data.</param>
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (IsBackPress(e))
            {
                e.Handled = true;

                if (!e.IsRepeat)
                {
                    OnBrowserBack();
                }
            }

            else if (IsForwardPress(e))
            {
                e.Handled = true;

                if (!e.IsRepeat)
                {
                    OnBrowserForward();
                }
            }
            base.OnPreviewKeyDown(e);
        }

        /// <summary>
        /// Determines if a keypress should be treated as a backward press
        /// </summary>
        /// <param name="e">The <see cref="KeyEventArgs" /> instance containing the event data.</param>
        /// <returns><c>true</c> if [is back press] [the specified e]; otherwise, <c>false</c>.</returns>
        private bool IsBackPress(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                return true;
            }

            if (e.Key == Key.BrowserBack || e.Key == Key.Back)
            {
                return true;
            }

            if (e.SystemKey == Key.Left && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a keypress should be treated as a forward press
        /// </summary>
        /// <param name="e">The <see cref="KeyEventArgs" /> instance containing the event data.</param>
        /// <returns><c>true</c> if [is forward press] [the specified e]; otherwise, <c>false</c>.</returns>
        private bool IsForwardPress(KeyEventArgs e)
        {
            if (e.Key == Key.BrowserForward)
            {
                return true;
            }

            if (e.SystemKey == Key.RightAlt && e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                return true;
            }

            return false;
        }
    }
}
