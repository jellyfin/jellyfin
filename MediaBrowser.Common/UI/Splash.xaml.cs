using System;
using System.Windows;
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
            // Setting this in markup throws an exception at runtime
            ShowTitleBar = false;

            imgLogo.Source = (Application.Current as BaseApplication).GetLogoImage();
        }
    }
}
