using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.IO;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    public abstract class BaseItem : BaseEntity, IHasProviderIds
    {

        public IEnumerable<string> PhysicalLocations
        {
            get
            {
                return _resolveArgs.PhysicalLocations;
            }
        }

        public string SortName { get; set; }

        /// <summary>
        /// When the item first debuted. For movies this could be premiere date, episodes would be first aired
        /// </summary>
        public DateTime? PremiereDate { get; set; }

        public string LogoImagePath { get; set; }

        public string ArtImagePath { get; set; }

        public string ThumbnailImagePath { get; set; }

        public string BannerImagePath { get; set; }

        public IEnumerable<string> BackdropImagePaths { get; set; }

        public string OfficialRating { get; set; }
        
        public string CustomRating { get; set; }
        public string CustomPin { get; set; }

        public string Language { get; set; }
        public string Overview { get; set; }
        public List<string> Taglines { get; set; }

        /// <summary>
        /// Using a Dictionary to prevent duplicates
        /// </summary>
        public Dictionary<string,PersonInfo> People { get; set; }

        public List<string> Studios { get; set; }

        public List<string> Genres { get; set; }

        public string DisplayMediaType { get; set; }

        public float? CommunityRating { get; set; }
        public long? RunTimeTicks { get; set; }

        public string AspectRatio { get; set; }
        public int? ProductionYear { get; set; }

        /// <summary>
        /// If the item is part of a series, this is it's number in the series.
        /// This could be episode number, album track number, etc.
        /// </summary>
        public int? IndexNumber { get; set; }

        /// <summary>
        /// For an episode this could be the season number, or for a song this could be the disc number.
        /// </summary>
        public int? ParentIndexNumber { get; set; }

        public IEnumerable<Video> LocalTrailers { get; set; }

        public string TrailerUrl { get; set; }

        public Dictionary<string, string> ProviderIds { get; set; }

        public Dictionary<Guid, UserItemData> UserData { get; set; }

        public UserItemData GetUserData(User user, bool createIfNull)
        {
            if (UserData == null || !UserData.ContainsKey(user.Id))
            {
                if (createIfNull)
                {
                    AddUserData(user, new UserItemData());
                }
                else
                {
                    return null;
                }
            }

            return UserData[user.Id];
        }

        private void AddUserData(User user, UserItemData data)
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

        public virtual bool IsFolder
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Determine if we have changed vs the passed in copy
        /// </summary>
        /// <param name="copy"></param>
        /// <returns></returns>
        public virtual bool IsChanged(BaseItem copy)
        {
            bool changed = copy.DateModified != this.DateModified;
            if (changed) MediaBrowser.Common.Logging.Logger.LogDebugInfo(this.Name + " changed - original creation: " + this.DateCreated + " new creation: " + copy.DateCreated + " original modified: " + this.DateModified + " new modified: " + copy.DateModified);
            return changed;
        }

        /// <summary>
        /// Determines if the item is considered new based on user settings
        /// </summary>
        public bool IsRecentlyAdded(User user)
        {
            return (DateTime.UtcNow - DateCreated).TotalDays < user.RecentItemDays;
        }

        public void AddPerson(PersonInfo person)
        {
            if (People == null)
            {
                People = new Dictionary<string, PersonInfo>(StringComparer.OrdinalIgnoreCase);
            }

            People[person.Name] = person;
        }

        /// <summary>
        /// Marks the item as either played or unplayed
        /// </summary>
        public virtual void SetPlayedStatus(User user, bool wasPlayed)
        {
            UserItemData data = GetUserData(user, true);

            if (wasPlayed)
            {
                data.PlayCount = Math.Max(data.PlayCount, 1);
            }
            else
            {
                data.PlayCount = 0;
                data.PlaybackPositionTicks = 0;
            }
        }

        /// <summary>
        /// Do whatever refreshing is necessary when the filesystem pertaining to this item has changed.
        /// </summary>
        /// <returns></returns>
        public virtual Task ChangedExternally()
        {
            return Task.Run(() => RefreshMetadata());
        }
    }
}
