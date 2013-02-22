using System;
using System.Windows.Threading;

namespace MediaBrowser.UI.Extensions
{
    public static class Extensions
    {
        /// <summary>
        /// Invokes an action after a specified delay
        /// </summary>
        /// <param name="dispatcher">The dispatcher.</param>
        /// <param name="action">The action.</param>
        /// <param name="delayMs">The delay ms.</param>
        public static void InvokeWithDelay(this Dispatcher dispatcher, Action action, long delayMs)
        {
            var timer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher);
            timer.Interval = TimeSpan.FromMilliseconds(delayMs);
            timer.Tick += (sender, args) =>
                {
                    timer.Stop();
                    action();
                };
            timer.Start();
        }
    }
}
