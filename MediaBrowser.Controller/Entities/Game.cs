using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public class Game : BaseItem, IHasSoundtracks, IHasTrailers, IHasThemeMedia, IHasTags, IHasLanguage, IHasScreenshots, IHasPreferredMetadataLanguage
    {
        public List<Guid> SoundtrackIds { get; set; }

        public List<Guid> ThemeSongIds { get; set; }
        public List<Guid> ThemeVideoIds { get; set; }

        public string PreferredMetadataLanguage { get; set; }

        public Game()
        {
            MultiPartGameFiles = new List<string>();
            SoundtrackIds = new List<Guid>();
            RemoteTrailers = new List<MediaUrl>();
            LocalTrailerIds = new List<Guid>();
            ThemeSongIds = new List<Guid>();
            ThemeVideoIds = new List<Guid>();
            Tags = new List<string>();
            ScreenshotImagePaths = new List<string>();
        }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        public string Language { get; set; }

        public List<Guid> LocalTrailerIds { get; set; }

        /// <summary>
        /// Gets or sets the screenshot image paths.
        /// </summary>
        /// <value>The screenshot image paths.</value>
        public List<string> ScreenshotImagePaths { get; set; }

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
        /// Gets or sets a value indicating whether this instance is installed on client.
        /// </summary>
        /// <value><c>true</c> if this instance is installed on client; otherwise, <c>false</c>.</value>
        public bool IsInstalledOnClient { get; set; }

        /// <summary>
        /// Gets or sets the game system.
        /// </summary>
        /// <value>The game system.</value>
        public string GameSystem { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public override string MetaLocation
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Path);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is multi part.
        /// </summary>
        /// <value><c>true</c> if this instance is multi part; otherwise, <c>false</c>.</value>
        public bool IsMultiPart { get; set; }

        /// <summary>
        /// Holds the paths to the game files in the event this is a multipart game
        /// </summary>
        public List<string> MultiPartGameFiles { get; set; }

        /// <summary>
        /// 
        /// </summary>
        protected override bool UseParentPathToCreateResolveArgs
        {
            get
            {
                return !IsInMixedFolder;
            }
        }

        public override string GetUserDataKey()
        {
            var id = this.GetProviderId(MetadataProviders.Gamesdb);

            if (!string.IsNullOrEmpty(id))
            {
                return "Game-Gamesdb-" + id;
            }
            return base.GetUserDataKey();
        }

        public override IEnumerable<string> GetDeletePaths()
        {
            if (!IsInMixedFolder)
            {
                return new[] { System.IO.Path.GetDirectoryName(Path) };
            }

            return base.GetDeletePaths();
        }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedGames;
        }
    }
}
