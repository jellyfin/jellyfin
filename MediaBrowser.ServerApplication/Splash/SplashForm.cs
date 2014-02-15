using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
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

    public static class ControlHelper
    {
        #region Redraw Suspend/Resume
        [DllImport("user32.dll", EntryPoint = "SendMessageA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        private const int WM_SETREDRAW = 0xB;

        public static void SuspendDrawing(this Control target)
        {
            SendMessage(target.Handle, WM_SETREDRAW, 0, 0);
        }

        public static void ResumeDrawing(this Control target) { ResumeDrawing(target, true); }
        public static void ResumeDrawing(this Control target, bool redraw)
        {
            SendMessage(target.Handle, WM_SETREDRAW, 1, 0);

            if (redraw)
            {
                target.Refresh();
            }
        }
        #endregion
    }
}
