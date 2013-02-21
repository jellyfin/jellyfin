using ProtoBuf;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class ChapterInfo
    /// </summary>
    [ProtoContract]
    public class ChapterInfoDto : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the start position ticks.
        /// </summary>
        /// <value>The start position ticks.</value>
        [ProtoMember(1)]
        public long StartPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(2)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the image tag.
        /// </summary>
        /// <value>The image tag.</value>
        [ProtoMember(3)]
        public Guid? ImageTag { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has image.
        /// </summary>
        /// <value><c>true</c> if this instance has image; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasImage
        {
            get { return ImageTag.HasValue; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
