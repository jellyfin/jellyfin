using MediaBrowser.Common.Events;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using CommonIO;
using MediaBrowser.Controller.Power;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public class TimerManager : ItemDataProvider<TimerInfo>
    {
        private readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);
        private readonly IPowerManagement _powerManagement;
        private readonly ILogger _logger;

        public event EventHandler<GenericEventArgs<TimerInfo>> TimerFired;

        public TimerManager(IFileSystem fileSystem, IJsonSerializer jsonSerializer, ILogger logger, string dataPath, IPowerManagement powerManagement, ILogger logger1)
            : base(fileSystem, jsonSerializer, logger, dataPath, (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase))
        {
            _powerManagement = powerManagement;
            _logger = logger1;
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
                ScheduleWake(item);
            }
            else
            {
                AddTimer(item);
            }
        }

        public void AddOrUpdate(TimerInfo item, bool resetTimer)
        {
            if (resetTimer)
            {
                AddOrUpdate(item);
                return;
            }

            var list = GetAll().ToList();

            if (!list.Any(i => EqualityComparer(i, item)))
            {
                base.Add(item);
            }
            else
            {
                base.Update(item);
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
            ScheduleWake(item);
        }

        private void AddTimer(TimerInfo item)
        {
            if (item.Status == RecordingStatus.Completed)
            {
                return;
            }

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

        private void ScheduleWake(TimerInfo info)
        {
            var startDate = RecordingHelper.GetStartTime(info).AddMinutes(-5);

            try
            {
                _powerManagement.ScheduleWake(startDate);
                _logger.Info("Scheduled system wake timer at {0} (UTC)", startDate);
            }
            catch (NotImplementedException)
            {

            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error scheduling wake timer", ex);
            }
        }

        public void StartTimer(TimerInfo item, TimeSpan dueTime)
        {
            StopTimer(item);

            var timer = new Timer(TimerCallback, item.Id, dueTime, TimeSpan.Zero);

            if (_timers.TryAdd(item.Id, timer))
            {
                _logger.Info("Creating recording timer for {0}, {1}. Timer will fire in {2} minutes", item.Id, item.Name, dueTime.TotalMinutes.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                timer.Dispose();
                _logger.Warn("Timer already exists for item {0}", item.Id);
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
