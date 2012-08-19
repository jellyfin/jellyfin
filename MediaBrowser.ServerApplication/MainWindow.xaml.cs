using System.Diagnostics;
using System.Windows;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //LoadKernel();
        }

        #region Context Menu events

        private void cmOpenDashboard_click(object sender, RoutedEventArgs e)
        {
            App.OpenDashboard();
        }

        private void cmVisitCT_click(object sender, RoutedEventArgs e)
        {
            using (Process process = Process.Start("http://community.mediabrowser.tv/"))
            {
            }
        }

        private void cmExit_click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion
    }
}
