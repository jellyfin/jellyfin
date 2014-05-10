using MediaBrowser.Model.Tasks;
using System;

namespace MediaBrowser.Common.ScheduledTasks
{
    public class TaskCompletionEventArgs : EventArgs
    {
        public IScheduledTaskWorker Task { get; set; }

        public TaskResult Result { get; set; }
    }
}
