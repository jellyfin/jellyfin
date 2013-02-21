using ProtoBuf;

namespace MediaBrowser.Model.Tasks
{
    [ProtoContract]
    public class TaskProgress
    {
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [ProtoMember(1)]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the percent complete.
        /// </summary>
        /// <value>The percent complete.</value>
        [ProtoMember(2)]
        public double PercentComplete { get; set; }
    }
}
