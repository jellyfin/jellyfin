using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using MediaBrowser.Controller;
using Microsoft.Shell;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
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

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Kernel.Instance.Dispose();
        }

        public static void OpenDashboard()
        {
            using (Process process = Process.Start("http://localhost:" + Kernel.Instance.Configuration.HttpServerPortNumber + "/mediabrowser/dashboard/index.html"))
            {
            }
        }
    }
}
