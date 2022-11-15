#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Tasks
{
    public class TaskCompletionEventArgs : EventArgs
    {
        public TaskCompletionEventArgs(IScheduledTaskWorker task, TaskResult result)
        {
            Task = task;
            Result = result;
        }

        public IScheduledTaskWorker Task { get; }

        public TaskResult Result { get; }
    }
}
