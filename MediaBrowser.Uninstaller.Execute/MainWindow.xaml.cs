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
            lblHeading.Content = "Removing Shortcuts...";
            btnCancel.IsEnabled = btnUninstall.IsEnabled = false;
            grdOptions.Visibility = Visibility.Hidden;

            var startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Media Browser 3");
            var linkName = "Media Browser " + Product + ".lnk";
            RemoveShortcut(Path.Combine(startMenu, linkName));
            linkName = "Uninstall " + linkName;
            RemoveShortcut(Path.Combine(startMenu, linkName));
            if (Product == "Server")
            {
                RemoveShortcut(Path.Combine(startMenu, "Dashboard.lnk"));
            }
            // if the startmenu item is empty now - delete it too
            if (Directory.GetFiles(startMenu).Length == 0)
            {
                try
                {
                    Directory.Delete(startMenu);
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (Exception ex)
                {
                    {
                        MessageBox.Show(string.Format("Error attempting to remove shortcut folder {0}\n\n {1}", startMenu, ex.Message), "Error");
                    }
                }
            }


            // and done
            lblHeading.Content = string.Format("Media Browser {0} Uninstalled.", Product);
            btnUninstall.Content = "Finish";
        }

        private static void RemoveShortcut(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (FileNotFoundException)
            {
            } // we're trying to get rid of it anyway
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error attempting to remove shortcut {0}\n\n {1}", path, ex.Message), "Error");
            }
            
        }
    }
}
