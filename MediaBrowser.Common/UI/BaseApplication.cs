using System;
using System.Threading.Tasks;
using System.Windows;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Logging;
using MediaBrowser.Model.Progress;

namespace MediaBrowser.Common.UI
{
    /// <summary>
    /// Serves as a base Application class for both the UI and Server apps.
    /// </summary>
    public abstract class BaseApplication : Application
    {
        private IKernel Kernel { get; set; }

        protected abstract IKernel InstantiateKernel();
        protected abstract Window InstantiateMainWindow();

        protected async override void OnStartup(StartupEventArgs e)
        {
            // Without this the app will shutdown after the splash screen closes
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            await LoadKernel().ConfigureAwait(false);
        }

        private async Task LoadKernel()
        {
            Kernel = InstantiateKernel();

            Progress<TaskProgress> progress = new Progress<TaskProgress>();
            Splash splash = new Splash(progress);

            splash.Show();

            try
            {
                DateTime now = DateTime.Now;

                await Kernel.Init(progress);

                double seconds = (DateTime.Now - now).TotalSeconds;

                Logger.LogInfo("Kernel.Init completed in {0} seconds.", seconds);
                splash.Close();

                this.ShutdownMode = System.Windows.ShutdownMode.OnLastWindowClose;
                InstantiateMainWindow().ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("There was an error launching Media Browser: " + ex.Message);
                splash.Close();

                // Shutdown the app with an error code
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Kernel.Dispose();
        }
    }
}
