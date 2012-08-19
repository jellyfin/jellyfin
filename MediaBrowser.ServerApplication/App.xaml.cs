using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.UI;
using MediaBrowser.Controller;
using MediaBrowser.Model.Progress;
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

        protected async override void OnStartup(StartupEventArgs e)
        {
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            await LoadKernel();
        }

        private async Task LoadKernel()
        {
            Progress<TaskProgress> progress = new Progress<TaskProgress>();
            Splash splash = new Splash(progress);

            splash.Show();

            try
            {
                DateTime now = DateTime.Now;

                await new Kernel().Init(progress);

                double seconds = (DateTime.Now - now).TotalSeconds;

                Logger.LogInfo("Kernel.Init completed in {0} seconds.", seconds);
                splash.Close();

                this.ShutdownMode = System.Windows.ShutdownMode.OnLastWindowClose;
                new MainWindow().ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error launching Media Browser Server: " + ex.Message);
                splash.Close();
                Shutdown(1);
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
