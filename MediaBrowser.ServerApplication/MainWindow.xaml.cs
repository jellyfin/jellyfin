using System;
using System.Diagnostics;
using System.Windows;
using MediaBrowser.Common.Logging;
using MediaBrowser.Controller;
using MediaBrowser.Model.Progress;
using System.Threading.Tasks;
using MediaBrowser.Common.UI;

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

        private async void LoadKernel()
        {
            Progress<TaskProgress> progress = new Progress<TaskProgress>();
            Splash splash = new Splash(progress);

            splash.Show();
            
            try
            {
                DateTime now = DateTime.Now;

                await new Kernel().Init(progress);

                double seconds = (DateTime.Now - now).TotalSeconds;

                Logger.LogInfo("Kernel.Init completed in {0} seconds.", seconds);

                // Don't show the system tray icon until the kernel finishes.
                this.MbTaskbarIcon.Visibility = System.Windows.Visibility.Visible;
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
