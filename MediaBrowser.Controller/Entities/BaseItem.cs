using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class BaseItem
    /// </summary>
    public abstract class BaseItem : IHasProviderIds, ILibraryItem, IHasImages, IHasUserData, IHasMetadata, IHasLookupInfo<ItemLookupInfo>
    {
        protected BaseItem()
        {
            Genres = new List<string>();
            Studios = new List<string>();
            People = new List<PersonInfo>();
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            LockedFields = new List<MetadataFields>();
            ImageInfos = new List<ItemImageInfo>();
        }

        /// <summary>
        /// The supported image extensions
        /// </summary>
        public static readonly string[] SupportedImageExtensions = new[] { ".png", ".jpg", ".jpeg", ".tbn" };

        /// <summary>
        /// The trailer folder name
        /// </summary>
        public const string TrailerFolderName = "trailers";
        public const string ThemeSongsFolderName = "theme-music";
        public const string ThemeSongFilename = "theme";
        public const string ThemeVideosFolderName = "backdrops";
        public const string XbmcTrailerFileSuffix = "-trailer";

        public List<ItemImageInfo> ImageInfos { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is in mixed folder.
        /// </summary>
        /// <value><c>true</c> if this instance is in mixed folder; otherwise, <c>false</c>.</value>
        public bool IsInMixedFolder { get; set; }

        private string _name;
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;

                // lazy load this again
                _sortName = null;
            }
        }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public Guid Id { get; set; }

        /// <summary>
        /// Return the id that should be used to key display prefs for this item.
        /// Default is based on the type for everything except actual generic folders.
        /// </summary>
        /// <value>The display prefs id.</value>
        [IgnoreDataMember]
        public virtual Guid DisplayPreferencesId
        {
            get
            {
                var thisType = GetType();
                return thisType == typeof(Folder) ? Id : thisType.FullName.GetMD5();
            }
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public virtual string Path { get; set; }

        [IgnoreDataMember]
        protected internal bool IsOffline { get; set; }

        /// <summary>
        /// Returns the folder containing the item.
        /// If the item is a folder, it returns the folder itself
        /// </summary>
        [IgnoreDataMember]
        public virtual string ContainingFolderPath
        {
            get
            {
                if (IsFolder)
                {
                    return Path;
                }

                return System.IO.Path.GetDirectoryName(Path);
            }
        }

        [IgnoreDataMember]
        public virtual bool IsOwnedItem
        {
            get
            {
                // Local trailer, special feature, theme video, etc.
                // An item that belongs to another item but is not part of the Parent-Child tree
                return !IsFolder && Parent == null;
            }
        }

        /// <summary>
        /// Gets or sets the type of the location.
        /// </summary>
        /// <value>The type of the location.</value>
        [IgnoreDataMember]
        public virtual LocationType LocationType
        {
            get
            {
                if (IsOffline)
                {
                    return LocationType.Offline;
                }

                if (string.IsNullOrEmpty(Path))
                {
                    return LocationType.Virtual;
                }

                return System.IO.Path.IsPathRooted(Path) ? LocationType.FileSystem : LocationType.Remote;
            }
        }

        public virtual bool SupportsLocalMetadata
        {
            get
            {
                var locationType = LocationType;

                return locationType != LocationType.Remote && locationType != LocationType.Virtual;
            }
        }

        /// <summary>
        /// This is just a helper for convenience
        /// </summary>
        /// <value>The primary image path.</value>
        [IgnoreDataMember]
        public string PrimaryImagePath
        {
            get { return this.GetImagePath(ImageType.Primary); }
        }

        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        /// <value>The date created.</value>
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the date modified.
        /// </summary>
        /// <value>The date modified.</value>
        public DateTime DateModified { get; set; }

        public DateTime DateLastSaved { get; set; }

        /// <summary>
        /// The logger
        /// </summary>
        public static ILogger Logger { get; set; }
        public static ILibraryManager LibraryManager { get; set; }
        public static IServerConfigurationManager ConfigurationManager { get; set; }
        public static IProviderManager ProviderManager { get; set; }
        public static ILocalizationManager LocalizationManager { get; set; }
        public static IItemRepository ItemRepository { get; set; }
        public static IFileSystem FileSystem { get; set; }
        public static IUserDataManager UserDataManager { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Returns true if this item should not attempt to fetch metadata
        /// </summary>
        /// <value><c>true</c> if [dont fetch meta]; otherwise, <c>false</c>.</value>
        [Obsolete("Please use IsLocked instead of DontFetchMeta")]
        public bool DontFetchMeta { get; set; }

        [IgnoreDataMember]
        public bool IsLocked
        {
            get
            {
                return DontFetchMeta;
            }
        }

        /// <summary>
        /// Gets or sets the locked fields.
        /// </summary>
        /// <value>The locked fields.</value>
        public List<MetadataFields> LockedFields { get; set; }

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        [IgnoreDataMember]
        public virtual string MediaType
        {
            get
            {
                return null;
            }
        }

        [IgnoreDataMember]
        public virtual IEnumerable<string> PhysicalLocations
        {
            get
            {
                var locationType = LocationType;

                if (locationType == LocationType.Remote || locationType == LocationType.Virtual)
                {
                    return new string[] { };
                }

                return new[] { Path };
            }
        }

        private string _forcedSortName;
        /// <summary>
        /// Gets or sets the name of the forced sort.
        /// </summary>
        /// <value>The name of the forced sort.</value>
        public string ForcedSortName
        {
            get { return _forcedSortName; }
            set { _forcedSortName = value; _sortName = null; }
        }

        private string _sortName;
        /// <summary>
        /// Gets the name of the sort.
        /// </summary>
        /// <value>The name of the sort.</value>
        [IgnoreDataMember]
        public string SortName
        {
            get
            {
                if (!string.IsNullOrEmpty(ForcedSortName))
                {
                    return ForcedSortName;
                }

                return _sortName ?? (_sortName = CreateSortName());
            }
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected virtual string CreateSortName()
        {
            if (Name == null) return null; //some items may not have name filled in properly

            var sortable = Name.Trim().ToLower();
            sortable = ConfigurationManager.Configuration.SortRemoveCharacters.Aggregate(sortable, (current, search) => current.Replace(search.ToLower(), string.Empty));

            sortable = ConfigurationManager.Configuration.SortReplaceCharacters.Aggregate(sortable, (current, search) => current.Replace(search.ToLower(), " "));

            foreach (var search in ConfigurationManager.Configuration.SortRemoveWords)
            {
                var searchLower = search.ToLower();
                // Remove from beginning if a space follows
                if (sortable.StartsWith(searchLower + " "))
                {
                    sortable = sortable.Remove(0, searchLower.Length + 1);
                }
                // Remove from middle if surrounded by spaces
                sortable = sortable.Replace(" " + searchLower + " ", " ");

                // Remove from end if followed by a space
                if (sortable.EndsWith(" " + searchLower))
                {
                    sortable = sortable.Remove(sortable.Length - (searchLower.Length + 1));
                }
            }
            return sortable;
        }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        [IgnoreDataMember]
        public Folder Parent { get; set; }

        [IgnoreDataMember]
        public IEnumerable<Folder> Parents
        {
            get
            {
                var parent = Parent;

                while (parent != null)
                {
                    yield return parent;

                    parent = parent.Parent;
                }
            }
        }

        /// <summary>
        /// When the item first debuted. For movies this could be premiere date, episodes would be first aired
        /// </summary>
        /// <value>The premiere date.</value>
        public DateTime? PremiereDate { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        /// <value>The end date.</value>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the display type of the media.
        /// </summary>
        /// <value>The display type of the media.</value>
        public string DisplayMediaType { get; set; }

        /// <summary>
        /// Gets or sets the official rating.
        /// </summary>
        /// <value>The official rating.</value>
        public string OfficialRating { get; set; }

        /// <summary>
        /// Gets or sets the official rating description.
        /// </summary>
        /// <value>The official rating description.</value>
        public string OfficialRatingDescription { get; set; }

        /// <summary>
        /// Gets or sets the custom rating.
        /// </summary>
        /// <value>The custom rating.</value>
        public string CustomRating { get; set; }

        /// <summary>
        /// Gets or sets the overview.
        /// </summary>
        /// <value>The overview.</value>
        public string Overview { get; set; }

        /// <summary>
        /// Gets or sets the people.
        /// </summary>
        /// <value>The people.</value>
        public List<PersonInfo> People { get; set; }

        /// <summary>
        /// Gets or sets the studios.
        /// </summary>
        /// <value>The studios.</value>
        public List<string> Studios { get; set; }

        /// <summary>
        /// Gets or sets the genres.
        /// </summary>
        /// <value>The genres.</value>
        public List<string> Genres { get; set; }

        /// <summary>
        /// Gets or sets the home page URL.
        /// </summary>
        /// <value>The home page URL.</value>
        public string HomePageUrl { get; set; }

        /// <summary>
        /// Gets or sets the community rating.
        /// </summary>
        /// <value>The community rating.</value>
        public float? CommunityRating { get; set; }

        /// <summary>
        /// Gets or sets the community rating vote count.
        /// </summary>
        /// <value>The community rating vote count.</value>
        public int? VoteCount { get; set; }

        /// <summary>
        /// Gets or sets the run time ticks.
        /// </summary>
        /// <value>The run time ticks.</value>
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the production year.
        /// </summary>
        /// <value>The production year.</value>
        public int? ProductionYear { get; set; }

        /// <summary>
        /// If the item is part of a series, this is it's number in the series.
        /// This could be episode number, album track number, etc.
        /// </summary>
        /// <value>The index number.</value>
        public int? IndexNumber { get; set; }

        /// <summary>
        /// For an episode this could be the season number, or for a song this could be the disc number.
        /// </summary>
        /// <value>The parent index number.</value>
        public int? ParentIndexNumber { get; set; }

        [IgnoreDataMember]
        public virtual string OfficialRatingForComparison
        {
            get { return OfficialRating; }
        }

        [IgnoreDataMember]
        public string CustomRatingForComparison
        {
            get
            {
                if (!string.IsNullOrEmpty(CustomRating))
                {
                    return CustomRating;
                }

                var parent = Parent;
                if (parent != null)
                {
                    return parent.CustomRatingForComparison;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the play access.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>PlayAccess.</returns>
        public PlayAccess GetPlayAccess(User user)
        {
            if (!user.Configuration.EnableMediaPlayback)
            {
                return PlayAccess.None;
            }

            return PlayAccess.Full;
        }

        /// <summary>
        /// Loads local trailers from the file system
        /// </summary>
        /// <returns>List{Video}.</returns>
        private IEnumerable<Trailer> LoadLocalTrailers(List<FileSystemInfo> fileSystemChildren, IDirectoryService directoryService)
        {
            var files = fileSystemChildren.OfType<DirectoryInfo>()
                .Where(i => string.Equals(i.Name, TrailerFolderName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => i.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                .ToList();

            // Support plex/xbmc convention
            files.AddRange(fileSystemChildren.OfType<FileInfo>()
                .Where(i => System.IO.Path.GetFileNameWithoutExtension(i.Name).EndsWith(XbmcTrailerFileSuffix, StringComparison.OrdinalIgnoreCase) && !string.Equals(Path, i.FullName, StringComparison.OrdinalIgnoreCase))
                );

            return LibraryManager.ResolvePaths<Trailer>(files, directoryService, null).Select(video =>
            {
                // Try to retrieve it from the db. If we don't find it, use the resolved version
                var dbItem = LibraryManager.GetItemById(video.Id) as Trailer;

                if (dbItem != null)
                {
                    video = dbItem;
                }

                return video;

                // Sort them so that the list can be easily compared for changes
            }).OrderBy(i => i.Path).ToList();
        }

        /// <summary>
        /// Loads the theme songs.
        /// </summary>
        /// <returns>List{Audio.Audio}.</returns>
        private IEnumerable<Audio.Audio> LoadThemeSongs(List<FileSystemInfo> fileSystemChildren, IDirectoryService directoryService)
        {
            var files = fileSystemChildren.OfType<DirectoryInfo>()
                .Where(i => string.Equals(i.Name, ThemeSongsFolderName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => i.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                .ToList();

            // Support plex/xbmc convention
            files.AddRange(fileSystemChildren.OfType<FileInfo>()
                .Where(i => string.Equals(System.IO.Path.GetFileNameWithoutExtension(i.Name), ThemeSongFilename, StringComparison.OrdinalIgnoreCase))
                );

            return LibraryManager.ResolvePaths<Audio.Audio>(files, directoryService, null).Select(audio =>
            {
                // Try to retrieve it from the db. If we don't find it, use the resolved version
                var dbItem = LibraryManager.GetItemById(audio.Id) as Audio.Audio;

                if (dbItem != null)
                {
                    audio = dbItem;
                }

                return audio;

                // Sort them so that the list can be easily compared for changes
            }).OrderBy(i => i.Path).ToList();
        }

        /// <summary>
        /// Loads the video backdrops.
        /// </summary>
        /// <returns>List{Video}.</returns>
        private IEnumerable<Video> LoadThemeVideos(IEnumerable<FileSystemInfo> fileSystemChildren, IDirectoryService directoryService)
        {
            var files = fileSystemChildren.OfType<DirectoryInfo>()
                .Where(i => string.Equals(i.Name, ThemeVideosFolderName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => i.EnumerateFiles("*", SearchOption.TopDirectoryOnly));

            return LibraryManager.ResolvePaths<Video>(files, directoryService, null).Select(item =>
            {
                // Try to retrieve it from the db. If we don't find it, use the resolved version
                var dbItem = LibraryManager.GetItemById(item.Id) as Video;

                if (dbItem != null)
                {
                    item = dbItem;
                }

                return item;

                // Sort them so that the list can be easily compared for changes
            }).OrderBy(i => i.Path).ToList();
        }

        public Task RefreshMetadata(CancellationToken cancellationToken)
        {
            return RefreshMetadata(new MetadataRefreshOptions(), cancellationToken);
        }

        /// <summary>
        /// Overrides the base implementation to refresh metadata for local trailers
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>true if a provider reports we changed</returns>
        public async Task RefreshMetadata(MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var locationType = LocationType;

            var requiresSave = false;

            if (IsFolder || Parent != null)
            {
                options.DirectoryService = options.DirectoryService ?? new DirectoryService(Logger);

                try
                {
                    var files = locationType != LocationType.Remote && locationType != LocationType.Virtual ?
                        GetFileSystemChildren(options.DirectoryService).ToList() :
                        new List<FileSystemInfo>();

                    var ownedItemsChanged = await RefreshedOwnedItems(options, files, cancellationToken).ConfigureAwait(false);

                    if (ownedItemsChanged)
                    {
                        requiresSave = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error refreshing owned items for {0}", ex, Path ?? Name);
                }
            }

            var dateLastSaved = DateLastSaved;

            await ProviderManager.RefreshMetadata(this, options, cancellationToken).ConfigureAwait(false);

            // If it wasn't saved by the provider process, save now
            if (requiresSave && dateLastSaved == DateLastSaved)
            {
                await UpdateToRepository(ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Refreshes owned items such as trailers, theme videos, special features, etc.
        /// Returns true or false indicating if changes were found.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="fileSystemChildren"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task<bool> RefreshedOwnedItems(MetadataRefreshOptions options, List<FileSystemInfo> fileSystemChildren, CancellationToken cancellationToken)
        {
            var themeSongsChanged = false;

            var themeVideosChanged = false;

            var localTrailersChanged = false;

            if (LocationType == LocationType.FileSystem && Parent != null)
            {
                var hasThemeMedia = this as IHasThemeMedia;
                if (hasThemeMedia != null)
                {
                    if (!IsInMixedFolder)
                    {
                        themeSongsChanged = await RefreshThemeSongs(hasThemeMedia, options, fileSystemChildren, cancellationToken).ConfigureAwait(false);

                        themeVideosChanged = await RefreshThemeVideos(hasThemeMedia, options, fileSystemChildren, cancellationToken).ConfigureAwait(false);
                    }
                }

                var hasTrailers = this as IHasTrailers;
                if (hasTrailers != null)
                {
                    localTrailersChanged = await RefreshLocalTrailers(hasTrailers, options, fileSystemChildren, cancellationToken).ConfigureAwait(false);
                }
            }

            return themeSongsChanged || themeVideosChanged || localTrailersChanged;
        }

        protected virtual IEnumerable<FileSystemInfo> GetFileSystemChildren(IDirectoryService directoryService)
        {
            var path = ContainingFolderPath;

            return directoryService.GetFileSystemEntries(path);
        }

        private async Task<bool> RefreshLocalTrailers(IHasTrailers item, MetadataRefreshOptions options, List<FileSystemInfo> fileSystemChildren, CancellationToken cancellationToken)
        {
            var newItems = LoadLocalTrailers(fileSystemChildren, options.DirectoryService).ToList();
            var newItemIds = newItems.Select(i => i.Id).ToList();

            var itemsChanged = !item.LocalTrailerIds.SequenceEqual(newItemIds);

            var tasks = newItems.Select(i => i.RefreshMetadata(options, cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            item.LocalTrailerIds = newItemIds;

            return itemsChanged;
        }

        private async Task<bool> RefreshThemeVideos(IHasThemeMedia item, MetadataRefreshOptions options, IEnumerable<FileSystemInfo> fileSystemChildren, CancellationToken cancellationToken)
        {
            var newThemeVideos = LoadThemeVideos(fileSystemChildren, options.DirectoryService).ToList();

            var newThemeVideoIds = newThemeVideos.Select(i => i.Id).ToList();

            var themeVideosChanged = !item.ThemeVideoIds.SequenceEqual(newThemeVideoIds);

            var tasks = newThemeVideos.Select(i => i.RefreshMetadata(options, cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            item.ThemeVideoIds = newThemeVideoIds;

            return themeVideosChanged;
        }

        /// <summary>
        /// Refreshes the theme songs.
        /// </summary>
        private async Task<bool> RefreshThemeSongs(IHasThemeMedia item, MetadataRefreshOptions options, List<FileSystemInfo> fileSystemChildren, CancellationToken cancellationToken)
        {
            var newThemeSongs = LoadThemeSongs(fileSystemChildren, options.DirectoryService).ToList();
            var newThemeSongIds = newThemeSongs.Select(i => i.Id).ToList();

            var themeSongsChanged = !item.ThemeSongIds.SequenceEqual(newThemeSongIds);

            var tasks = newThemeSongs.Select(i => i.RefreshMetadata(options, cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            item.ThemeSongIds = newThemeSongIds;

            return themeSongsChanged;
        }

        /// <summary>
        /// Gets or sets the provider ids.
        /// </summary>
        /// <value>The provider ids.</value>
        public Dictionary<string, string> ProviderIds { get; set; }

        /// <summary>
        /// Override this to false if class should be ignored for indexing purposes
        /// </summary>
        /// <value><c>true</c> if [include in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public virtual bool IncludeInIndex
        {
            get { return true; }
        }

        /// <summary>
        /// Override this to true if class should be grouped under a container in indicies
        /// The container class should be defined via IndexContainer
        /// </summary>
        /// <value><c>true</c> if [group in index]; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public virtual bool GroupInIndex
        {
            get { return false; }
        }

        /// <summary>
        /// Override this to return the folder that should be used to construct a container
        /// for this item in an index.  GroupInIndex should be true as well.
        /// </summary>
        /// <value>The index container.</value>
        [IgnoreDataMember]
        public virtual Folder IndexContainer
        {
            get { return null; }
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public virtual string GetUserDataKey()
        {
            return Id.ToString();
        }

        /// <summary>
        /// Gets the preferred metadata language.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetPreferredMetadataLanguage()
        {
            string lang = null;

            var hasLang = this as IHasPreferredMetadataLanguage;

            if (hasLang != null)
            {
                lang = hasLang.PreferredMetadataLanguage;
            }

            if (string.IsNullOrEmpty(lang))
            {
                lang = Parents.OfType<IHasPreferredMetadataLanguage>()
                    .Select(i => i.PreferredMetadataLanguage)
                    .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            }

            if (string.IsNullOrEmpty(lang))
            {
                lang = ConfigurationManager.Configuration.PreferredMetadataLanguage;
            }

            return lang;
        }

        /// <summary>
        /// Gets the preferred metadata language.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetPreferredMetadataCountryCode()
        {
            string lang = null;

            var hasLang = this as IHasPreferredMetadataLanguage;

            if (hasLang != null)
            {
                lang = hasLang.PreferredMetadataCountryCode;
            }

            if (string.IsNullOrEmpty(lang))
            {
                lang = Parents.OfType<IHasPreferredMetadataLanguage>()
                    .Select(i => i.PreferredMetadataCountryCode)
                    .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            }

            if (string.IsNullOrEmpty(lang))
            {
                lang = ConfigurationManager.Configuration.MetadataCountryCode;
            }

            return lang;
        }

        public virtual bool IsSaveLocalMetadataEnabled()
        {
            return ConfigurationManager.Configuration.SaveLocalMeta;
        }

        /// <summary>
        /// Determines if a given user has access to this item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if [is parental allowed] [the specified user]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public bool IsParentalAllowed(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            var maxAllowedRating = user.Configuration.MaxParentalRating;

            if (maxAllowedRating == null)
            {
                return true;
            }

            var rating = CustomRatingForComparison;

            if (string.IsNullOrEmpty(rating))
            {
                rating = OfficialRatingForComparison;
            }

            if (string.IsNullOrEmpty(rating))
            {
                return !GetBlockUnratedValue(user.Configuration);
            }

            var value = LocalizationManager.GetRatingLevel(rating);

            // Could not determine the integer value
            if (!value.HasValue)
            {
                return true;
            }

            return value.Value <= maxAllowedRating.Value;
        }

        /// <summary>
        /// Gets the block unrated value.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected virtual bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockNotRated;
        }

        /// <summary>
        /// Determines if this folder should be visible to a given user.
        /// Default is just parental allowed. Can be overridden for more functionality.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the specified user is visible; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public virtual bool IsVisible(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            return IsParentalAllowed(user);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is folder.
        /// </summary>
        /// <value><c>true</c> if this instance is folder; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public virtual bool IsFolder
        {
            get
            {
                return false;
            }
        }

        public virtual string GetClientTypeName()
        {
            return GetType().Name;
        }

        /// <summary>
        /// Determines if the item is considered new based on user settings
        /// </summary>
        /// <returns><c>true</c> if [is recently added] [the specified user]; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public bool IsRecentlyAdded()
        {
            return (DateTime.UtcNow - DateCreated).TotalDays < ConfigurationManager.Configuration.RecentItemDays;
        }

        /// <summary>
        /// Adds a person to the item
        /// </summary>
        /// <param name="person">The person.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void AddPerson(PersonInfo person)
        {
            if (person == null)
            {
                throw new ArgumentNullException("person");
            }

            if (string.IsNullOrWhiteSpace(person.Name))
            {
                throw new ArgumentNullException();
            }

            // Normalize
            if (string.Equals(person.Role, PersonType.GuestStar, StringComparison.OrdinalIgnoreCase))
            {
                person.Type = PersonType.GuestStar;
            }
            else if (string.Equals(person.Role, PersonType.Director, StringComparison.OrdinalIgnoreCase))
            {
                person.Type = PersonType.Director;
            }
            else if (string.Equals(person.Role, PersonType.Producer, StringComparison.OrdinalIgnoreCase))
            {
                person.Type = PersonType.Producer;
            }
            else if (string.Equals(person.Role, PersonType.Writer, StringComparison.OrdinalIgnoreCase))
            {
                person.Type = PersonType.Writer;
            }

            // If the type is GuestStar and there's already an Actor entry, then update it to avoid dupes
            if (string.Equals(person.Type, PersonType.GuestStar, StringComparison.OrdinalIgnoreCase))
            {
                var existing = People.FirstOrDefault(p => p.Name.Equals(person.Name, StringComparison.OrdinalIgnoreCase) && p.Type.Equals(PersonType.Actor, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.Type = PersonType.GuestStar;
                    existing.SortOrder = person.SortOrder ?? existing.SortOrder;
                    return;
                }
            }

            if (string.Equals(person.Type, PersonType.Actor, StringComparison.OrdinalIgnoreCase))
            {
                // If the actor already exists without a role and we have one, fill it in
                var existing = People.FirstOrDefault(p => p.Name.Equals(person.Name, StringComparison.OrdinalIgnoreCase) && (p.Type.Equals(PersonType.Actor, StringComparison.OrdinalIgnoreCase) || p.Type.Equals(PersonType.GuestStar, StringComparison.OrdinalIgnoreCase)));
                if (existing == null)
                {
                    // Wasn't there - add it
                    People.Add(person);
                }
                else
                {
                    // Was there, if no role and we have one - fill it in
                    if (string.IsNullOrWhiteSpace(existing.Role) && !string.IsNullOrWhiteSpace(person.Role))
                    {
                        existing.Role = person.Role;
                    }

                    existing.SortOrder = person.SortOrder ?? existing.SortOrder;
                }
            }
            else
            {
                var existing = People.FirstOrDefault(p =>
                            string.Equals(p.Name, person.Name, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(p.Type, person.Type, StringComparison.OrdinalIgnoreCase));

                // Check for dupes based on the combination of Name and Type
                if (existing == null)
                {
                    People.Add(person);
                }
                else
                {
                    existing.SortOrder = person.SortOrder ?? existing.SortOrder;
                }
            }
        }

        /// <summary>
        /// Adds a studio to the item
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void AddStudio(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            if (!Studios.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                Studios.Add(name);
            }
        }

        /// <summary>
        /// Adds a genre to the item
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ArgumentNullException"></exception>
        public void AddGenre(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            if (!Genres.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                Genres.Add(name);
            }
        }

        /// <summary>
        /// Marks the played.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="datePlayed">The date played.</param>
        /// <param name="userManager">The user manager.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public virtual async Task MarkPlayed(User user, DateTime? datePlayed, IUserDataManager userManager)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }

            var key = GetUserDataKey();

            var data = userManager.GetUserData(user.Id, key);

            if (datePlayed.HasValue)
            {
                // Incremenet
                data.PlayCount++;
            }

            // Ensure it's at least one
            data.PlayCount = Math.Max(data.PlayCount, 1);

            data.LastPlayedDate = datePlayed ?? data.LastPlayedDate;
            data.Played = true;

            await userManager.SaveUserData(user.Id, this, data, UserDataSaveReason.TogglePlayed, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Marks the unplayed.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="userManager">The user manager.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public virtual async Task MarkUnplayed(User user, IUserDataManager userManager)
        {
            if (user == null)
            {
                throw new ArgumentNullException();
            }

            var key = GetUserDataKey();

            var data = userManager.GetUserData(user.Id, key);

            //I think it is okay to do this here.
            // if this is only called when a user is manually forcing something to un-played
            // then it probably is what we want to do...
            data.PlayCount = 0;
            data.PlaybackPositionTicks = 0;
            data.LastPlayedDate = null;
            data.Played = false;

            await userManager.SaveUserData(user.Id, this, data, UserDataSaveReason.TogglePlayed, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Do whatever refreshing is necessary when the filesystem pertaining to this item has changed.
        /// </summary>
        /// <returns>Task.</returns>
        public virtual Task ChangedExternally()
        {
            return RefreshMetadata(CancellationToken.None);
        }

        /// <summary>
        /// Finds a parent of a given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T FindParent<T>()
            where T : Folder
        {
            var parent = Parent;

            while (parent != null)
            {
                var result = parent as T;
                if (result != null)
                {
                    return result;
                }

                parent = parent.Parent;
            }

            return null;
        }

        /// <summary>
        /// Gets an image
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns><c>true</c> if the specified type has image; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentException">Backdrops should be accessed using Item.Backdrops</exception>
        public bool HasImage(ImageType type, int imageIndex)
        {
            return GetImageInfo(type, imageIndex) != null;
        }

        public void SetImagePath(ImageType type, int index, FileInfo file)
        {
            if (type == ImageType.Chapter)
            {
                throw new ArgumentException("Cannot set chapter images using SetImagePath");
            }

            var image = GetImageInfo(type, index);

            if (image == null)
            {
                ImageInfos.Add(new ItemImageInfo
                {
                    Path = file.FullName,
                    Type = type,
                    DateModified = FileSystem.GetLastWriteTimeUtc(file)
                });
            }
            else
            {
                image.Path = file.FullName;
                image.DateModified = FileSystem.GetLastWriteTimeUtc(file);
            }
        }

        /// <summary>
        /// Deletes the image.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="index">The index.</param>
        /// <returns>Task.</returns>
        public Task DeleteImage(ImageType type, int index)
        {
            var info = GetImageInfo(type, index);

            if (info == null)
            {
                // Nothing to do
                return Task.FromResult(true);
            }

            // Remove it from the item
            ImageInfos.Remove(info);

            // Delete the source file
            var currentFile = new FileInfo(info.Path);

            // Deletion will fail if the file is hidden so remove the attribute first
            if (currentFile.Exists)
            {
                if ((currentFile.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    currentFile.Attributes &= ~FileAttributes.Hidden;
                }

                currentFile.Delete();
            }

            return UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None);
        }

        public virtual Task UpdateToRepository(ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            return LibraryManager.UpdateItem(this, updateReason, cancellationToken);
        }

        /// <summary>
        /// Validates that images within the item are still on the file system
        /// </summary>
        public bool ValidateImages(IDirectoryService directoryService)
        {
            var allDirectories = ImageInfos.Select(i => System.IO.Path.GetDirectoryName(i.Path)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var allFiles = allDirectories.SelectMany(directoryService.GetFiles).Select(i => i.FullName).ToList();

            var deletedImages = ImageInfos
                .Where(image => !allFiles.Contains(image.Path, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (deletedImages.Count > 0)
            {
                ImageInfos = ImageInfos.Except(deletedImages).ToList();
            }

            return deletedImages.Count > 0;
        }

        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// </exception>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public string GetImagePath(ImageType imageType, int imageIndex)
        {
            var info = GetImageInfo(imageType, imageIndex);

            return info == null ? null : info.Path;
        }

        /// <summary>
        /// Gets the image information.
        /// </summary>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>ItemImageInfo.</returns>
        public ItemImageInfo GetImageInfo(ImageType imageType, int imageIndex)
        {
            if (imageType == ImageType.Chapter)
            {
                var chapter = ItemRepository.GetChapter(Id, imageIndex);

                if (chapter == null)
                {
                    return null;
                }

                var path = chapter.ImagePath;

                if (string.IsNullOrWhiteSpace(path))
                {
                    return null;
                }

                return new ItemImageInfo
                {
                    Path = path,
                    DateModified = FileSystem.GetLastWriteTimeUtc(path),
                    Type = imageType
                };
            }

            return GetImages(imageType)
                .ElementAtOrDefault(imageIndex);
        }

        public IEnumerable<ItemImageInfo> GetImages(ImageType imageType)
        {
            if (imageType == ImageType.Chapter)
            {
                throw new ArgumentException("No image info for chapter images");
            }

            return ImageInfos.Where(i => i.Type == imageType);
        }

        /// <summary>
        /// Adds the images.
        /// </summary>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="images">The images.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentException">Cannot call AddImages with chapter images</exception>
        public bool AddImages(ImageType imageType, IEnumerable<FileInfo> images)
        {
            if (imageType == ImageType.Chapter)
            {
                throw new ArgumentException("Cannot call AddImages with chapter images");
            }

            var existingImagePaths = GetImages(imageType)
                .Select(i => i.Path)
                .ToList();

            var newImages = images
                .Where(i => !existingImagePaths.Contains(i.FullName, StringComparer.OrdinalIgnoreCase))
                .ToList();

            ImageInfos.AddRange(newImages.Select(i => new ItemImageInfo
            {
                Path = i.FullName,
                Type = imageType,
                DateModified = FileSystem.GetLastWriteTimeUtc(i)
            }));

            return newImages.Count > 0;
        }

        /// <summary>
        /// Gets the file system path to delete when the item is to be deleted
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetDeletePaths()
        {
            return new[] { Path };
        }

        public bool AllowsMultipleImages(ImageType type)
        {
            return type == ImageType.Backdrop || type == ImageType.Screenshot || type == ImageType.Chapter;
        }

        public Task SwapImages(ImageType type, int index1, int index2)
        {
            if (!AllowsMultipleImages(type))
            {
                throw new ArgumentException("The change index operation is only applicable to backdrops and screenshots");
            }

            var info1 = GetImageInfo(type, index1);
            var info2 = GetImageInfo(type, index2);

            if (info1 == null || info2 == null)
            {
                // Nothing to do
                return Task.FromResult(true);
            }

            var path1 = info1.Path;
            var path2 = info2.Path;

            FileSystem.SwapFiles(path1, path2);

            // Refresh these values
            info1.DateModified = FileSystem.GetLastWriteTimeUtc(info1.Path);
            info2.DateModified = FileSystem.GetLastWriteTimeUtc(info2.Path);

            return UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None);
        }

        public virtual bool IsPlayed(User user)
        {
            var userdata = UserDataManager.GetUserData(user.Id, GetUserDataKey());

            return userdata != null && userdata.Played;
        }

        public virtual bool IsUnplayed(User user)
        {
            var userdata = UserDataManager.GetUserData(user.Id, GetUserDataKey());

            return userdata == null || !userdata.Played;
        }

        ItemLookupInfo IHasLookupInfo<ItemLookupInfo>.GetLookupInfo()
        {
            return GetItemLookupInfo<ItemLookupInfo>();
        }

        protected T GetItemLookupInfo<T>()
            where T : ItemLookupInfo, new()
        {
            return new T
            {
                MetadataCountryCode = GetPreferredMetadataCountryCode(),
                MetadataLanguage = GetPreferredMetadataLanguage(),
                Name = Name,
                ProviderIds = ProviderIds,
                IndexNumber = IndexNumber,
                ParentIndexNumber = ParentIndexNumber
            };
        }

        /// <summary>
        /// This is called before any metadata refresh and returns true or false indicating if changes were made
        /// </summary>
        public virtual bool BeforeMetadataRefresh()
        {
            var hasChanges = false;

            if (string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Path))
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(Path);
                hasChanges = true;
            }

            return hasChanges;
        }
    }
}
