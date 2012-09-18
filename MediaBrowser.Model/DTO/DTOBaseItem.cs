using MediaBrowser.Model.Entities;
using ProtoBuf;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.DTO
{
    /// <summary>
    /// This is strictly used as a data transfer object from the api layer.
    /// This holds information about a BaseItem in a format that is convenient for the client.
    /// </summary>
    [ProtoContract]
    public class DtoBaseItem : IHasProviderIds
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public Guid Id { get; set; }

        [ProtoMember(3)]
        public DateTime DateCreated { get; set; }

        [ProtoMember(4)]
        public string SortName { get; set; }

        [ProtoMember(5)]
        public DateTime? PremiereDate { get; set; }

        [ProtoMember(6)]
        public string Path { get; set; }

        [ProtoMember(7)]
        public string OfficialRating { get; set; }

        [ProtoMember(8)]
        public string Overview { get; set; }

        [ProtoMember(9)]
        public string[] Taglines { get; set; }

        [ProtoMember(10)]
        public string[] Genres { get; set; }

        [ProtoMember(11)]
        public string DisplayMediaType { get; set; }

        [ProtoMember(12)]
        public float? CommunityRating { get; set; }

        [ProtoMember(13)]
        public long? RunTimeTicks { get; set; }

        [ProtoMember(14)]
        public string AspectRatio { get; set; }

        [ProtoMember(15)]
        public int? ProductionYear { get; set; }

        [ProtoMember(16)]
        public int? IndexNumber { get; set; }

        [ProtoMember(17)]
        public int? ParentIndexNumber { get; set; }

        [ProtoMember(18)]
        public string TrailerUrl { get; set; }

        [ProtoMember(19)]
        public Dictionary<string, string> ProviderIds { get; set; }

        [ProtoMember(20)]
        public bool HasBanner { get; set; }

        [ProtoMember(21)]
        public bool HasArt { get; set; }

        [ProtoMember(22)]
        public bool HasLogo { get; set; }

        [ProtoMember(23)]
        public bool HasThumb { get; set; }

        [ProtoMember(24)]
        public bool HasPrimaryImage { get; set; }

        [ProtoMember(25)]
        public string Language { get; set; }

        [ProtoMember(26)]
        public int BackdropCount { get; set; }

        [ProtoMember(27)]
        public DtoBaseItem[] Children { get; set; }

        [ProtoMember(28)]
        public bool IsFolder { get; set; }

        /// <summary>
        /// If the item is a Folder this will determine if it's the Root or not
        /// </summary>
        [ProtoMember(29)]
        public bool? IsRoot { get; set; }

        /// <summary>
        /// If the item is a Folder this will determine if it's a VF or not
        /// </summary>
        [ProtoMember(30)]
        public bool? IsVirtualFolder { get; set; }

        [ProtoMember(31)]
        public Guid? ParentId { get; set; }

        [ProtoMember(32)]
        public string Type { get; set; }

        [ProtoMember(33)]
        public BaseItemPerson[] People { get; set; }

        [ProtoMember(34)]
        public BaseItemStudio[] Studios { get; set; }

        /// <summary>
        /// If the item does not have a logo, this will hold the Id of the Parent that has one.
        /// </summary>
        [ProtoMember(35)]
        public Guid? ParentLogoItemId { get; set; }

        /// <summary>
        /// If the item does not have any backdrops, this will hold the Id of the Parent that has one.
        /// </summary>
        [ProtoMember(36)]
        public Guid? ParentBackdropItemId { get; set; }

        [ProtoMember(37)]
        public int? ParentBackdropCount { get; set; }

        [ProtoMember(38)]
        public DtoBaseItem[] LocalTrailers { get; set; }

        [ProtoMember(39)]
        public int LocalTrailerCount { get; set; }

        /// <summary>
        /// User data for this item based on the user it's being requested for
        /// </summary>
        [ProtoMember(40)]
        public DtoUserItemData UserData { get; set; }

        [ProtoMember(41)]
        public ItemSpecialCounts SpecialCounts { get; set; }

        [ProtoMember(42)]
        public AudioInfo AudioInfo { get; set; }

        [ProtoMember(43)]
        public VideoInfo VideoInfo { get; set; }

        [ProtoMember(44)]
        public SeriesInfo SeriesInfo { get; set; }

        [ProtoMember(45)]
        public MovieInfo MovieInfo { get; set; }

        [ProtoMember(46)]
        public bool IsNew { get; set; }
        
        public bool IsType(Type type)
        {
            return IsType(type.Name);
        }

        public bool IsType(string type)
        {
            return Type.Equals(type, StringComparison.OrdinalIgnoreCase);
        }
    }
}
