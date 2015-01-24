using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Entities
{
    public class Game : BaseItem, IHasSoundtracks, IHasTrailers, IHasThemeMedia, IHasTags, IHasScreenshots, ISupportsPlaceHolders, IHasPreferredMetadataLanguage, IHasLookupInfo<GameInfo>
    {
        public List<Guid> SoundtrackIds { get; set; }

        public List<Guid> ThemeSongIds { get; set; }
        public List<Guid> ThemeVideoIds { get; set; }

        public string PreferredMetadataLanguage { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata country code.
        /// </summary>
        /// <value>The preferred metadata country code.</value>
        public string PreferredMetadataCountryCode { get; set; }

        public Game()
        {
            MultiPartGameFiles = new List<string>();
            SoundtrackIds = new List<Guid>();
            RemoteTrailers = new List<MediaUrl>();
            LocalTrailerIds = new List<Guid>();
            RemoteTrailerIds = new List<Guid>();
            ThemeSongIds = new List<Guid>();
            ThemeVideoIds = new List<Guid>();
            Tags = new List<string>();
        }

        public List<Guid> LocalTrailerIds { get; set; }
        public List<Guid> RemoteTrailerIds { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the remote trailers.
        /// </summary>
        /// <value>The remote trailers.</value>
        public List<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
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
        public List<string> MultiPartGameFiles { get; set; }

        protected override string CreateUserDataKey()
        {
            var id = this.GetProviderId(MetadataProviders.Gamesdb);

            if (!string.IsNullOrEmpty(id))
            {
                return "Game-Gamesdb-" + id;
            }
            return base.CreateUserDataKey();
        }

        public override IEnumerable<string> GetDeletePaths()
        {
            if (!IsInMixedFolder)
            {
                return new[] { System.IO.Path.GetDirectoryName(Path) };
            }

            return base.GetDeletePaths();
        }

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            return config.BlockUnratedItems.Contains(UnratedItem.Game);
        }

        public GameInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<GameInfo>();

            id.GameSystem = GameSystem;

            return id;
        }

        /// <summary>
        /// Gets the trailer ids.
        /// </summary>
        /// <returns>List&lt;Guid&gt;.</returns>
        public List<Guid> GetTrailerIds()
        {
            var list = LocalTrailerIds.ToList();
            list.AddRange(RemoteTrailerIds);
            return list;
        }
    }
}
