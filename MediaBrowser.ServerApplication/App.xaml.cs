using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Constants;
using MediaBrowser.Common.Implementations.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Server.Implementations;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Cache;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            // Look for the existence of an update archive
            var appPaths = new ServerApplicationPaths();
            var updateArchive = Path.Combine(appPaths.TempUpdatePath, Constants.MBServerPkgName + ".zip");
            if (File.Exists(updateArchive))
            {
                // Update is there - execute update
                try
                {
                    new ApplicationUpdater().UpdateApplication(MBApplication.MBServer, appPaths, updateArchive);

                    // And just let the app exit so it can update
                    return;
                }
                catch (Exception e)
                {
                    MessageBox.Show(string.Format("Error attempting to update application.\n\n{0}\n\n{1}", e.GetType().Name, e.Message));
                }
            }

            var application = new App();

            application.Run();
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static App Instance
        {
            get
            {
                return Current as App;
            }
        }

        /// <summary>
        /// The single instance mutex
        /// </summary>
        private Mutex SingleInstanceMutex;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the composition root.
        /// </summary>
        /// <value>The composition root.</value>
        protected ApplicationHost CompositionRoot { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="App" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public App()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the name of the uninstaller file.
        /// </summary>
        /// <value>The name of the uninstaller file.</value>
        protected string UninstallerFileName
        {
            get { return "MediaBrowser.Server.Uninstall.exe"; }
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
            var exception = (Exception)e.ExceptionObject;

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
        protected async void LoadKernel()
        {
            try
            {
                CompositionRoot = new ApplicationHost();

                Logger = CompositionRoot.LogManager.GetLogger("App");

                await CompositionRoot.Init();

                var win = new MainWindow(CompositionRoot.LogManager, CompositionRoot, CompositionRoot.ServerConfigurationManager, CompositionRoot.UserManager, CompositionRoot.LibraryManager, CompositionRoot.JsonSerializer);

                win.Show();
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
        /// Raises the <see cref="E:System.Windows.Application.Exit" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.Windows.ExitEventArgs" /> that contains the event data.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            ReleaseMutex();

            base.OnExit(e);

            CompositionRoot.Dispose();
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
        /// Opens the dashboard page.
        /// </summary>
        /// <param name="page">The page.</param>
        public static void OpenDashboardPage(string page, User loggedInUser, IServerConfigurationManager configurationManager)
        {
            var url = "http://localhost:" + configurationManager.Configuration.HttpServerPortNumber + "/" +
                      Kernel.Instance.WebApplicationName + "/dashboard/" + page;

            if (loggedInUser != null)
            {
                url = AddAutoLoginToDashboardUrl(url, loggedInUser);
            }

            OpenUrl(url);
        }

        /// <summary>
        /// Adds the auto login to dashboard URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="user">The user.</param>
        /// <returns>System.String.</returns>
        public static string AddAutoLoginToDashboardUrl(string url, User user)
        {
            if (url.IndexOf('?') == -1)
            {
                url += "?u=" + user.Id;
            }
            else
            {
                url += "&u=" + user.Id;
            }

            return url;
        }

        /// <summary>
        /// Opens the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
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

        /// <summary>
        /// Processes the exited.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        static void ProcessExited(object sender, EventArgs e)
        {
            ((Process)sender).Dispose();
        }

        /// <summary>
        /// Restarts this instance.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Restart()
        {
            Dispatcher.Invoke(ReleaseMutex);

            CompositionRoot.Dispose();

            System.Windows.Forms.Application.Restart();

            Dispatcher.Invoke(Shutdown);
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
    }
}
