#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
{
    public class TimerManager : ItemDataProvider<TimerInfo>
    {
        private readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>(StringComparer.OrdinalIgnoreCase);

        public TimerManager(IJsonSerializer jsonSerializer, ILogger logger, string dataPath)
            : base(jsonSerializer, logger, dataPath, (r1, r2) => string.Equals(r1.Id, r2.Id, StringComparison.OrdinalIgnoreCase))
        {
        }

        public event EventHandler<GenericEventArgs<TimerInfo>> TimerFired;

        public void RestartTimers()
        {
            StopTimers();

            foreach (var item in GetAll())
            {
                AddOrUpdateSystemTimer(item);
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
            AddOrUpdateSystemTimer(item);
        }

        public void AddOrUpdate(TimerInfo item, bool resetTimer)
        {
            if (resetTimer)
            {
                AddOrUpdate(item);
                return;
            }

            base.AddOrUpdate(item);
        }

        public override void AddOrUpdate(TimerInfo item)
        {
            base.AddOrUpdate(item);
            AddOrUpdateSystemTimer(item);
        }

        public override void Add(TimerInfo item)
        {
            if (string.IsNullOrEmpty(item.Id))
            {
                throw new ArgumentException("TimerInfo.Id cannot be null or empty.");
            }

            base.Add(item);
            AddOrUpdateSystemTimer(item);
        }

        private static bool ShouldStartTimer(TimerInfo item)
        {
            if (item.Status == RecordingStatus.Completed
                || item.Status == RecordingStatus.Cancelled)
            {
                return false;
            }

            return true;
        }

        private void AddOrUpdateSystemTimer(TimerInfo item)
        {
            StopTimer(item);

            if (!ShouldStartTimer(item))
            {
                return;
            }

            var startDate = RecordingHelper.GetStartTime(item);
            var now = DateTime.UtcNow;

            if (startDate < now)
            {
                TimerFired?.Invoke(this, new GenericEventArgs<TimerInfo>(item));
                return;
            }

            var dueTime = startDate - now;
            StartTimer(item, dueTime);
        }

        private void StartTimer(TimerInfo item, TimeSpan dueTime)
        {
            var timer = new Timer(TimerCallback, item.Id, dueTime, TimeSpan.Zero);

            if (_timers.TryAdd(item.Id, timer))
            {
                Logger.LogInformation(
                    "Creating recording timer for {Id}, {Name}. Timer will fire in {Minutes} minutes",
                    item.Id,
                    item.Name,
                    dueTime.TotalMinutes.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                timer.Dispose();
                Logger.LogWarning("Timer already exists for item {Id}", item.Id);
            }
        }

        private void StopTimer(TimerInfo item)
        {
            if (_timers.TryRemove(item.Id, out var timer))
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
                TimerFired?.Invoke(this, new GenericEventArgs<TimerInfo>(timer));
            }
        }

        public TimerInfo GetTimer(string id)
        {
            return GetAll().FirstOrDefault(r => string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public TimerInfo GetTimerByProgramId(string programId)
        {
            return GetAll().FirstOrDefault(r => string.Equals(r.ProgramId, programId, StringComparison.OrdinalIgnoreCase));
        }
    }
}
