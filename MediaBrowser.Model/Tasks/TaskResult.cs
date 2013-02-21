using ProtoBuf;
using System;

namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Class TaskExecutionInfo
    /// </summary>
    [ProtoContract]
    public class TaskResult
    {
        /// <summary>
        /// Gets or sets the start time UTC.
        /// </summary>
        /// <value>The start time UTC.</value>
        [ProtoMember(1)]
        public DateTime StartTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets the end time UTC.
        /// </summary>
        /// <value>The end time UTC.</value>
        [ProtoMember(2)]
        public DateTime EndTimeUtc { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        [ProtoMember(3)]
        public TaskCompletionStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(4)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ProtoMember(5)]
        public Guid Id { get; set; }
    }
}
