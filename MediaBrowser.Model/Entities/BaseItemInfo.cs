using ProtoBuf;
using System;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// This is a stub class containing only basic information about an item
    /// </summary>
    [ProtoContract]
    public class BaseItemInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [ProtoMember(2)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        [ProtoMember(3)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is folder.
        /// </summary>
        /// <value><c>true</c> if this instance is folder; otherwise, <c>false</c>.</value>
        [ProtoMember(4)]
        public bool IsFolder { get; set; }

        /// <summary>
        /// Gets or sets the run time ticks.
        /// </summary>
        /// <value>The run time ticks.</value>
        [ProtoMember(5)]
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the primary image tag.
        /// </summary>
        /// <value>The primary image tag.</value>
        [ProtoMember(6)]
        public Guid? PrimaryImageTag { get; set; }

        /// <summary>
        /// Gets or sets the backdrop image tag.
        /// </summary>
        /// <value>The backdrop image tag.</value>
        [ProtoMember(7)]
        public Guid? BackdropImageTag { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has primary image.
        /// </summary>
        /// <value><c>true</c> if this instance has primary image; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasPrimaryImage
        {
            get { return PrimaryImageTag.HasValue; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has backdrop.
        /// </summary>
        /// <value><c>true</c> if this instance has backdrop; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasBackdrop
        {
            get { return BackdropImageTag.HasValue; }
        }
    }
}
