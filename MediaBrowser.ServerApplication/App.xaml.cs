using MediaBrowser.ClickOnce;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Controller;
using MediaBrowser.IsoMounter;
using MediaBrowser.Logging.Nlog;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Updates;
using MediaBrowser.Server.Uninstall;
using MediaBrowser.ServerApplication.Implementations;
using Microsoft.Win32;
using SimpleInjector;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Cache;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IApplicationHost
    {
        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            var application = new App(new NLogger("App"));

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
        /// Gets or sets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        protected IKernel Kernel { get; set; }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the log file path.
        /// </summary>
        /// <value>The log file path.</value>
        public string LogFilePath { get; private set; }

        /// <summary>
        /// The container
        /// </summary>
        private Container _container = new Container();

        /// <summary>
        /// Initializes a new instance of the <see cref="App" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public App(ILogger logger)
        {
            Logger = logger;

            InitializeComponent();
        }

        /// <summary>
        /// Gets the name of the product.
        /// </summary>
        /// <value>The name of the product.</value>
        protected string ProductName
        {
            get { return Globals.ProductName; }
        }

        /// <summary>
        /// Gets the name of the publisher.
        /// </summary>
        /// <value>The name of the publisher.</value>
        protected string PublisherName
        {
            get { return Globals.PublisherName; }
        }

        /// <summary>
        /// Gets the name of the suite.
        /// </summary>
        /// <value>The name of the suite.</value>
        protected string SuiteName
        {
            get { return Globals.SuiteName; }
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
        /// Gets or sets the iso manager.
        /// </summary>
        /// <value>The iso manager.</value>
        private IIsoManager IsoManager { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [last run at startup value].
        /// </summary>
        /// <value><c>null</c> if [last run at startup value] contains no value, <c>true</c> if [last run at startup value]; otherwise, <c>false</c>.</value>
        private bool? LastRunAtStartupValue { get; set; }

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
            RegisterResources();

            Kernel = new Kernel(this, Logger);

            try
            {
                new MainWindow(Logger).Show();

                var now = DateTime.UtcNow;

                await Kernel.Init();

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
        protected Task OnKernelLoaded()
        {
            return Task.Run(() =>
            {
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
            try
            {
                ClickOnceHelper.ConfigureClickOnceStartupIfInstalled(PublisherName, ProductName, SuiteName, Kernel.Configuration.RunAtStartup, UninstallerFileName);

                LastRunAtStartupValue = Kernel.Configuration.RunAtStartup;
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error configuring ClickOnce", ex);
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

            Kernel.Dispose();
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
        /// Opens the dashboard.
        /// </summary>
        public static void OpenDashboard()
        {
            OpenDashboardPage("dashboard.html");
        }

        /// <summary>
        /// Opens the dashboard page.
        /// </summary>
        /// <param name="page">The page.</param>
        public static void OpenDashboardPage(string page)
        {
            var url = "http://localhost:" + Controller.Kernel.Instance.Configuration.HttpServerPortNumber + "/" +
                      Controller.Kernel.Instance.WebApplicationName + "/dashboard/" + page;

            url = AddAutoLoginToDashboardUrl(url);

            OpenUrl(url);
        }

        /// <summary>
        /// Adds the auto login to dashboard URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        public static string AddAutoLoginToDashboardUrl(string url)
        {
            var user = Controller.Kernel.Instance.Users.FirstOrDefault(u => u.Configuration.IsAdministrator);

            if (user != null)
            {
                if (url.IndexOf('?') == -1)
                {
                    url += "?u=" + user.Id;
                }
                else
                {
                    url += "&u=" + user.Id;
                }
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

            Kernel.Dispose();

            System.Windows.Forms.Application.Restart();

            Dispatcher.Invoke(Shutdown);
        }

        /// <summary>
        /// Reloads the logger.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void ReloadLogger()
        {
            LogFilePath = Path.Combine(Kernel.ApplicationPaths.LogDirectoryPath, "Server-" + DateTime.Now.Ticks + ".log");

            NlogManager.AddFileTarget(LogFilePath, Kernel.Configuration.EnableDebugLevelLogging);
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
        /// Gets or sets a value indicating whether this instance can self update.
        /// </summary>
        /// <value><c>true</c> if this instance can self update; otherwise, <c>false</c>.</value>
        public bool CanSelfUpdate
        {
            get { return ClickOnceHelper.IsNetworkDeployed; }
        }

        /// <summary>
        /// Checks for update.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task{CheckForUpdateResult}.</returns>
        public Task<CheckForUpdateResult> CheckForApplicationUpdate(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new ApplicationUpdateCheck().CheckForApplicationUpdate(cancellationToken, progress);
        }

        /// <summary>
        /// Updates the application.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        public Task UpdateApplication(CancellationToken cancellationToken, IProgress<double> progress)
        {
            return new ApplicationUpdater().UpdateApplication(cancellationToken, progress);
        }

        /// <summary>
        /// Registers resources that classes will depend on
        /// </summary>
        private void RegisterResources()
        {
            Register<IApplicationHost>(this);
            Register(Logger);

            IsoManager = new PismoIsoManager(Logger);

            Register<IIsoManager>(IsoManager);
            Register<IBlurayExaminer>(new BdInfoExaminer());
            Register<IZipClient>(new DotNetZipClient());
        }

        /// <summary>
        /// Creates an instance of type and resolves all constructor dependancies
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        public object CreateInstance(Type type)
        {
            try
            {
                return _container.GetInstance(type);
            }
            catch
            {
                Logger.Error("Error creating {0}", type.Name);

                throw;
            }
        }

        /// <summary>
        /// Registers the specified obj.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The obj.</param>
        public void Register<T>(T obj)
            where T : class
        {
            _container.RegisterSingle(obj);
        }

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T Resolve<T>()
        {
            return (T)_container.GetRegistration(typeof (T), true).GetInstance();
        }

        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T TryResolve<T>()
        {
            var result = _container.GetRegistration(typeof (T), false);

            if (result == null)
            {
                return default(T);
            }
            return (T)result.GetInstance();
        }
    }
}
