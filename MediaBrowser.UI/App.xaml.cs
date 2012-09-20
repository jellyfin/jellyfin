using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.UI;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Weather;
using MediaBrowser.UI.Controller;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MediaBrowser.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : BaseApplication, IApplication
    {
        private Timer ClockTimer { get; set; }
        private Timer ServerConfigurationTimer { get; set; }

        public static App Instance
        {
            get
            {
                return Application.Current as App;
            }
        }

        public DtoUser CurrentUser
        {
            get
            {
                return UIKernel.Instance.CurrentUser;
            }
            set
            {
                UIKernel.Instance.CurrentUser = value;
                OnPropertyChanged("CurrentUser");
            }
        }

        public ServerConfiguration ServerConfiguration
        {
            get
            {
                return UIKernel.Instance.ServerConfiguration;
            }
            set
            {
                UIKernel.Instance.ServerConfiguration = value;
                OnPropertyChanged("ServerConfiguration");
            }
        }

        private DateTime _currentTime = DateTime.Now;
        public DateTime CurrentTime
        {
            get
            {
                return _currentTime;
            }
            private set
            {
                _currentTime = value;
                OnPropertyChanged("CurrentTime");
            }
        }

        private WeatherInfo _currentWeather;
        public WeatherInfo CurrentWeather
        {
            get
            {
                return _currentWeather;
            }
            private set
            {
                _currentWeather = value;
                OnPropertyChanged("CurrentWeather");
            }
        }

        private BaseTheme _currentTheme;
        public BaseTheme CurrentTheme
        {
            get
            {
                return _currentTheme;
            }
            private set
            {
                _currentTheme = value;
                OnPropertyChanged("CurrentTheme");
            }
        }

        [STAThread]
        public static void Main()
        {
            RunApplication<App>("MediaBrowserUI");
        }

        #region BaseApplication Overrides
        protected override IKernel InstantiateKernel()
        {
            return new UIKernel();
        }

        protected override Window InstantiateMainWindow()
        {
            return new MainWindow();
        }

        protected override void OnKernelLoaded()
        {
            base.OnKernelLoaded();

            PropertyChanged += AppPropertyChanged;
            
            // Update every 10 seconds
            ClockTimer = new Timer(ClockTimerCallback, null, 0, 10000);

            // Update every 30 minutes
            ServerConfigurationTimer = new Timer(ServerConfigurationTimerCallback, null, 0, 1800000);

            CurrentTheme = UIKernel.Instance.Plugins.OfType<BaseTheme>().First();

            foreach (var resource in CurrentTheme.GlobalResources)
            {
                Resources.MergedDictionaries.Add(resource);
            }
        }
        #endregion

        async void AppPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("ServerConfiguration"))
            {
                if (string.IsNullOrEmpty(ServerConfiguration.WeatherZipCode))
                {
                    CurrentWeather = null;
                }
                else
                {
                    CurrentWeather = await UIKernel.Instance.ApiClient.GetWeatherInfoAsync(ServerConfiguration.WeatherZipCode);
                }
            }
        }

        private void ClockTimerCallback(object stateInfo)
        {
            CurrentTime = DateTime.Now;
        }

        private async void ServerConfigurationTimerCallback(object stateInfo)
        {
            ServerConfiguration = await UIKernel.Instance.ApiClient.GetServerConfigurationAsync();
        }

        public async Task<Image> GetImage(string url)
        {
            var image = new Image();

            image.Source = await GetBitmapImage(url);

            return image;
        }

        public async Task<BitmapImage> GetBitmapImage(string url)
        {
            Stream stream = await UIKernel.Instance.ApiClient.GetImageStreamAsync(url);

            BitmapImage bitmap = new BitmapImage();

            bitmap.CacheOption = BitmapCacheOption.Default;

            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.EndInit();

            return bitmap;
        }

        public async Task LogoutUser()
        {
            CurrentUser = null;

            if (ServerConfiguration.EnableUserProfiles)
            {
                Navigate(CurrentTheme.LoginPageUri);
            }
            else
            {
                DtoUser defaultUser = await UIKernel.Instance.ApiClient.GetDefaultUserAsync();
                CurrentUser = defaultUser;

                Navigate(new Uri("/Pages/HomePage.xaml", UriKind.Relative));
            }
        }

        public void Navigate(Uri uri)
        {
            (MainWindow as MainWindow).Navigate(uri);
        }
    }
}
