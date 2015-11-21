using MediaBrowser.Common.Events;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using CommonIO;
using MediaBrowser.Common.IO;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public class TimerManager : ItemDataProvider<TimerInfo>
    {
        private readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<GenericEventArgs<TimerInfo>> TimerFired;

        public TimerManager(IFileSystem fileSystem, IJsonSerializer jsonSerializer, ILogger logger, string dataPath)
            : base(fileSystem, jsonSerializer, logger, dataPath, (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase))
        {
        }

        public void RestartTimers()
        {
            StopTimers();

            foreach (var item in GetAll().ToList())
            {
                AddTimer(item);
            }
        }

        public void StopTimers()
        {
            foreach (var pair in _timers.ToList())
            {
                pair.Value.Dispose();
            }

            _timers.Clear();
        }

        public override void Delete(TimerInfo item)
        {
            base.Delete(item);
            StopTimer(item);
        }

        public override void Update(TimerInfo item)
        {
            base.Update(item);

            Timer timer;
            if (_timers.TryGetValue(item.Id, out timer))
            {
                var timespan = RecordingHelper.GetStartTime(item) - DateTime.UtcNow;
                timer.Change(timespan, TimeSpan.Zero);
            }
            else
            {
                AddTimer(item);
            }
        }

        public override void Add(TimerInfo item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                throw new ArgumentException("TimerInfo.Id cannot be null or empty.");
            }

            base.Add(item);
            AddTimer(item);
        }

        private void AddTimer(TimerInfo item)
        {
            var startDate = RecordingHelper.GetStartTime(item);
            var now = DateTime.UtcNow;

            if (startDate < now)
            {
                EventHelper.FireEventIfNotNull(TimerFired, this, new GenericEventArgs<TimerInfo> { Argument = item }, Logger);
                return;
            }

            var timerLength = startDate - now;
            StartTimer(item, timerLength);
        }

        public void StartTimer(TimerInfo item, TimeSpan length)
        {
            StopTimer(item);

            var timer = new Timer(TimerCallback, item.Id, length, TimeSpan.Zero);

            if (!_timers.TryAdd(item.Id, timer))
            {
                timer.Dispose();
            }
        }

        private void StopTimer(TimerInfo item)
        {
            Timer timer;
            if (_timers.TryRemove(item.Id, out timer))
            {
                timer.Dispose();
            }
        }

        private void TimerCallback(object state)
        {
            var timerId = (string)state;

            var timer = GetAll().FirstOrDefault(i => string.Equals(i.Id, timerId, StringComparison.OrdinalIgnoreCase));
            if (timer != null)
            {
                EventHelper.FireEventIfNotNull(TimerFired, this, new GenericEventArgs<TimerInfo> { Argument = timer }, Logger);
            }
        }
    }
}
