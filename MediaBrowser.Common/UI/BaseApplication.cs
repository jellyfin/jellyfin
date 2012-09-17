using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Logging;
using MediaBrowser.Model.Progress;
using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace MediaBrowser.Common.UI
{
    /// <summary>
    /// Serves as a base Application class for both the UI and Server apps.
    /// </summary>
    public abstract class BaseApplication : Application, INotifyPropertyChanged, ISingleInstanceApp
    {
        private IKernel Kernel { get; set; }

        protected abstract IKernel InstantiateKernel();
        protected abstract Window InstantiateMainWindow();

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Without this the app will shutdown after the splash screen closes
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            LoadKernel();
        }

        private async void LoadKernel()
        {
            Kernel = InstantiateKernel();

            var progress = new Progress<TaskProgress>();

            progress.ProgressChanged += progress_ProgressChanged;
            
            var splash = new Splash(progress);

            splash.Show();

            try
            {
                DateTime now = DateTime.UtcNow;

                await Kernel.Init(progress);

                progress.ProgressChanged -= progress_ProgressChanged;
                
                Logger.LogInfo("Kernel.Init completed in {0} seconds.", (DateTime.UtcNow - now).TotalSeconds);
                splash.Close();

                ShutdownMode = System.Windows.ShutdownMode.OnLastWindowClose;

                OnKernelLoaded();

                InstantiateMainWindow().Show();
            }
            catch (Exception ex)
            {
                progress.ProgressChanged -= progress_ProgressChanged;
                
                if (Logger.LoggerInstance != null)
                {
                    Logger.LogException(ex);
                }

                MessageBox.Show("There was an error launching Media Browser: " + ex.Message);
                splash.Close();

                // Shutdown the app with an error code
                Shutdown(1);
            }
        }

        public async Task ReloadKernel()
        {
            var progress = new Progress<TaskProgress>();

            progress.ProgressChanged += progress_ProgressChanged;

            try
            {
                DateTime now = DateTime.UtcNow;

                await Kernel.Reload(progress);

                progress.ProgressChanged -= progress_ProgressChanged;

                Logger.LogInfo("Kernel.Reload completed in {0} seconds.", (DateTime.UtcNow - now).TotalSeconds);
            }
            catch (Exception ex)
            {
                progress.ProgressChanged -= progress_ProgressChanged;

                Logger.LogException(ex);

                // Shutdown the app with an error code
                Shutdown(1);
            }
        }

        void progress_ProgressChanged(object sender, TaskProgress e)
        {
            if (Logger.LoggerInstance != null)
            {
                Logger.LogInfo(e.Description);
            }
        }

        protected virtual void OnKernelLoaded()
        {

        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Kernel.Dispose();
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            OnSecondInstanceLaunched(args);

            return true;
        }

        protected virtual void OnSecondInstanceLaunched(IList<string> args)
        {
            if (this.MainWindow.WindowState == WindowState.Minimized)
            {
                this.MainWindow.WindowState = WindowState.Maximized;
            }            
        }

        public static void RunApplication<TApplicationType>(string uniqueKey)
            where TApplicationType : BaseApplication, IApplication, new()
        {
            if (SingleInstance<TApplicationType>.InitializeAsFirstInstance(uniqueKey))
            {
                var application = new TApplicationType();
                application.InitializeComponent();

                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<TApplicationType>.Cleanup();
            }
        }
    }

    public interface IApplication
    {
        void InitializeComponent();
    }
}
