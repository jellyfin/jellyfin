using System;
using ProtoBuf;

namespace MediaBrowser.Model.DTO
{
    /// <summary>
    /// This is a stub class used by the api to get IBN types along with their item counts
    /// </summary>
    [ProtoContract]
    public class IbnItem
    {
        /// <summary>
        /// The name of the person, genre, etc
        /// </summary>
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary>
        /// The id of the person, genre, etc
        /// </summary>
        [ProtoMember(2)]
        public Guid Id { get; set; }

        [ProtoMember(3)]
        public bool HasImage { get; set; }

        /// <summary>
        /// The number of items that have the genre, year, studio, etc
        /// </summary>
        [ProtoMember(4)]
        public int BaseItemCount { get; set; }
    }

    /// <summary>
    /// This is used by the api to get information about a Person within a BaseItem
    /// </summary>
    [ProtoContract]
    public class BaseItemPerson
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public string Overview { get; set; }

        [ProtoMember(3)]
        public string Type { get; set; }

        [ProtoMember(4)]
        public bool HasImage { get; set; }
    }

    /// <summary>
    /// This is used by the api to get information about a studio within a BaseItem
    /// </summary>
    [ProtoContract]
    public class BaseItemStudio
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public bool HasImage { get; set; }
    }
}
