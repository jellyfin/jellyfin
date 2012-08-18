using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Model.Entities
{
    public abstract class BaseItem : BaseEntity, IHasProviderIds
    {
        /// <summary>
        /// Goes up the tree to find the virtual folder parent
        /// </summary>
        public VirtualFolder VirtualFolder
        {
            get
            {
                var vf = this as VirtualFolder;

                if (vf != null)
                {
                    return vf;
                }

                if (Parent != null)
                {
                    return Parent.VirtualFolder;
                }

                return null;
            }
        }

        public string SortName { get; set; }

        /// <summary>
        /// When the item first debuted. For movies this could be premiere date, episodes would be first aired
        /// </summary>
        public DateTime? PremiereDate { get; set; }

        public string Path { get; set; }

        public Folder Parent { get; set; }

        public string LogoImagePath { get; set; }

        public string ArtImagePath { get; set; }

        public string ThumbnailImagePath { get; set; }

        public string BannerImagePath { get; set; }

        public IEnumerable<string> BackdropImagePaths { get; set; }

        public string OfficialRating { get; set; }
        
        public string CustomRating { get; set; }

        public string Overview { get; set; }
        public IEnumerable<string> Taglines { get; set; }

        public IEnumerable<PersonInfo> People { get; set; }

        public IEnumerable<string> Studios { get; set; }

        public IEnumerable<string> Genres { get; set; }

        public string DisplayMediaType { get; set; }

        public float? UserRating { get; set; }
        public long? RunTimeTicks { get; set; }

        public string AspectRatio { get; set; }
        public int? ProductionYear { get; set; }

        /// <summary>
        /// If the item is part of a series, this is it's number in the series.
        /// This could be episode number, album track number, etc.
        /// </summary>
        public int? IndexNumber { get; set; }

        public IEnumerable<Video> LocalTrailers { get; set; }

        public string TrailerUrl { get; set; }

        public Dictionary<string, string> ProviderIds { get; set; }

        public Dictionary<Guid, UserItemData> UserData { get; set; }

        public UserItemData GetUserData(User user)
        {
            if (UserData == null || !UserData.ContainsKey(user.Id))
            {
                return null;
            }

            return UserData[user.Id];
        }

        public void AddUserData(User user, UserItemData data)
        {
            if (UserData == null)
            {
                UserData = new Dictionary<Guid, UserItemData>();
            }

            UserData[user.Id] = data;
        }

        /// <summary>
        /// Determines if a given user has access to this item
        /// </summary>
        internal bool IsParentalAllowed(User user)
        {
            return true;
        }

        /// <summary>
        /// Finds an item by ID, recursively
        /// </summary>
        public virtual BaseItem FindItemById(Guid id)
        {
            if (Id == id)
            {
                return this;
            }

            if (LocalTrailers != null)
            {
                return LocalTrailers.FirstOrDefault(i => i.Id == id);
            }

            return null;
        }
    }
}
