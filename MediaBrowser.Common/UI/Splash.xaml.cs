using MahApps.Metro.Controls;
using MediaBrowser.Model.Progress;
using System;
using System.Windows;

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
            
            progress.ProgressChanged += ProgressChanged;
            Loaded+=SplashLoaded;
        }

        void ProgressChanged(object sender, TaskProgress e)
        {
            lblProgress.Text = e.Description + "...";
        }

        private void SplashLoaded(object sender, RoutedEventArgs e)
        {
            // Setting this in markup throws an exception at runtime
            ShowTitleBar = false;
        }
    }
}
