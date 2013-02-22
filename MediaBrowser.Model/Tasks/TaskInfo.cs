using ProtoBuf;
using System;

namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Class TaskInfo
    /// </summary>
    [ProtoContract]
    public class TaskInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the state of the task.
        /// </summary>
        /// <value>The state of the task.</value>
        [ProtoMember(2)]
        public TaskState State { get; set; }

        /// <summary>
        /// Gets or sets the progress.
        /// </summary>
        /// <value>The progress.</value>
        [ProtoMember(3)]
        public double? CurrentProgressPercentage { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ProtoMember(4)]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the last execution result.
        /// </summary>
        /// <value>The last execution result.</value>
        [ProtoMember(5)]
        public TaskResult LastExecutionResult { get; set; }

        /// <summary>
        /// Gets or sets the triggers.
        /// </summary>
        /// <value>The triggers.</value>
        [ProtoMember(6)]
        public TaskTriggerInfo[] Triggers { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [ProtoMember(7)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the category.
        /// </summary>
        /// <value>The category.</value>
        [ProtoMember(8)]
        public string Category { get; set; }
    }
}
