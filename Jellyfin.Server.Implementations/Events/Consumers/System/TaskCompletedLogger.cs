using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Events.Consumers.System
{
    /// <summary>
    /// Creates an activity log entry whenever a task is completed.
    /// </summary>
    public class TaskCompletedLogger : IEventConsumer<TaskCompletionEventArgs>
    {
        private readonly ILocalizationManager _localizationManager;
        private readonly IActivityManager _activityManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCompletedLogger"/> class.
        /// </summary>
        /// <param name="localizationManager">The localization manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        public TaskCompletedLogger(ILocalizationManager localizationManager, IActivityManager activityManager)
        {
            _localizationManager = localizationManager;
            _activityManager = activityManager;
        }

        /// <inheritdoc />
        public async Task OnEvent(TaskCompletionEventArgs eventArgs)
        {
            var result = eventArgs.Result;
            var task = eventArgs.Task;

            if (task.ScheduledTask is IConfigurableScheduledTask activityTask
                && !activityTask.IsLogged)
            {
                return;
            }

            var time = result.EndTimeUtc - result.StartTimeUtc;
            var runningTime = string.Format(
                CultureInfo.InvariantCulture,
                _localizationManager.GetLocalizedString("LabelRunningTimeValue"),
                ToUserFriendlyString(time));

            if (result.Status == TaskCompletionStatus.Failed)
            {
                var vals = new List<string>();

                if (!string.IsNullOrEmpty(eventArgs.Result.ErrorMessage))
                {
                    vals.Add(eventArgs.Result.ErrorMessage);
                }

                if (!string.IsNullOrEmpty(eventArgs.Result.LongErrorMessage))
                {
                    vals.Add(eventArgs.Result.LongErrorMessage);
                }

                await _activityManager.CreateAsync(new ActivityLog(
                    string.Format(CultureInfo.InvariantCulture, _localizationManager.GetLocalizedString("ScheduledTaskFailedWithName"), task.Name),
                    NotificationType.TaskFailed.ToString(),
                    Guid.Empty)
                {
                    LogSeverity = LogLevel.Error,
                    Overview = string.Join(Environment.NewLine, vals),
                    ShortOverview = runningTime
                }).ConfigureAwait(false);
            }
        }

        private static string ToUserFriendlyString(TimeSpan span)
        {
            const int DaysInYear = 365;
            const int DaysInMonth = 30;

            // Get each non-zero value from TimeSpan component
            var values = new List<string>();

            // Number of years
            int days = span.Days;
            if (days >= DaysInYear)
            {
                int years = days / DaysInYear;
                values.Add(CreateValueString(years, "year"));
                days %= DaysInYear;
            }

            // Number of months
            if (days >= DaysInMonth)
            {
                int months = days / DaysInMonth;
                values.Add(CreateValueString(months, "month"));
                days = days % DaysInMonth;
            }

            // Number of days
            if (days >= 1)
            {
                values.Add(CreateValueString(days, "day"));
            }

            // Number of hours
            if (span.Hours >= 1)
            {
                values.Add(CreateValueString(span.Hours, "hour"));
            }

            // Number of minutes
            if (span.Minutes >= 1)
            {
                values.Add(CreateValueString(span.Minutes, "minute"));
            }

            // Number of seconds (include when 0 if no other components included)
            if (span.Seconds >= 1 || values.Count == 0)
            {
                values.Add(CreateValueString(span.Seconds, "second"));
            }

            // Combine values into string
            var builder = new StringBuilder();
            for (int i = 0; i < values.Count; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append(i == values.Count - 1 ? " and " : ", ");
                }

                builder.Append(values[i]);
            }

            // Return result
            return builder.ToString();
        }

        /// <summary>
        /// Constructs a string description of a time-span value.
        /// </summary>
        /// <param name="value">The value of this item.</param>
        /// <param name="description">The name of this item (singular form).</param>
        private static string CreateValueString(int value, string description)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:#,##0} {1}",
                value,
                value == 1 ? description : string.Format(CultureInfo.InvariantCulture, "{0}s", description));
        }
    }
}
