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
        public MainWindow()
        {
            InitializeComponent();
            LoadKernel();
        }

        private void LoadKernel()
        {
            Progress<TaskProgress> progress = new Progress<TaskProgress>();
            SplashScreen splash = new SplashScreen(progress);

            try
            {
                splash.Show();

                new Kernel().Init(progress);
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error launching Media Browser: " + ex.Message);
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
