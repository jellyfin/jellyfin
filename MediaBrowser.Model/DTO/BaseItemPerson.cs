using ProtoBuf;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// This is used by the api to get information about a Person within a BaseItem
    /// </summary>
    [ProtoContract]
    public class BaseItemPerson : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        /// <value>The role.</value>
        [ProtoMember(2)]
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        [ProtoMember(3)]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the primary image tag.
        /// </summary>
        /// <value>The primary image tag.</value>
        [ProtoMember(4)]
        public Guid? PrimaryImageTag { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has primary image.
        /// </summary>
        /// <value><c>true</c> if this instance has primary image; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasPrimaryImage
        {
            get
            {
                return PrimaryImageTag.HasValue;
            }
        }

        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
