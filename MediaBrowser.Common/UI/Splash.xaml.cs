using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MahApps.Metro.Controls;
using MediaBrowser.Common.Logging;
using MediaBrowser.Model.Progress;

namespace MediaBrowser.Common.UI
{
    /// <summary>
    /// Interaction logic for Splash.xaml
    /// </summary>
    public partial class Splash : MetroWindow
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public Splash(Progress<TaskProgress> progress)
        {
            InitializeComponent();
            
            progress.ProgressChanged += progress_ProgressChanged;
            Loaded+=Splash_Loaded;
        }

        void progress_ProgressChanged(object sender, TaskProgress e)
        {
            // If logging has loaded, put a message in the log.
            if (Logger.LoggerInstance != null)
            {
                Logger.LogInfo(e.Description);
            }

            this.lblProgress.Content = e.Description;
            this.pbProgress.Value = (double)e.PercentComplete;
        }

        private void Splash_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

            // Setting this in markup throws an exception at runtime
            ShowTitleBar = false;
        }
    }
}
