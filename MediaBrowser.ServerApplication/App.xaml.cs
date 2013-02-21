using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.UI;
using MediaBrowser.Controller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : BaseApplication, IApplication
    {
        [STAThread]
        public static void Main()
        {
            RunApplication<App>("MediaBrowserServer");
        }

        protected override void OnSecondInstanceLaunched(IList<string> args)
        {
            base.OnSecondInstanceLaunched(args);

            OpenDashboard();
            InitializeComponent();
        }

        public static void OpenDashboard()
        {
            OpenUrl("http://localhost:" + Kernel.Instance.Configuration.HttpServerPortNumber + "/mediabrowser/dashboard/index.html");
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
