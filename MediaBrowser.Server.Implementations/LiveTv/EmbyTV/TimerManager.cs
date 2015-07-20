using MediaBrowser.Common.Events;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public class TimerManager : ItemDataProvider<TimerInfo>
    {
        private readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler<GenericEventArgs<TimerInfo>> TimerFired;

        public TimerManager(IJsonSerializer jsonSerializer, ILogger logger, string dataPath)
            : base(jsonSerializer, logger, dataPath, (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase))
        {
        }

        public void RestartTimers()
        {
            StopTimers();
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

            Timer timer;
            if (_timers.TryRemove(item.Id, out timer))
            {
                timer.Dispose();
            }
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

        public void AddOrUpdate(TimerInfo item)
        {
            var list = GetAll().ToList();

            if (!list.Any(i => EqualityComparer(i, item)))
            {
                Add(item);
            }
            else
            {
                Update(item);
            }
        }

        private void AddTimer(TimerInfo item)
        {
            var timespan = RecordingHelper.GetStartTime(item) - DateTime.UtcNow;

            var timer = new Timer(TimerCallback, item.Id, timespan, TimeSpan.Zero);

            if (!_timers.TryAdd(item.Id, timer))
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
