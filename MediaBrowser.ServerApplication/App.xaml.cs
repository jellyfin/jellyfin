using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.UI;
using MediaBrowser.Controller;
using Microsoft.Shell;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : BaseApplication, ISingleInstanceApp
    {
        private const string Unique = "MediaBrowser3";

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                var application = new App();
                application.InitializeComponent();

                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
        }

        #region ISingleInstanceApp Members
        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            OpenDashboard();

            return true;
        }
        #endregion

        public static void OpenDashboard()
        {
            OpenUrl("http://localhost:" + Kernel.Instance.Configuration.HttpServerPortNumber +
                        "/mediabrowser/dashboard/index.html");
        }
        
        public static void OpenUrl(string url)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = url
                },

                EnableRaisingEvents = true
            };

            process.Exited += ProcessExited;

            process.Start();
        }

        static void ProcessExited(object sender, EventArgs e)
        {
            (sender as Process).Dispose();
        }

        protected override IKernel InstantiateKernel()
        {
            return new Kernel();
        }

        protected override Window InstantiateMainWindow()
        {
            return new MainWindow();
        }
    }
}
