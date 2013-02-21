using MediaBrowser.Common.IO;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Deployment.Application;
using System.Net.Cache;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MediaBrowser.Common.UI
{
    /// <summary>
    /// Serves as a base Application class for both the UI and Server apps.
    /// </summary>
    public abstract class BaseApplication : Application, INotifyPropertyChanged
    {
        /// <summary>
        /// The single instance mutex
        /// </summary>
        private Mutex SingleInstanceMutex;
        /// <summary>
        /// Gets the name of the publisher.
        /// </summary>
        /// <value>The name of the publisher.</value>
        protected abstract string PublisherName { get; }
        /// <summary>
        /// Gets the name of the suite.
        /// </summary>
        /// <value>The name of the suite.</value>
        protected abstract string SuiteName { get; }
        /// <summary>
        /// Gets the name of the product.
        /// </summary>
        /// <value>The name of the product.</value>
        protected abstract string ProductName { get; }
        /// <summary>
        /// Gets the name of the uninstaller file.
        /// </summary>
        /// <value>The name of the uninstaller file.</value>
        protected abstract string UninstallerFileName { get; }

        /// <summary>
        /// Gets or sets a value indicating whether [last run at startup value].
        /// </summary>
        /// <value><c>null</c> if [last run at startup value] contains no value, <c>true</c> if [last run at startup value]; otherwise, <c>false</c>.</value>
        private bool? LastRunAtStartupValue { get; set; }

        /// <summary>
        /// Gets or sets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        protected IKernel Kernel { get; set; }

        /// <summary>
        /// Instantiates the kernel.
        /// </summary>
        /// <returns>IKernel.</returns>
        protected abstract IKernel InstantiateKernel();
        /// <summary>
        /// Instantiates the main window.
        /// </summary>
        /// <returns>Window.</returns>
        protected abstract Window InstantiateMainWindow();

        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Instantiates the iso manager.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <returns>IIsoManager.</returns>
        protected abstract IIsoManager InstantiateIsoManager(IKernel kernel);

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplication" /> class.
        /// </summary>
        protected BaseApplication()
        {
            Logger = LogManager.GetLogger("App");
        }

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="info">The info.</param>
        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                try
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(info));
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error in event handler", ex);
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Application.Startup" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.StartupEventArgs" /> that contains the event data.</param>
        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            SingleInstanceMutex = new Mutex(true, @"Local\" + GetType().Assembly.GetName().Name, out createdNew);
            if (!createdNew)
            {
                SingleInstanceMutex = null;
                Shutdown();
                return;
            }
            
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            LoadKernel();

            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
        }

        /// <summary>
        /// Handles the UnhandledException event of the CurrentDomain control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="UnhandledExceptionEventArgs" /> instance containing the event data.</param>
        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = (Exception) e.ExceptionObject;

            Logger.ErrorException("UnhandledException", exception);

            MessageBox.Show("Unhandled exception: " + exception.Message);
        }

        /// <summary>
        /// Handles the SessionEnding event of the SystemEvents control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SessionEndingEventArgs" /> instance containing the event data.</param>
        void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            // Try to shut down gracefully
            Shutdown();
        }

        /// <summary>
        /// Loads the kernel.
        /// </summary>
        protected virtual async void LoadKernel()
        {
            Kernel = InstantiateKernel();

            try
            {
                InstantiateMainWindow().Show();

                var now = DateTime.UtcNow;

                await Kernel.Init(InstantiateIsoManager(Kernel));

                var done = (DateTime.UtcNow - now);
                Logger.Info("Kernel.Init completed in {0}{1} minutes and {2} seconds.", done.Hours > 0 ? done.Hours + " Hours " : "", done.Minutes, done.Seconds);

                await OnKernelLoaded();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error launching application", ex);

                MessageBox.Show("There was an error launching Media Browser: " + ex.Message);

                // Shutdown the app with an error code
                Shutdown(1);
            }
        }

        /// <summary>
        /// Called when [kernel loaded].
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual Task OnKernelLoaded()
        {
            return Task.Run(() =>
            {
                Kernel.ApplicationRestartRequested += Kernel_ApplicationRestartRequested;
                Kernel.ConfigurationUpdated += Kernel_ConfigurationUpdated;

                ConfigureClickOnceStartup();
            });
        }

        /// <summary>
        /// Handles the ConfigurationUpdated event of the Kernel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void Kernel_ConfigurationUpdated(object sender, EventArgs e)
        {
            if (!LastRunAtStartupValue.HasValue || LastRunAtStartupValue.Value != Kernel.Configuration.RunAtStartup)
            {
                ConfigureClickOnceStartup();
            }
        }

        /// <summary>
        /// Configures the click once startup.
        /// </summary>
        private void ConfigureClickOnceStartup()
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
            {
                return;
            }

            try
            {
                var clickOnceHelper = new ClickOnceHelper(PublisherName, ProductName, SuiteName);

                if (Kernel.Configuration.RunAtStartup)
                {
                    clickOnceHelper.UpdateUninstallParameters(UninstallerFileName);
                    clickOnceHelper.AddShortcutToStartup();
                }
                else
                {
                    clickOnceHelper.RemoveShortcutFromStartup();
                }

                LastRunAtStartupValue = Kernel.Configuration.RunAtStartup;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error configuring ClickOnce", ex);
            }
        }

        /// <summary>
        /// Handles the ApplicationRestartRequested event of the Kernel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        void Kernel_ApplicationRestartRequested(object sender, EventArgs e)
        {
            Restart();
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        public void Restart()
        {
            Dispatcher.Invoke(ReleaseMutex);

            Kernel.Dispose();

            System.Windows.Forms.Application.Restart();

            Dispatcher.Invoke(Shutdown);
        }

        /// <summary>
        /// Releases the mutex.
        /// </summary>
        private void ReleaseMutex()
        {
            if (SingleInstanceMutex == null)
            {
                return;
            }

            SingleInstanceMutex.ReleaseMutex();
            SingleInstanceMutex.Close();
            SingleInstanceMutex.Dispose();
            SingleInstanceMutex = null;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Application.Exit" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.Windows.ExitEventArgs" /> that contains the event data.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            ReleaseMutex(); 
            
            base.OnExit(e);

            Kernel.Dispose();
        }

        /// <summary>
        /// Signals the external command line args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            OnSecondInstanceLaunched(args);

            return true;
        }

        /// <summary>
        /// Called when [second instance launched].
        /// </summary>
        /// <param name="args">The args.</param>
        protected virtual void OnSecondInstanceLaunched(IList<string> args)
        {
            if (MainWindow.WindowState == WindowState.Minimized)
            {
                MainWindow.WindowState = WindowState.Maximized;
            }
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>Image.</returns>
        /// <exception cref="System.ArgumentNullException">uri</exception>
        public Image GetImage(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentNullException("uri");
            }

            return GetImage(new Uri(uri));
        }

        /// <summary>
        /// Gets the image.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>Image.</returns>
        /// <exception cref="System.ArgumentNullException">uri</exception>
        public Image GetImage(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            return new Image { Source = GetBitmapImage(uri) };
        }

        /// <summary>
        /// Gets the bitmap image.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>BitmapImage.</returns>
        /// <exception cref="System.ArgumentNullException">uri</exception>
        public BitmapImage GetBitmapImage(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentNullException("uri");
            }

            return GetBitmapImage(new Uri(uri));
        }

        /// <summary>
        /// Gets the bitmap image.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns>BitmapImage.</returns>
        /// <exception cref="System.ArgumentNullException">uri</exception>
        public BitmapImage GetBitmapImage(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException("uri");
            }

            var bitmap = new BitmapImage
            {
                CreateOptions = BitmapCreateOptions.DelayCreation,
                CacheOption = BitmapCacheOption.OnDemand,
                UriCachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable)
            };

            bitmap.BeginInit();
            bitmap.UriSource = uri;
            bitmap.EndInit();

            RenderOptions.SetBitmapScalingMode(bitmap, BitmapScalingMode.Fant);
            return bitmap;
        }

        /// <summary>
        /// Runs the application.
        /// </summary>
        /// <typeparam name="TApplicationType">The type of the T application type.</typeparam>
        /// <param name="uniqueKey">The unique key.</param>
        public static void RunApplication<TApplicationType>(string uniqueKey)
            where TApplicationType : BaseApplication, IApplication, new()
        {
            var application = new TApplicationType();
            application.InitializeComponent();

            application.Run();
        }
    }

    /// <summary>
    /// Interface IApplication
    /// </summary>
    public interface IApplication
    {
        /// <summary>
        /// Initializes the component.
        /// </summary>
        void InitializeComponent();
    }
}
