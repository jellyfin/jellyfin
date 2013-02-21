using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Web;
using System.Linq;
using MediaBrowser.Installer.Code;
using ServiceStack.Text;
using ServiceStack.Text.Json;

namespace MediaBrowser.Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        protected PackageVersionClass PackageClass;
        protected Version PackageVersion;
        protected string PackageName = "MBServer";

        public MainWindow()
        {
            GetArgs();
            InitializeComponent();
            StartInstall();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Cancel Installation - Are you sure?", "Cancel", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
            base.OnClosing(e);
        }

        protected void GetArgs()
        {
            var args = AppDomain.CurrentDomain.SetupInformation.ActivationArguments;

            if (args == null || args.ActivationData == null || args.ActivationData.Length <= 0) return;
            var url = new Uri(args.ActivationData[0], UriKind.Absolute);

            var parameters = HttpUtility.ParseQueryString(url.Query);

            // fill in our arguments if there
            PackageName = parameters["package"] ?? "MBServer";
            PackageClass = (PackageVersionClass)Enum.Parse(typeof(PackageVersionClass), parameters["class"] ?? "Release");
            PackageVersion = new Version(parameters["version"].ValueOrDefault("0.0.0.1"));

        }

        protected async Task StartInstall()
        {
            lblStatus.Content = "Downloading Server Package...";
            dlAnimation.StartAnimation();
            prgProgress.Value = 0;
            prgProgress.Visibility = Visibility.Visible;

            var archive = await DownloadPackage();

        }

        protected async Task<string> DownloadPackage()
        {
            using (var client = new WebClient())
            {
                try
                {
                    // get the package information for the server
                    var json = await client.DownloadStringTaskAsync("http://www.mb3admin.com/admin/service/package/retrieveAll?name="+PackageName);
                    var packages = JsonSerializer.DeserializeFromString<List<PackageInfo>>(json);

                    var version = packages[0].versions.Where(v => v.classification == PackageClass).OrderByDescending(v => v.version).FirstOrDefault();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        return "";

        }
    }
}
