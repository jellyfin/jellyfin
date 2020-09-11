namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Class ScheduledTaskHelpers.
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

            if (task.ScheduledTask is IConfigurableScheduledTask configurableTask)
            {
                isHidden = configurableTask.IsHidden;
            }

            string key = task.ScheduledTask.Key;

            return new TaskInfo
            {
                Name = task.Name,
                CurrentProgressPercentage = task.CurrentProgress,
                State = task.State,
                Id = task.Id,
                LastExecutionResult = task.LastExecutionResult,

                Triggers = task.Triggers,

                Description = task.Description,
                Category = task.Category,
                IsHidden = isHidden,
                Key = key
            };
        }
    }
}
