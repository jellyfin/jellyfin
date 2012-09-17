using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private int _loadingImageIndex;
        public int LoadingImageIndex
        {
            get { return _loadingImageIndex; }
            set
            {
                _loadingImageIndex = value;
                OnPropertyChanged("LoadingImageIndex");
            }
        }

        #region Context Menu events

        private void cmOpenDashboard_click(object sender, RoutedEventArgs e)
        {
            App.OpenDashboard();
        }

        private void cmVisitCT_click(object sender, RoutedEventArgs e)
        {
            App.OpenUrl("http://community.mediabrowser.tv/");
        }

        private void cmExit_click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void cmdReloadServer_click(object sender, RoutedEventArgs e)
        {
            MbTaskbarIcon.ShowBalloonTip("Media Browser is reloading", "Please wait...", BalloonIcon.Info);

            LoadingImageIndex = 0;

            Timer timer = new Timer(LoadingIconTimerCallback, null, 0, 250);

            await (Application.Current as App).ReloadKernel().ConfigureAwait(false);

            timer.Dispose();

            LoadingImageIndex = 0;
        }

        private void LoadingIconTimerCallback(object stateInfo)
        {
            const int numImages = 4;

            if (LoadingImageIndex < numImages)
            {
                LoadingImageIndex++;
            }
            else
            {
                LoadingImageIndex = 1;
            }
        }

        #endregion

    }
}
