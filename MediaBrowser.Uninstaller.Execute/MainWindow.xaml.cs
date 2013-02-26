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
        protected string RootSuffix = "-Server";

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
                    RootSuffix = "-Server";
                    break;

                case "mbt":
                    Product = "Theater";
                    RootSuffix = "-UI";
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
            RemoveShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),linkName));
            linkName = "Uninstall " + linkName;
            RemoveShortcut(Path.Combine(startMenu, linkName));
            if (Product == "Server")
            {
                RemoveShortcut(Path.Combine(startMenu, "MB Dashboard.lnk"));
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

            var rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MediaBrowser" + RootSuffix);

            if (cbxRemoveAll.IsChecked == true)
            {
                // Just remove our whole directory
                RemovePath(rootPath);
            }
            else
            {
                // First remove the system
                lblHeading.Content = "Removing System Files...";
                RemovePath(Path.Combine(rootPath, "System"));
                RemovePath(Path.Combine(rootPath, "MediaTools"));

                // And then the others specified
                if (cbxRemoveCache.IsChecked == true)
                {
                    lblHeading.Content = "Removing Cache and Data Files...";
                    RemovePath(Path.Combine(rootPath, "cache"));
                    RemovePath(Path.Combine(rootPath, "data"));
                }
                if (cbxRemoveConfig.IsChecked == true)
                {
                    lblHeading.Content = "Removing Config Files...";
                    RemovePath(Path.Combine(rootPath, "config"));
                    RemovePath(Path.Combine(rootPath, "logs"));
                }
                if (cbxRemovePlugins.IsChecked == true)
                {
                    lblHeading.Content = "Removing Plugin Files...";
                    RemovePath(Path.Combine(rootPath, "plugins"));
                }
            }

            // and done
            lblHeading.Content = string.Format("Media Browser {0} Uninstalled.", Product);
            btnUninstall.Visibility = Visibility.Hidden;
            btnFinished.Visibility = Visibility.Visible;
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

        private static void RemovePath(string path)
        {
            try
            {
                Directory.Delete(path, true);
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error attempting to remove progam folder {0}\n\n {1}", path, ex.Message), "Error");
            }
            
        }

        private void BtnFinished_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
