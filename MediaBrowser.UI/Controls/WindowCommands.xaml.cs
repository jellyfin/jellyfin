using System.Windows;
using System.Windows.Controls;

namespace MediaBrowser.UI.Controls
{
    /// <summary>
    /// Interaction logic for WindowCommands.xaml
    /// </summary>
    public partial class WindowCommands : UserControl
    {
        public Window ParentWindow
        {
            get { return TreeHelper.TryFindParent<Window>(this); }
        }

        public WindowCommands()
        {
            InitializeComponent();
            Loaded += WindowCommandsLoaded;
        }

        void WindowCommandsLoaded(object sender, RoutedEventArgs e)
        {
            CloseApplicationButton.Click += CloseApplicationButtonClick;
            MinimizeApplicationButton.Click += MinimizeApplicationButtonClick;
            MaximizeApplicationButton.Click += MaximizeApplicationButtonClick;
            UndoMaximizeApplicationButton.Click += UndoMaximizeApplicationButtonClick;
        }

        void UndoMaximizeApplicationButtonClick(object sender, RoutedEventArgs e)
        {
            ParentWindow.WindowState = WindowState.Normal;
        }

        void MaximizeApplicationButtonClick(object sender, RoutedEventArgs e)
        {
            ParentWindow.WindowState = WindowState.Maximized;
        }

        void MinimizeApplicationButtonClick(object sender, RoutedEventArgs e)
        {
            ParentWindow.WindowState = WindowState.Minimized;
        }

        void CloseApplicationButtonClick(object sender, RoutedEventArgs e)
        {
            ParentWindow.Close();
        }
    }
}
