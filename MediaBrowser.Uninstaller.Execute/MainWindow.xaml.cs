using System;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Windows;

namespace MediaBrowser.Uninstaller.Execute
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected string Product = "Server";

        public MainWindow()
        {
            Thread.Sleep(800); // be sure our caller is shut down

            var args = Environment.GetCommandLineArgs();
            var product = args.Length > 1 ? args[1] : "server";
            InitializeComponent();


            switch (product)
            {
                case "server":
                    Product = "Server";
                    break;

                case "mbt":
                    Product = "Theater";
                    break;

                default:
                    MessageBox.Show("Please specify which application to un-install (server or mbt)");
                    Close();
                    break;

            }

            lblHeading.Content = this.Title = "Uninstall Media Browser " + Product;

        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void cbxRemoveAll_Checked(object sender, RoutedEventArgs e)
        {
            if (cbxRemoveAll.IsChecked == true)
            {
                cbxRemoveCache.IsChecked = cbxRemoveConfig.IsChecked = cbxRemovePlugins.IsChecked = true;
            }

            cbxRemoveCache.IsEnabled = cbxRemoveConfig.IsEnabled = cbxRemovePlugins.IsEnabled = !cbxRemoveAll.IsChecked.Value;
        }

        private void btnUninstall_Click(object sender, RoutedEventArgs e)
        {
            // First remove our shortcuts
            var startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Media Browser");
            var linkName = "Media Browser " + Product + ".lnk";
            try 
            {
                File.Delete(Path.Combine(startMenu,linkName));
            }
            catch {} // oh well

            linkName = "Uninstall " + linkName;
            try 
            {
                File.Delete(Path.Combine(startMenu,linkName));
            }
            catch {} // oh well

        }
    }
}
