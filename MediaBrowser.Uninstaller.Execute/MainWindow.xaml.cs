using System;
using System.Diagnostics;
using System.Net;
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

            var args = Environment.GetCommandLineArgs();
            var product = args.Length > 1 ? args[1] : "server";
            var callerId = args.Length > 2 ? args[2] : null;
            if (callerId != null)
            {
                // Wait for our caller to exit
                try
                {
                    var process = Process.GetProcessById(Convert.ToInt32(callerId));
                    process.WaitForExit();
                }
                catch (ArgumentException)
                {
                    // wasn't running
                }
            }
            else
            {
                Thread.Sleep(1000); // crude method
            }

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
                var procs = Process.GetProcessesByName("mediabrowser.serverapplication");
                var server = procs.Length > 0 ? procs[0] : null;
                if (server != null)
                {
                    using (var client = new WebClient())
                    {
                        lblHeading.Content = "Shutting Down Media Browser Server...";
                        try
                        {
                            client.UploadString("http://localhost:8096/mediabrowser/system/shutdown", "");
                            try
                            {
                                server.WaitForExit();
                            }
                            catch (ArgumentException)
                            {
                                // already gone
                            }
                        }
                        catch (WebException ex)
                        {
                            if (ex.Status != WebExceptionStatus.ConnectFailure && !ex.Message.StartsWith("Unable to connect", StringComparison.OrdinalIgnoreCase))
                            {
                                MessageBox.Show("Error shutting down server.  Please be sure it is not running before hitting OK.\n\n" + ex.Status + "\n\n" + ex.Message);
                            }
                        }
                    }
                }
            }
            else
            {
                // Installing MBT - shut it down if it is running
                var processes = Process.GetProcessesByName("mediabrowser.ui");
                if (processes.Length > 0)
                {
                    lblHeading.Content = "Shutting Down Media Browser Theater...";
                    try
                    {
                        processes[0].Kill();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to shutdown Media Browser Theater.  Please ensure it is not running before hitting OK.\n\n" + ex.Message, "Error");
                    }
                }
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

            lblHeading.Content = "Removing System Files...";
            if (cbxRemoveAll.IsChecked == true)
            {
                // Just remove our whole directory
                RemovePath(rootPath);
            }
            else
            {
                // First remove the system
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
