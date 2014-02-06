using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Localization;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
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
    public abstract class BaseItem : IHasProviderIds, ILibraryItem, IHasImages, IHasUserData, IHasMetadata
    {
        protected BaseItem()
        {
            Genres = new List<string>();
            Studios = new List<string>();
            People = new List<PersonInfo>();
            BackdropImagePaths = new List<string>();
            Images = new Dictionary<ImageType, string>();
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            LockedFields = new List<MetadataFields>();
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
        public bool IsOwnedItem
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

        /// <summary>
        /// This is just a helper for convenience
        /// </summary>
        /// <value>The primary image path.</value>
        [IgnoreDataMember]
        public string PrimaryImagePath
        {
            get { return this.GetImagePath(ImageType.Primary); }
            set { this.SetImagePath(ImageType.Primary, value); }
        }

        /// <summary>
        /// Gets or sets the images.
        /// </summary>
        /// <value>The images.</value>
        public Dictionary<ImageType, string> Images { get; set; }

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
        public bool DontFetchMeta { get; set; }

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

                if (locationType != LocationType.Remote && locationType != LocationType.Virtual)
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
        /// Gets or sets the name of the sort.
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
        /// Gets or sets the backdrop image paths.
        /// </summary>
        /// <value>The backdrop image paths.</value>
        public List<string> BackdropImagePaths { get; set; }

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
        /// Loads local trailers from the file system
        /// </summary>
        /// <returns>List{Video}.</returns>
        private IEnumerable<Trailer> LoadLocalTrailers(List<FileSystemInfo> fileSystemChildren)
        {
            return new List<Trailer>();
            //ItemResolveArgs resolveArgs;

            //try
            //{
            //    resolveArgs = ResolveArgs;

            //    if (!resolveArgs.IsDirectory)
            //    {
            //        return new List<Trailer>();
            //    }
            //}
            //catch (IOException ex)
            //{
            //    Logger.ErrorException("Error getting ResolveArgs for {0}", ex, Path);
            //    return new List<Trailer>();
            //}

            //var files = new List<FileSystemInfo>();

            //var folder = resolveArgs.GetFileSystemEntryByName(TrailerFolderName);

            //// Path doesn't exist. No biggie
            //if (folder != null)
            //{
            //    try
            //    {
            //        files.AddRange(new DirectoryInfo(folder.FullName).EnumerateFiles());
            //    }
            //    catch (IOException ex)
            //    {
            //        Logger.ErrorException("Error loading trailers for {0}", ex, Name);
            //    }
            //}

            //// Support xbmc trailers (-trailer suffix on video file names)
            //files.AddRange(resolveArgs.FileSystemChildren.Where(i =>
            //{
            //    try
            //    {
            //        if ((i.Attributes & FileAttributes.Directory) != FileAttributes.Directory)
            //        {
            //            if (System.IO.Path.GetFileNameWithoutExtension(i.Name).EndsWith(XbmcTrailerFileSuffix, StringComparison.OrdinalIgnoreCase) && !string.Equals(Path, i.FullName, StringComparison.OrdinalIgnoreCase))
            //            {
            //                return true;
            //            }
            //        }
            //    }
            //    catch (IOException ex)
            //    {
            //        Logger.ErrorException("Error accessing path {0}", ex, i.FullName);
            //    }

            //    return false;
            //}));

            //return LibraryManager.ResolvePaths<Trailer>(files, null).Select(video =>
            //{
            //    // Try to retrieve it from the db. If we don't find it, use the resolved version
            //    var dbItem = LibraryManager.GetItemById(video.Id) as Trailer;

            //    if (dbItem != null)
            //    {
            //        video = dbItem;
            //    }

            //    return video;

            //}).ToList();
        }

        /// <summary>
        /// Loads the theme songs.
        /// </summary>
        /// <returns>List{Audio.Audio}.</returns>
        private IEnumerable<Audio.Audio> LoadThemeSongs(List<FileSystemInfo> fileSystemChildren)
        {
            var files = fileSystemChildren.OfType<DirectoryInfo>()
                .Where(i => string.Equals(i.Name, ThemeSongsFolderName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => i.EnumerateFiles("*", SearchOption.TopDirectoryOnly))
                .ToList();

            // Support plex/xbmc convention
            files.AddRange(fileSystemChildren.OfType<FileInfo>()
                .Where(i => string.Equals(System.IO.Path.GetFileNameWithoutExtension(i.Name), ThemeSongFilename, StringComparison.OrdinalIgnoreCase))
                );

            return LibraryManager.ResolvePaths<Audio.Audio>(files, null).Select(audio =>
            {
                // Try to retrieve it from the db. If we don't find it, use the resolved version
                var dbItem = LibraryManager.GetItemById(audio.Id) as Audio.Audio;

                if (dbItem != null)
                {
                    audio = dbItem;
                }

                return audio;
            }).ToList();
        }

        /// <summary>
        /// Loads the video backdrops.
        /// </summary>
        /// <returns>List{Video}.</returns>
        private IEnumerable<Video> LoadThemeVideos(IEnumerable<FileSystemInfo> fileSystemChildren)
        {
            var files = fileSystemChildren.OfType<DirectoryInfo>()
                .Where(i => string.Equals(i.Name, ThemeVideosFolderName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => i.EnumerateFiles("*", SearchOption.TopDirectoryOnly));

            return LibraryManager.ResolvePaths<Video>(files, null).Select(item =>
            {
                // Try to retrieve it from the db. If we don't find it, use the resolved version
                var dbItem = LibraryManager.GetItemById(item.Id) as Video;

                if (dbItem != null)
                {
                    item = dbItem;
                }

                return item;
            }).ToList();
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

            if (IsFolder || Parent != null)
            {
                var files = locationType == LocationType.FileSystem || locationType == LocationType.Offline ?
                    GetFileSystemChildren().ToList() :
                    new List<FileSystemInfo>();

                await BeforeRefreshMetadata(options, files, cancellationToken).ConfigureAwait(false);
            }

            await ProviderManager.RefreshMetadata(this, options, cancellationToken).ConfigureAwait(false);
        }

        protected virtual async Task BeforeRefreshMetadata(MetadataRefreshOptions options, List<FileSystemInfo> fileSystemChildren, CancellationToken cancellationToken)
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
            
            if (themeSongsChanged || themeVideosChanged || localTrailersChanged)
            {
                options.ForceSave = true;
            }
        }

        protected virtual IEnumerable<FileSystemInfo> GetFileSystemChildren()
        {
            var path = ContainingFolderPath;

            return new DirectoryInfo(path).EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
        }

        private async Task<bool> RefreshLocalTrailers(IHasTrailers item, MetadataRefreshOptions options, List<FileSystemInfo> fileSystemChildren, CancellationToken cancellationToken)
        {
            var newItems = LoadLocalTrailers(fileSystemChildren).ToList();
            var newItemIds = newItems.Select(i => i.Id).ToList();

            var itemsChanged = !item.LocalTrailerIds.SequenceEqual(newItemIds);

            var tasks = newItems.Select(i => i.RefreshMetadata(options, cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);

            item.LocalTrailerIds = newItemIds;

            return itemsChanged;
        }

        private async Task<bool> RefreshThemeVideos(IHasThemeMedia item, MetadataRefreshOptions options, IEnumerable<FileSystemInfo> fileSystemChildren, CancellationToken cancellationToken)
        {
            var newThemeVideos = LoadThemeVideos(fileSystemChildren).ToList();
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
            var newThemeSongs = LoadThemeSongs(fileSystemChildren).ToList();
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
        /// Finds the particular item by searching through our parents and, if not found there, loading from repo
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentException"></exception>
        protected BaseItem FindParentItem(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException();
            }

            var parent = Parent;
            while (parent != null && !parent.IsRoot)
            {
                if (parent.Id == id) return parent;
                parent = parent.Parent;
            }

            return null;
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

        /// <summary>
        /// Determine if we have changed vs the passed in copy
        /// </summary>
        /// <param name="copy">The copy.</param>
        /// <returns><c>true</c> if the specified copy has changed; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public virtual bool HasChanged(BaseItem copy)
        {
            if (copy == null)
            {
                throw new ArgumentNullException();
            }
            if (IsInMixedFolder != copy.IsInMixedFolder)
            {
                Logger.Debug(Name + " changed due to different value for IsInMixedFolder.");
                return true;
            }

            var changed = copy.DateModified != DateModified;
            if (changed)
            {
                Logger.Debug(Name + " changed - original creation: " + DateCreated + " new creation: " + copy.DateCreated + " original modified: " + DateModified + " new modified: " + copy.DateModified);
            }
            return changed;
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
            if (type == ImageType.Backdrop)
            {
                return BackdropImagePaths.Count > imageIndex;
            }
            if (type == ImageType.Screenshot)
            {
                var hasScreenshots = this as IHasScreenshots;
                return hasScreenshots != null && hasScreenshots.ScreenshotImagePaths.Count > imageIndex;
            }

            return !string.IsNullOrEmpty(this.GetImagePath(type));
        }

        public void SetImagePath(ImageType type, int index, string path)
        {
            if (type == ImageType.Backdrop)
            {
                throw new ArgumentException("Backdrops should be accessed using Item.Backdrops");
            }
            if (type == ImageType.Screenshot)
            {
                throw new ArgumentException("Screenshots should be accessed using Item.Screenshots");
            }

            var typeKey = type;

            // If it's null remove the key from the dictionary
            if (string.IsNullOrEmpty(path))
            {
                if (Images.ContainsKey(typeKey))
                {
                    Images.Remove(typeKey);
                }
            }
            else
            {
                Images[typeKey] = path;
            }
        }

        /// <summary>
        /// Deletes the image.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="index">The index.</param>
        /// <returns>Task.</returns>
        public Task DeleteImage(ImageType type, int? index)
        {
            if (type == ImageType.Backdrop)
            {
                if (!index.HasValue)
                {
                    throw new ArgumentException("Please specify a backdrop image index to delete.");
                }

                var file = BackdropImagePaths[index.Value];

                BackdropImagePaths.Remove(file);

                // Delete the source file
                DeleteImagePath(file);
            }
            else if (type == ImageType.Screenshot)
            {
                if (!index.HasValue)
                {
                    throw new ArgumentException("Please specify a screenshot image index to delete.");
                }

                var hasScreenshots = (IHasScreenshots)this;
                var file = hasScreenshots.ScreenshotImagePaths[index.Value];

                hasScreenshots.ScreenshotImagePaths.Remove(file);

                // Delete the source file
                DeleteImagePath(file);
            }
            else
            {
                // Delete the source file
                DeleteImagePath(this.GetImagePath(type));

                // Remove it from the item
                this.SetImagePath(type, null);
            }

            // Refresh metadata
            // Need to disable slow providers or the image might get re-downloaded
            return RefreshMetadata(new MetadataRefreshOptions
            {
                ForceSave = true,
                ImageRefreshMode = ImageRefreshMode.ValidationOnly,
                MetadataRefreshMode = MetadataRefreshMode.None

            }, CancellationToken.None);
        }

        /// <summary>
        /// Deletes the image path.
        /// </summary>
        /// <param name="path">The path.</param>
        private void DeleteImagePath(string path)
        {
            var currentFile = new FileInfo(path);

            // This will fail if the file is hidden
            if (currentFile.Exists)
            {
                if ((currentFile.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    currentFile.Attributes &= ~FileAttributes.Hidden;
                }

                currentFile.Delete();
            }
        }

        /// <summary>
        /// Validates that images within the item are still on the file system
        /// </summary>
        public bool ValidateImages()
        {
            var changed = false;

            // Only validate paths from the same directory - need to copy to a list because we are going to potentially modify the collection below
            var deletedKeys = Images
                .Where(image => !File.Exists(image.Value))
                .Select(i => i.Key)
                .ToList();

            // Now remove them from the dictionary
            foreach (var key in deletedKeys)
            {
                Images.Remove(key);
                changed = true;
            }

            if (ValidateBackdrops())
            {
                changed = true;
            }
            if (ValidateScreenshots())
            {
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Validates that backdrops within the item are still on the file system
        /// </summary>
        private bool ValidateBackdrops()
        {
            var changed = false;

            // Only validate paths from the same directory - need to copy to a list because we are going to potentially modify the collection below
            var deletedImages = BackdropImagePaths
                .Where(path => !File.Exists(path))
                .ToList();

            // Now remove them from the dictionary
            foreach (var path in deletedImages)
            {
                BackdropImagePaths.Remove(path);

                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Validates the screenshots.
        /// </summary>
        private bool ValidateScreenshots()
        {
            var changed = false;

            var hasScreenshots = this as IHasScreenshots;

            if (hasScreenshots == null)
            {
                return changed;
            }

            // Only validate paths from the same directory - need to copy to a list because we are going to potentially modify the collection below
            var deletedImages = hasScreenshots.ScreenshotImagePaths
                .Where(path => !File.Exists(path))
                .ToList();

            // Now remove them from the dictionary
            foreach (var path in deletedImages)
            {
                hasScreenshots.ScreenshotImagePaths.Remove(path);
                changed = true;
            }

            return changed;
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
            if (imageType == ImageType.Backdrop)
            {
                return BackdropImagePaths.Count > imageIndex ? BackdropImagePaths[imageIndex] : null;
            }

            if (imageType == ImageType.Screenshot)
            {
                var hasScreenshots = (IHasScreenshots)this;
                return hasScreenshots.ScreenshotImagePaths.Count > imageIndex ? hasScreenshots.ScreenshotImagePaths[imageIndex] : null;
            }

            if (imageType == ImageType.Chapter)
            {
                return ItemRepository.GetChapter(Id, imageIndex).ImagePath;
            }

            string val;
            Images.TryGetValue(imageType, out val);
            return val;
        }

        /// <summary>
        /// Gets the image date modified.
        /// </summary>
        /// <param name="imagePath">The image path.</param>
        /// <returns>DateTime.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public DateTime GetImageDateModified(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                throw new ArgumentNullException("imagePath");
            }

            // See if we can avoid a file system lookup by looking for the file in ResolveArgs
            return FileSystem.GetLastWriteTimeUtc(imagePath);
        }

        /// <summary>
        /// Gets the file system path to delete when the item is to be deleted
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<string> GetDeletePaths()
        {
            return new[] { Path };
        }

        public Task SwapImages(ImageType type, int index1, int index2)
        {
            if (type != ImageType.Screenshot && type != ImageType.Backdrop)
            {
                throw new ArgumentException("The change index operation is only applicable to backdrops and screenshots");
            }

            var file1 = GetImagePath(type, index1);
            var file2 = GetImagePath(type, index2);

            FileSystem.SwapFiles(file1, file2);

            // Directory watchers should repeat this, but do a quick refresh first
            return RefreshMetadata(new MetadataRefreshOptions
            {
                ForceSave = true,
                MetadataRefreshMode = MetadataRefreshMode.None

            }, CancellationToken.None);
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
    }
}
