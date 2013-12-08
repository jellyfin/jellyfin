using MediaBrowser.Controller.Plugins;
using System;
using System.Threading;

namespace MediaBrowser.ServerApplication.EntryPoints
{
    public class ResourceEntryPoint : IServerEntryPoint
    {
        private Timer _timer;

        public void Run()
        {
            _timer = new Timer(TimerCallback, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(30));
        }

        private void TimerCallback(object state)
        {
            try
            {
                // Bad practice, i know. But we keep a lot in memory, unfortunately.
                GC.Collect(2, GCCollectionMode.Forced, true);
                GC.Collect(2, GCCollectionMode.Forced, true);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
