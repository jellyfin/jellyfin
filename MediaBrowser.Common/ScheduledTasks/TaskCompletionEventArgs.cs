using MediaBrowser.Model.Tasks;
using System;

namespace MediaBrowser.Common.ScheduledTasks
{
    public class TaskCompletionEventArgs : EventArgs
    {
        public IScheduledTask Task { get; set; }

        public TaskResult Result { get; set; }
    }
}
