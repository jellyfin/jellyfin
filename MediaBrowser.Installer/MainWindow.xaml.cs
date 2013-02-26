using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using Ionic.Zip;
using MediaBrowser.Installer.Code;
using ServiceStack.Text;

namespace MediaBrowser.Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected PackageVersionClass PackageClass = PackageVersionClass.Release;
        protected Version PackageVersion = new Version(4,0,0,0);
        protected string PackageName = "MBServer";
        protected string RootSuffix = "-Server";
        protected string TargetExe = "MediaBrowser.ServerApplication.exe";
        protected string FriendlyName = "Media Browser Server";
        protected string RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MediaBrowser-Server");

        protected bool SystemClosing = false;

        protected string TempLocation = Path.Combine(Path.GetTempPath(), "MediaBrowser");

        public MainWindow()
        {
            GetArgs();
            InitializeComponent();
            DoInstall();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!SystemClosing && MessageBox.Show("Cancel Installation - Are you sure?", "Cancel", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
            ClearTempLocation(TempLocation);
            base.OnClosing(e);
        }

        protected void SystemClose(string message = null)
        {
            if (message != null)
            {
                MessageBox.Show(message, "Error");
            }
            SystemClosing = true;
            this.Close();
        }

        protected void GetArgs()
        {
            var product = ConfigurationManager.AppSettings["product"] ?? "server";
            PackageClass = (PackageVersionClass) Enum.Parse(typeof (PackageVersionClass), ConfigurationManager.AppSettings["class"] ?? "Release");

            switch (product.ToLower())
            {
                case "mbt":
                    PackageName = "MBTheater";
                    RootSuffix = "-UI";
                    TargetExe = "MediaBrowser.UI.exe";
                    FriendlyName = "Media Browser Theater";
                    break;

                default:
                    PackageName = "MBServer";
                    RootSuffix = "-Server";
                    TargetExe = "MediaBrowser.ServerApplication.exe";
                    FriendlyName = "Media Browser Server";
                    break;
            }

            RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "MediaBrowser" + RootSuffix);

        }

        /// <summary>
        /// Execute the install process
        /// </summary>
        /// <returns></returns>
        protected async Task DoInstall()
        {
            lblStatus.Content = string.Format("Downloading {0}...", FriendlyName);
            dlAnimation.StartAnimation();
            prgProgress.Value = 0;
            prgProgress.Visibility = Visibility.Visible;

            // Determine Package version
            var version = await GetPackageVersion();
            lblStatus.Content = string.Format("Downloading {0} (version {1})...", FriendlyName, version.versionStr);

            // Download
            var archive = await DownloadPackage(version);
            dlAnimation.StopAnimation();
            prgProgress.Visibility = btnCancel.Visibility = Visibility.Hidden;

            // Extract
            lblStatus.Content = "Extracting Package...";
            try 
            {
                ExtractPackage(archive);
            }
            catch (Exception e)
            {
                SystemClose("Error Extracting - " + e.GetType().FullName + "\n\n" + e.Message);
            }

            // Create shortcut
            var fullPath = Path.Combine(RootPath, "System", TargetExe);
            try
            {
                CreateShortcuts(fullPath);
            }
            catch (Exception e)
            {
                SystemClose("Error Creating Shortcut - "+e.GetType().FullName+"\n\n"+e.Message);
            }

            // And run
            try
            {
                Process.Start(fullPath);
            }
            catch (Exception e)
            {
                SystemClose("Error Executing - "+fullPath+ " "+e.GetType().FullName+"\n\n"+e.Message);
            }

            SystemClose();

        }

        protected async Task<PackageVersionInfo> GetPackageVersion()
        {
            using (var client = new WebClient())
            {
                try
                {
                    // get the package information for the server
                    var json = await client.DownloadStringTaskAsync("http://www.mb3admin.com/admin/service/package/retrieveAll?name=" + PackageName);
                    var packages = JsonSerializer.DeserializeFromString<List<PackageInfo>>(json);

                    var version = packages[0].versions.Where(v => v.classification <= PackageClass).OrderByDescending(v => v.version).FirstOrDefault(v => v.version <= PackageVersion);
                    if (version == null)
                    {
                        SystemClose("Could not locate download package.  Aborting.");
                        return null;
                    }
                    return version;
                }
                catch (Exception e)
                {
                    SystemClose(e.GetType().FullName + "\n\n" + e.Message);
                }

            }
            return null;
        }

        /// <summary>
        /// Download our specified package to an archive in a temp location
        /// </summary>
        /// <returns>The fully qualified name of the downloaded package</returns>
        protected async Task<string> DownloadPackage(PackageVersionInfo version)
        {
            using (var client = new WebClient())
            {
                try
                {
                    var archiveFile = Path.Combine(PrepareTempLocation(), version.targetFilename);

                    // setup download progress and download the package
                    client.DownloadProgressChanged += DownloadProgressChanged;
                    await client.DownloadFileTaskAsync(version.sourceUrl, archiveFile);
                    return archiveFile;
                }
                catch (Exception e)
                {
                    SystemClose(e.GetType().FullName + "\n\n" + e.Message);
                }
            }
        return "";

        }

        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            prgProgress.Value = e.ProgressPercentage;
        }

        /// <summary>
        /// Extract the provided archive to our program root
        /// It is assumed the archive is a zip file relative to that root (with all necessary sub-folders)
        /// </summary>
        /// <param name="archive"></param>
        protected void ExtractPackage(string archive)
        {
            using (var fileStream = System.IO.File.OpenRead(archive))
            {
                using (var zipFile = ZipFile.Read(fileStream))
                {
                    zipFile.ExtractAll(RootPath, ExtractExistingFileAction.OverwriteSilently);
                }
            }

        }

        /// <summary>
        /// Create a shortcut in the current user's start menu
        ///  Only do current user to avoid need for admin elevation
        /// </summary>
        /// <param name="targetExe"></param>
        protected void CreateShortcuts(string targetExe)
        {
            // get path to all users start menu
            var startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),"Media Browser 3");
            if (!Directory.Exists(startMenu)) Directory.CreateDirectory(startMenu);
            var product = new ShellShortcut(Path.Combine(startMenu, FriendlyName+".lnk")) {Path = targetExe, Description = "Run " + FriendlyName};
            product.Save();

            if (PackageName == "MBServer")
            {
                var path = Path.Combine(startMenu, "MB Dashboard.lnk");
                var dashboard = new ShellShortcut(path) 
                {Path = @"http://localhost:8096/mediabrowser/dashboard/dashboard.html", Description = "Open the Media Browser Server Dashboard (configuration)"};
                dashboard.Save();
                
            }
            var uninstall = new ShellShortcut(Path.Combine(startMenu, "Uninstall " + FriendlyName + ".lnk")) 
            {Path = Path.Combine(Path.GetDirectoryName(targetExe), "MediaBrowser.Uninstaller.exe"), Arguments = (PackageName == "MBServer" ? "server" : "mbt"), Description = "Uninstall " + FriendlyName};
            uninstall.Save();

        }

        /// <summary>
        /// Prepare a temporary location to download to
        /// </summary>
        /// <returns>The path to the temporary location</returns>
        protected string PrepareTempLocation()
        {
            ClearTempLocation(TempLocation);
            Directory.CreateDirectory(TempLocation);
            return TempLocation;
        }

        /// <summary>
        /// Clear out (delete recursively) the supplied temp location
        /// </summary>
        /// <param name="location"></param>
        protected void ClearTempLocation(string location)
        {
            if (Directory.Exists(location))
            {
                Directory.Delete(location, true);
            }
        }

    }
}
