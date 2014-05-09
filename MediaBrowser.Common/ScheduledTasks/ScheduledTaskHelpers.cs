using MediaBrowser.Model.Tasks;
using System;
using System.Linq;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// Class ScheduledTaskHelpers
    /// </summary>
    public static class ScheduledTaskHelpers
    {
        /// <summary>
        /// Gets the task info.
        /// </summary>
        /// <param name="task">The task.</param>
        /// <returns>TaskInfo.</returns>
        public static TaskInfo GetTaskInfo(IScheduledTaskWorker task)
        {
            var isHidden = false;

            var configurableTask = task.ScheduledTask as IConfigurableScheduledTask;

            if (configurableTask != null)
            {
                isHidden = configurableTask.IsHidden;
            }

            string key = null;

            var hasKey = task.ScheduledTask as IHasKey;

            if (hasKey != null)
            {
                key = hasKey.Key;
            }
            return new TaskInfo
            {
                Name = task.Name,
                CurrentProgressPercentage = task.CurrentProgress,
                State = task.State,
                Id = task.Id,
                LastExecutionResult = task.LastExecutionResult,
                Triggers = task.Triggers.Select(GetTriggerInfo).ToList(),
                Description = task.Description,
                Category = task.Category,
                IsHidden = isHidden,
                Key = key
            };
        }

        /// <summary>
        /// Gets the trigger info.
        /// </summary>
        /// <param name="trigger">The trigger.</param>
        /// <returns>TaskTriggerInfo.</returns>
        public static TaskTriggerInfo GetTriggerInfo(ITaskTrigger trigger)
        {
            var info = new TaskTriggerInfo
            {
                Type = trigger.GetType().Name
            };

            var dailyTrigger = trigger as DailyTrigger;

            if (dailyTrigger != null)
            {
                info.TimeOfDayTicks = dailyTrigger.TimeOfDay.Ticks;
            }

            var weeklyTaskTrigger = trigger as WeeklyTrigger;

            if (weeklyTaskTrigger != null)
            {
                info.TimeOfDayTicks = weeklyTaskTrigger.TimeOfDay.Ticks;
                info.DayOfWeek = weeklyTaskTrigger.DayOfWeek;
            }

            var intervalTaskTrigger = trigger as IntervalTrigger;

            if (intervalTaskTrigger != null)
            {
                info.IntervalTicks = intervalTaskTrigger.Interval.Ticks;
            }

            var systemEventTrigger = trigger as SystemEventTrigger;

            if (systemEventTrigger != null)
            {
                info.SystemEvent = systemEventTrigger.SystemEvent;
            }

            return info;
        }

        /// <summary>
        /// Converts a TaskTriggerInfo into a concrete BaseTaskTrigger
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>BaseTaskTrigger.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.ArgumentException">Invalid trigger type:  + info.Type</exception>
        public static ITaskTrigger GetTrigger(TaskTriggerInfo info)
        {
            if (info.Type.Equals(typeof(DailyTrigger).Name, StringComparison.OrdinalIgnoreCase))
            {
                if (!info.TimeOfDayTicks.HasValue)
                {
                    throw new ArgumentNullException();
                }

                return new DailyTrigger
                {
                    TimeOfDay = TimeSpan.FromTicks(info.TimeOfDayTicks.Value)
                };
            }

            if (info.Type.Equals(typeof(WeeklyTrigger).Name, StringComparison.OrdinalIgnoreCase))
            {
                if (!info.TimeOfDayTicks.HasValue)
                {
                    throw new ArgumentNullException();
                }

                if (!info.DayOfWeek.HasValue)
                {
                    throw new ArgumentNullException();
                }

                return new WeeklyTrigger
                {
                    TimeOfDay = TimeSpan.FromTicks(info.TimeOfDayTicks.Value),
                    DayOfWeek = info.DayOfWeek.Value
                };
            }

            if (info.Type.Equals(typeof(IntervalTrigger).Name, StringComparison.OrdinalIgnoreCase))
            {
                if (!info.IntervalTicks.HasValue)
                {
                    throw new ArgumentNullException();
                }

                return new IntervalTrigger
                {
                    Interval = TimeSpan.FromTicks(info.IntervalTicks.Value)
                };
            }

            if (info.Type.Equals(typeof(SystemEventTrigger).Name, StringComparison.OrdinalIgnoreCase))
            {
                if (!info.SystemEvent.HasValue)
                {
                    throw new ArgumentNullException();
                }

                return new SystemEventTrigger
                {
                    SystemEvent = info.SystemEvent.Value
                };
            }

            if (info.Type.Equals(typeof(StartupTrigger).Name, StringComparison.OrdinalIgnoreCase))
            {
                return new StartupTrigger();
            }

            throw new ArgumentException("Unrecognized trigger type: " + info.Type);
        }
    }
}
