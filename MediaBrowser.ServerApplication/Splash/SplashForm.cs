using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBrowser.ServerApplication.Splash
{
    public partial class SplashForm : Form
    {
        private readonly TaskScheduler _uiThread;
        
        private readonly Progress<double> _progress;

        public SplashForm(Version version, Progress<double> progress)
        {
            InitializeComponent();

            lblVersion.Text = string.Format("Version {0}...", version);

            _progress = progress;
            
            progress.ProgressChanged += progress_ProgressChanged;
            _uiThread = TaskScheduler.FromCurrentSynchronizationContext();
        }

        async void progress_ProgressChanged(object sender, double e)
        {
            await Task.Factory.StartNew(() =>
            {
                var width = e * 6.48;

                panelProgress.Width = Convert.ToInt32(width);

            }, CancellationToken.None, TaskCreationOptions.None, _uiThread);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _progress.ProgressChanged += progress_ProgressChanged;

            base.OnClosing(e);
        }
    }
}
