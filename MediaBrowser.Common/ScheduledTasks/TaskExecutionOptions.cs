using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Common.ScheduledTasks
{
    /// <summary>
    /// A class that encomposases all common task run properties.
    /// </summary>
    public class TaskExecutionOptions
    {
        public int? MaxRuntimeMs { get; set; }
    }
}
