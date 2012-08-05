using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MediaBrowser.Controller;
using MediaBrowser.Model.Progress;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected static Kernel kernel;

        public MainWindow()
        {
            InitializeComponent();
            LoadKernel();
        }

        private static void LoadKernel()
        {
            Progress<TaskProgress> progress = new Progress<TaskProgress>();
            SplashScreen splash = new SplashScreen(progress);

            try
            {
                DateTime now = DateTime.Now;

                splash.Show();

                kernel = new Kernel();

                kernel.Init(progress);

                var time = DateTime.Now - now;
            }
            catch
            {
            }
            finally
            {
                splash.Close();
            }
        }

        #region Main Window Events

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Don't show the system tray icon until the app has loaded.
            this.MbTaskbarIcon.Visibility = System.Windows.Visibility.Visible;
        }
        
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            kernel.Dispose();
        }

        #endregion

        #region Context Menu events

        private void cmOpenDashboard_click(object sender, RoutedEventArgs e)
        {

        }

        private void cmVisitCT_click(object sender, RoutedEventArgs e)
        {

        }

        private void cmExit_click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion
    }
}
