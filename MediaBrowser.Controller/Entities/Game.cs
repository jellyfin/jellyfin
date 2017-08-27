using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Controller.Entities
{
    public class Game : BaseItem, IHasTrailers, IHasScreenshots, ISupportsPlaceHolders, IHasLookupInfo<GameInfo>
    {
        public Game()
        {
            MultiPartGameFiles = EmptyStringArray;
            RemoteTrailers = EmptyMediaUrlArray;
            LocalTrailerIds = EmptyGuidArray;
            RemoteTrailerIds = EmptyGuidArray;
        }

        public Guid[] LocalTrailerIds { get; set; }
        public Guid[] RemoteTrailerIds { get; set; }

        public override bool CanDownload()
        {
            var locationType = LocationType;
            return locationType != LocationType.Remote &&
                   locationType != LocationType.Virtual;
        }

        [IgnoreDataMember]
        public override bool EnableRefreshOnDateModifiedChange
        {
            get { return true; }
        }

        [IgnoreDataMember]
        public override bool SupportsThemeMedia
        {
            get { return true; }
        }

        /// <summary>
        /// Gets or sets the remote trailers.
        /// </summary>
        /// <value>The remote trailers.</value>
        public MediaUrl[] RemoteTrailers { get; set; }

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        [IgnoreDataMember]
        public override string MediaType
        {
            get { return Model.Entities.MediaType.Game; }
        }

        /// <summary>
        /// Gets or sets the players supported.
        /// </summary>
        /// <value>The players supported.</value>
        public int? PlayersSupported { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is place holder.
        /// </summary>
        /// <value><c>true</c> if this instance is place holder; otherwise, <c>false</c>.</value>
        public bool IsPlaceHolder { get; set; }

        /// <summary>
        /// Gets or sets the game system.
        /// </summary>
        /// <value>The game system.</value>
        public string GameSystem { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is multi part.
        /// </summary>
        /// <value><c>true</c> if this instance is multi part; otherwise, <c>false</c>.</value>
        public bool IsMultiPart { get; set; }

        /// <summary>
        /// Holds the paths to the game files in the event this is a multipart game
        /// </summary>
        public string[] MultiPartGameFiles { get; set; }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();
            var id = this.GetProviderId(MetadataProviders.Gamesdb);

            if (!string.IsNullOrEmpty(id))
            {
                list.Insert(0, "Game-Gamesdb-" + id);
            }
            return list;
        }

        public override IEnumerable<FileSystemMetadata> GetDeletePaths()
        {
            if (!IsInMixedFolder)
            {
                return new[] {
                    new FileSystemMetadata
                    {
                        FullName = FileSystem.GetDirectoryName(Path),
                        IsDirectory = true
                    }
                };
            }

            return base.GetDeletePaths();
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Game;
        }

        public GameInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<GameInfo>();

            id.GameSystem = GameSystem;

            return id;
        }
    }
}
