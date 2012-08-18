using System;
using System.Diagnostics;
using System.Windows;
using MediaBrowser.Common.Logging;
using MediaBrowser.Controller;
using MediaBrowser.Model.Progress;

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
            LoadKernel();
        }

        private void LoadKernel()
        {
            Progress<TaskProgress> progress = new Progress<TaskProgress>();
            Common.UI.Splash splash = new Common.UI.Splash(progress);

            splash.Show();
            
            try
            {
                DateTime now = DateTime.Now;

                new Kernel().Init(progress);

                double seconds = (DateTime.Now - now).TotalSeconds;

                Logger.LogInfo("Kernel.Init completed in {0} seconds.", seconds);
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error launching Media Browser Server: " + ex.Message);
                Close();
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

        #endregion

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
            this.Close();
        }

        #endregion
    }
}
