using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicArtist
    /// </summary>
    public class MusicArtist : Folder, IItemByName, IHasMusicGenres, IHasDualAccess, IHasTags, IHasProductionLocations
    {
        [IgnoreDataMember]
        public List<ItemByNameCounts> UserItemCountList { get; set; }

        public bool IsAccessedByName { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public List<string> Tags { get; set; }

        public List<string> ProductionLocations { get; set; }
        
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
                    return new List<BaseItem>();
                }

                return base.ActualChildren;
            }
        }

        protected override Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool? recursive = null, bool forceRefreshMetadata = false)
        {
            if (IsAccessedByName)
            {
                // Should never get in here anyway
                return Task.FromResult(true);
            }

            return base.ValidateChildrenInternal(progress, cancellationToken, recursive, forceRefreshMetadata);
        }

        public override string GetClientTypeName()
        {
            if (IsAccessedByName)
            {
                //return "Artist";
            }

            return base.GetClientTypeName();
        }

        public MusicArtist()
        {
            UserItemCountList = new List<ItemByNameCounts>();
            Tags = new List<string>();
            ProductionLocations = new List<string>();
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
        private static string GetUserDataKey(MusicArtist item)
        {
            var id = item.GetProviderId(MetadataProviders.Musicbrainz);

            if (!string.IsNullOrEmpty(id))
            {
                return "Artist-Musicbrainz-" + id;
            }

            return "Artist-" + item.Name;
        }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedMusic;
        }
    }
}
