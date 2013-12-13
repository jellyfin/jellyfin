using System;
using System.ComponentModel;
using System.Windows;

namespace MediaBrowser.ServerApplication.Splash
{
    /// <summary>
    /// Interaction logic for SplashWindow.xaml
    /// </summary>
    public partial class SplashWindow : Window
    {
        private readonly Progress<double> _progress;

        public SplashWindow(Version version, Progress<double> progress)
        {
            InitializeComponent();
            lblStatus.Text = string.Format("Loading Media Browser Server\nVersion {0}...", version);

            _progress = progress;

            progress.ProgressChanged += progress_ProgressChanged;
        }

        void progress_ProgressChanged(object sender, double e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                var width = e * 6.62;

                RectProgress.Width = width;
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _progress.ProgressChanged += progress_ProgressChanged;

            base.OnClosing(e);
        }
    }
}
