using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicArtist
    /// </summary>
    public class MusicArtist : Folder, IItemByName, IHasMusicGenres, IHasDualAccess
    {
        [IgnoreDataMember]
        public Dictionary<Guid, ItemByNameCounts> UserItemCounts { get; set; }

        public bool IsAccessedByName { get; set; }

        public override bool IsFolder
        {
            get
            {
                return !IsAccessedByName;
            }
        }

        protected override IEnumerable<BaseItem> ActualChildren
        {
            get
            {
                if (IsAccessedByName)
                {
                    throw new InvalidOperationException("Artists accessed by name do not have children.");
                }

                return base.ActualChildren;
            }
        }

        public override string GetClientTypeName()
        {
            if (IsAccessedByName)
            {
                //return "Artist";
            }

            return base.GetClientTypeName();
        }

        /// <summary>
        /// Gets or sets the last fm image URL.
        /// </summary>
        /// <value>The last fm image URL.</value>
        public string LastFmImageUrl { get; set; }
        public string LastFmImageSize { get; set; }

        public MusicArtist()
        {
            UserItemCounts = new Dictionary<Guid, ItemByNameCounts>();
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return GetUserDataKey(this);
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        public static string GetUserDataKey(BaseItem item)
        {
            var id = item.GetProviderId(MetadataProviders.Musicbrainz);

            if (!string.IsNullOrEmpty(id))
            {
                return "Artist-Musicbrainz-" + id;
            }

            return "Artist-" + item.Name;
        }
    }
}
