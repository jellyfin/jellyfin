using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MediaBrowser.Installer.Code
{
    /// <summary>
    /// Interaction logic for DownloadAnimation.xaml
    /// </summary>
    public partial class DownloadAnimation : UserControl
    {
        private int _i;
        private readonly double _startPos;
        private readonly DispatcherTimer _timer;

        public DownloadAnimation()
        {
            _i = 0;
            InitializeComponent();

            // Store start position of sliding canvas
            _startPos = Canvas.GetLeft(SlidingCanvas);

            // Create animation timer
            _timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(100)};
            _timer.Tick += TimerTick;
        }

        public void StartAnimation()
        {
            _timer.Start();
        }

        public void StopAnimation()
        {
            _timer.Stop();
        }

        private void TimerTick(object sender, EventArgs e)
        {
            _i++;

            if (_i < 16)
            {
                // Move SlidingCanvas containing the three colored dots 14 units to the right
                Canvas.SetLeft(SlidingCanvas, Canvas.GetLeft(SlidingCanvas) + 14);
            }
            else
            {
                // Move SlidingCanvas back to its starting position and reset counter
                _i = 0;
                Canvas.SetLeft(SlidingCanvas, _startPos);
            }
        }
    }
}
