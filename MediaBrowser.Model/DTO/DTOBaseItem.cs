using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.DTO
{
    /// <summary>
    /// This is strictly used as a data transfer object from the api layer.
    /// This holds information about a BaseItem in a format that is convenient for the client.
    /// </summary>
    public class DTOBaseItem : IHasProviderIds
    {
        public string Name { get; set; }
        public Guid Id { get; set; }
        public DateTime DateCreated { get; set; }

        public string SortName { get; set; }
        public DateTime? PremiereDate { get; set; }
        public string Path { get; set; }
        public string OfficialRating { get; set; }
        public string Overview { get; set; }
        public IEnumerable<string> Taglines { get; set; }

        public IEnumerable<string> Genres { get; set; }

        public string DisplayMediaType { get; set; }

        public float? UserRating { get; set; }
        public long? RunTimeTicks { get; set; }

        public string AspectRatio { get; set; }
        public int? ProductionYear { get; set; }

        public int? IndexNumber { get; set; }

        public string TrailerUrl { get; set; }

        public Dictionary<string, string> ProviderIds { get; set; }

        public bool HasBanner { get; set; }
        public bool HasArt { get; set; }
        public bool HasLogo { get; set; }
        public bool HasThumb { get; set; }
        public bool HasPrimaryImage { get; set; }

        public int BackdropCount { get; set; }

        public IEnumerable<DTOBaseItem> Children { get; set; }

        public bool IsFolder { get; set; }

        /// <summary>
        /// If the item is a Folder this will determine if it's the Root or not
        /// </summary>
        public bool? IsRoot { get; set; }

        /// <summary>
        /// If the item is a Folder this will determine if it's a VF or not
        /// </summary>
        public bool? IsVirtualFolder { get; set; }
        
        public Guid? ParentId { get; set; }

        public string Type { get; set; }

        public IEnumerable<BaseItemPerson> People { get; set; }
        public IEnumerable<BaseItemStudio> Studios { get; set; }

        /// <summary>
        /// If the item does not have a logo, this will hold the Id of the Parent that has one.
        /// </summary>
        public Guid? ParentLogoItemId { get; set; }

        /// <summary>
        /// If the item does not have any backdrops, this will hold the Id of the Parent that has one.
        /// </summary>
        public Guid? ParentBackdropItemId { get; set; }
        public int? ParentBackdropCount { get; set; }

        public IEnumerable<Video> LocalTrailers { get; set; }
        public int LocalTrailerCount { get; set; }

        /// <summary>
        /// User data for this item based on the user it's being requested for
        /// </summary>
        public UserItemData UserData { get; set; }

        public ItemSpecialCounts SpecialCounts { get; set; }

        public bool IsType(Type type)
        {
            return IsType(type.Name);
        }

        public bool IsType(string type)
        {
            return Type.Equals(type, StringComparison.OrdinalIgnoreCase);
        }

        public bool IsNew { get; set; }
    }
}
