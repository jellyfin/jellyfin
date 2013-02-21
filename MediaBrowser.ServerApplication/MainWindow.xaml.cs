using Hardcodet.Wpf.TaskbarNotification;
using MediaBrowser.Common.Events;
using MediaBrowser.Controller;
using MediaBrowser.Model.Progress;
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
        private Timer LoadingIconTimer { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindowLoaded;
        }

        void MainWindowLoaded(object sender, RoutedEventArgs e)
        {
            DataContext = this;

            Kernel.Instance.ReloadBeginning += KernelReloadBeginning;
            Kernel.Instance.ReloadCompleted += KernelReloadCompleted;
        }

        void KernelReloadBeginning(object sender, GenericEventArgs<IProgress<TaskProgress>> e)
        {
            MbTaskbarIcon.ShowBalloonTip("Media Browser is reloading", "Please wait...", BalloonIcon.Info);

            LoadingImageIndex = 0;

            LoadingIconTimer = new Timer(LoadingIconTimerCallback, null, 0, 250);
        }

        void KernelReloadCompleted(object sender, GenericEventArgs<IProgress<TaskProgress>> e)
        {
            LoadingIconTimer.Dispose();

            LoadingImageIndex = 0;
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
            await Kernel.Instance.Reload(new Progress<TaskProgress>()).ConfigureAwait(false);
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
