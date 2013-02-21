using System.Windows;
using System.Windows.Controls;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// Interaction logic for WindowCommands.xaml
    /// </summary>
    public partial class WindowCommands : UserControl
    {
        /// <summary>
        /// Gets the parent window.
        /// </summary>
        /// <value>The parent window.</value>
        public Window ParentWindow
        {
            get { return TreeHelper.TryFindParent<Window>(this); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowCommands" /> class.
        /// </summary>
        public WindowCommands()
        {
            InitializeComponent();
            Loaded += WindowCommandsLoaded;
        }

        /// <summary>
        /// Windows the commands loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void WindowCommandsLoaded(object sender, RoutedEventArgs e)
        {
            CloseApplicationButton.Click += CloseApplicationButtonClick;
            MinimizeApplicationButton.Click += MinimizeApplicationButtonClick;
            MaximizeApplicationButton.Click += MaximizeApplicationButtonClick;
            UndoMaximizeApplicationButton.Click += UndoMaximizeApplicationButtonClick;
        }

        /// <summary>
        /// Undoes the maximize application button click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void UndoMaximizeApplicationButtonClick(object sender, RoutedEventArgs e)
        {
            ParentWindow.WindowState = WindowState.Normal;
        }

        /// <summary>
        /// Maximizes the application button click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void MaximizeApplicationButtonClick(object sender, RoutedEventArgs e)
        {
            ParentWindow.WindowState = WindowState.Maximized;
        }

        /// <summary>
        /// Minimizes the application button click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void MinimizeApplicationButtonClick(object sender, RoutedEventArgs e)
        {
            ParentWindow.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Closes the application button click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        void CloseApplicationButtonClick(object sender, RoutedEventArgs e)
        {
            App.Instance.Shutdown();
        }
    }
}
