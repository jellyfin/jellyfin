using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Users;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class BaseItem
    /// </summary>
    public abstract class BaseItem : IHasProviderIds, IHasLookupInfo<ItemLookupInfo>, IEquatable<BaseItem>
    {
        /// <summary>
        /// The supported image extensions
        /// </summary>
        public static readonly string[] SupportedImageExtensions
            = new[] { ".png", ".jpg", ".jpeg", ".tbn", ".gif" };

        private static readonly List<string> _supportedExtensions = new List<string>(SupportedImageExtensions)
        {
            ".nfo",
            ".xml",
            ".srt",
            ".vtt",
            ".sub",
            ".idx",
            ".txt",
            ".edl",
            ".bif",
            ".smi",
            ".ttml"
        };

        protected BaseItem()
        {
            ThemeSongIds = Array.Empty<Guid>();
            ThemeVideoIds = Array.Empty<Guid>();
            Tags = Array.Empty<string>();
            Genres = Array.Empty<string>();
            Studios = Array.Empty<string>();
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            LockedFields = Array.Empty<MetadataFields>();
            ImageInfos = Array.Empty<ItemImageInfo>();
            ProductionLocations = Array.Empty<string>();
            RemoteTrailers = Array.Empty<MediaUrl>();
            ExtraIds = Array.Empty<Guid>();
        }

        public static readonly char[] SlugReplaceChars = { '?', '/', '&' };
        public static char SlugChar = '-';

        /// <summary>
        /// The trailer folder name
        /// </summary>
        public const string TrailerFolderName = "trailers";
        public const string ThemeSongsFolderName = "theme-music";
        public const string ThemeSongFilename = "theme";
        public const string ThemeVideosFolderName = "backdrops";
        public const string ExtrasFolderName = "extras";
        public const string BehindTheScenesFolderName = "behind the scenes";
        public const string DeletedScenesFolderName = "deleted scenes";
        public const string InterviewFolderName = "interviews";
        public const string SceneFolderName = "scenes";
        public const string SampleFolderName = "samples";

        public static readonly string[] AllExtrasTypesFolderNames = {
            ExtrasFolderName,
            BehindTheScenesFolderName,
            DeletedScenesFolderName,
            InterviewFolderName,
            SceneFolderName,
            SampleFolderName
        };

        [JsonIgnore]
        public Guid[] ThemeSongIds { get; set; }
        [JsonIgnore]
        public Guid[] ThemeVideoIds { get; set; }

        [JsonIgnore]
        public string PreferredMetadataCountryCode { get; set; }
        [JsonIgnore]
        public string PreferredMetadataLanguage { get; set; }

        public long? Size { get; set; }
        public string Container { get; set; }

        [JsonIgnore]
        public string Tagline { get; set; }

        [JsonIgnore]
        public virtual ItemImageInfo[] ImageInfos { get; set; }

        [JsonIgnore]
        public bool IsVirtualItem { get; set; }

        /// <summary>
        /// Gets or sets the album.
        /// </summary>
        /// <value>The album.</value>
        [JsonIgnore]
        public string Album { get; set; }

        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>The channel identifier.</value>
        [JsonIgnore]
        public Guid ChannelId { get; set; }

        [JsonIgnore]
        public virtual bool SupportsAddingToPlaylist => false;

        [JsonIgnore]
        public virtual bool AlwaysScanInternalMetadataPath => false;

        /// <summary>
        /// Gets a value indicating whether this instance is in mixed folder.
        /// </summary>
        /// <value><c>true</c> if this instance is in mixed folder; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsInMixedFolder { get; set; }

        [JsonIgnore]
        public virtual bool SupportsPlayedStatus => false;

        [JsonIgnore]
        public virtual bool SupportsPositionTicksResume => false;

        [JsonIgnore]
        public virtual bool SupportsRemoteImageDownloading => true;

        private string _name;
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [JsonIgnore]
        public virtual string Name
        {
            get => _name;
            set
            {
                _name = value;

                // lazy load this again
                _sortName = null;
            }
        }

        [JsonIgnore]
        public bool IsUnaired => PremiereDate.HasValue && PremiereDate.Value.ToLocalTime().Date >= DateTime.Now.Date;

        [JsonIgnore]
        public int? TotalBitrate { get; set; }

        [JsonIgnore]
        public ExtraType? ExtraType { get; set; }

        [JsonIgnore]
        public bool IsThemeMedia => ExtraType.HasValue && (ExtraType.Value == Model.Entities.ExtraType.ThemeSong || ExtraType.Value == Model.Entities.ExtraType.ThemeVideo);

        [JsonIgnore]
        public string OriginalTitle { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        [JsonIgnore]
        public Guid Id { get; set; }

        [JsonIgnore]
        public Guid OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the audio.
        /// </summary>
        /// <value>The audio.</value>
        [JsonIgnore]
        public ProgramAudio? Audio { get; set; }

        /// <summary>
        /// Return the id that should be used to key display prefs for this item.
        /// Default is based on the type for everything except actual generic folders.
        /// </summary>
        /// <value>The display prefs id.</value>
        [JsonIgnore]
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
        [JsonIgnore]
        public virtual string Path { get; set; }

        [JsonIgnore]
        public virtual SourceType SourceType
        {
            get
            {
                if (!ChannelId.Equals(Guid.Empty))
                {
                    return SourceType.Channel;
                }

                return SourceType.Library;
            }
        }

        /// <summary>
        /// Returns the folder containing the item.
        /// If the item is a folder, it returns the folder itself
        /// </summary>
        [JsonIgnore]
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

        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        /// <value>The name of the service.</value>
        [JsonIgnore]
        public string ServiceName { get; set; }

        /// <summary>
        /// If this content came from an external service, the id of the content on that service
        /// </summary>
        [JsonIgnore]
        public string ExternalId { get; set; }

        [JsonIgnore]
        public string ExternalSeriesId { get; set; }

        /// <summary>
        /// Gets or sets the etag.
        /// </summary>
        /// <value>The etag.</value>
        [JsonIgnore]
        public string ExternalEtag { get; set; }

        [JsonIgnore]
        public virtual bool IsHidden => false;

        public BaseItem GetOwner()
        {
            var ownerId = OwnerId;
            return ownerId.Equals(Guid.Empty) ? null : LibraryManager.GetItemById(ownerId);
        }

        /// <summary>
        /// Gets or sets the type of the location.
        /// </summary>
        /// <value>The type of the location.</value>
        [JsonIgnore]
        public virtual LocationType LocationType
        {
            get
            {
                //if (IsOffline)
                //{
                //    return LocationType.Offline;
                //}

                var path = Path;
                if (string.IsNullOrEmpty(path))
                {
                    if (SourceType == SourceType.Channel)
                    {
                        return LocationType.Remote;
                    }

                    return LocationType.Virtual;
                }

                return FileSystem.IsPathFile(path) ? LocationType.FileSystem : LocationType.Remote;
            }
        }

        [JsonIgnore]
        public MediaProtocol? PathProtocol
        {
            get
            {
                var path = Path;

                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                return MediaSourceManager.GetPathProtocol(path);
            }
        }

        public bool IsPathProtocol(MediaProtocol protocol)
        {
            var current = PathProtocol;

            return current.HasValue && current.Value == protocol;
        }

        [JsonIgnore]
        public bool IsFileProtocol => IsPathProtocol(MediaProtocol.File);

        [JsonIgnore]
        public bool HasPathProtocol => PathProtocol.HasValue;

        [JsonIgnore]
        public virtual bool SupportsLocalMetadata
        {
            get
            {
                if (SourceType == SourceType.Channel)
                {
                    return false;
                }

                return IsFileProtocol;
            }
        }

        [JsonIgnore]
        public virtual string FileNameWithoutExtension
        {
            get
            {
                if (IsFileProtocol)
                {
                    return System.IO.Path.GetFileNameWithoutExtension(Path);
                }

                return null;
            }
        }

        [JsonIgnore]
        public virtual bool EnableAlphaNumericSorting => true;

        private List<Tuple<StringBuilder, bool>> GetSortChunks(string s1)
        {
            var list = new List<Tuple<StringBuilder, bool>>();

            int thisMarker = 0;

            while (thisMarker < s1.Length)
            {
                char thisCh = s1[thisMarker];

                var thisChunk = new StringBuilder();
                bool isNumeric = char.IsDigit(thisCh);

                while (thisMarker < s1.Length && char.IsDigit(thisCh) == isNumeric)
                {
                    thisChunk.Append(thisCh);
                    thisMarker++;

                    if (thisMarker < s1.Length)
                    {
                        thisCh = s1[thisMarker];
                    }
                }

                list.Add(new Tuple<StringBuilder, bool>(thisChunk, isNumeric));
            }

            return list;
        }

        /// <summary>
        /// This is just a helper for convenience
        /// </summary>
        /// <value>The primary image path.</value>
        [JsonIgnore]
        public string PrimaryImagePath => this.GetImagePath(ImageType.Primary);

        public bool IsMetadataFetcherEnabled(LibraryOptions libraryOptions, string name)
        {
            if (SourceType == SourceType.Channel)
            {
                // hack alert
                return !EnableMediaSourceDisplay;
            }

            var typeOptions = libraryOptions.GetTypeOptions(GetType().Name);
            if (typeOptions != null)
            {
                return typeOptions.MetadataFetchers.Contains(name, StringComparer.OrdinalIgnoreCase);
            }

            if (!libraryOptions.EnableInternetProviders)
            {
                return false;
            }

            var itemConfig = ConfigurationManager.Configuration.MetadataOptions.FirstOrDefault(i => string.Equals(i.ItemType, GetType().Name, StringComparison.OrdinalIgnoreCase));

            return itemConfig == null || !itemConfig.DisabledMetadataFetchers.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        public bool IsImageFetcherEnabled(LibraryOptions libraryOptions, string name)
        {
            if (this is Channel)
            {
                // hack alert
                return true;
            }
            if (SourceType == SourceType.Channel)
            {
                // hack alert
                return !EnableMediaSourceDisplay;
            }

            var typeOptions = libraryOptions.GetTypeOptions(GetType().Name);
            if (typeOptions != null)
            {
                return typeOptions.ImageFetchers.Contains(name, StringComparer.OrdinalIgnoreCase);
            }

            if (!libraryOptions.EnableInternetProviders)
            {
                return false;
            }

            var itemConfig = ConfigurationManager.Configuration.MetadataOptions.FirstOrDefault(i => string.Equals(i.ItemType, GetType().Name, StringComparison.OrdinalIgnoreCase));

            return itemConfig == null || !itemConfig.DisabledImageFetchers.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        public virtual bool CanDelete()
        {
            if (SourceType == SourceType.Channel)
            {
                return ChannelManager.CanDelete(this);
            }

            return IsFileProtocol;
        }

        public virtual bool IsAuthorizedToDelete(User user, List<Folder> allCollectionFolders)
        {
            if (user.Policy.EnableContentDeletion)
            {
                return true;
            }

            var allowed = user.Policy.EnableContentDeletionFromFolders;

            if (SourceType == SourceType.Channel)
            {
                return allowed.Contains(ChannelId.ToString(""), StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                var collectionFolders = LibraryManager.GetCollectionFolders(this, allCollectionFolders);

                foreach (var folder in collectionFolders)
                {
                    if (allowed.Contains(folder.Id.ToString("N", CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool CanDelete(User user, List<Folder> allCollectionFolders)
        {
            return CanDelete() && IsAuthorizedToDelete(user, allCollectionFolders);
        }

        public bool CanDelete(User user)
        {
            var allCollectionFolders = LibraryManager.GetUserRootFolder().Children.OfType<Folder>().ToList();

            return CanDelete(user, allCollectionFolders);
        }

        public virtual bool CanDownload()
        {
            return false;
        }

        public virtual bool IsAuthorizedToDownload(User user)
        {
            return user.Policy.EnableContentDownloading;
        }

        public bool CanDownload(User user)
        {
            return CanDownload() && IsAuthorizedToDownload(user);
        }

        /// <summary>
        /// Gets or sets the date created.
        /// </summary>
        /// <value>The date created.</value>
        [JsonIgnore]
        public DateTime DateCreated { get; set; }

        /// <summary>
        /// Gets or sets the date modified.
        /// </summary>
        /// <value>The date modified.</value>
        [JsonIgnore]
        public DateTime DateModified { get; set; }

        public DateTime DateLastSaved { get; set; }

        [JsonIgnore]
        public DateTime DateLastRefreshed { get; set; }

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
        public static IChannelManager ChannelManager { get; set; }
        public static IMediaSourceManager MediaSourceManager { get; set; }

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Name;
        }

        [JsonIgnore]
        public bool IsLocked { get; set; }

        /// <summary>
        /// Gets or sets the locked fields.
        /// </summary>
        /// <value>The locked fields.</value>
        [JsonIgnore]
        public MetadataFields[] LockedFields { get; set; }

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        [JsonIgnore]
        public virtual string MediaType => null;

        [JsonIgnore]
        public virtual string[] PhysicalLocations
        {
            get
            {
                if (!IsFileProtocol)
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
        [JsonIgnore]
        public string ForcedSortName
        {
            get => _forcedSortName;
            set { _forcedSortName = value; _sortName = null; }
        }

        private string _sortName;
        /// <summary>
        /// Gets the name of the sort.
        /// </summary>
        /// <value>The name of the sort.</value>
        [JsonIgnore]
        public string SortName
        {
            get
            {
                if (_sortName == null)
                {
                    if (!string.IsNullOrEmpty(ForcedSortName))
                    {
                        // Need the ToLower because that's what CreateSortName does
                        _sortName = ModifySortChunks(ForcedSortName).ToLowerInvariant();
                    }
                    else
                    {
                        _sortName = CreateSortName();
                    }
                }
                return _sortName;
            }
            set => _sortName = value;
        }

        public string GetInternalMetadataPath()
        {
            var basePath = ConfigurationManager.ApplicationPaths.InternalMetadataPath;

            return GetInternalMetadataPath(basePath);
        }

        protected virtual string GetInternalMetadataPath(string basePath)
        {
            if (SourceType == SourceType.Channel)
            {
                return System.IO.Path.Combine(basePath, "channels", ChannelId.ToString("N", CultureInfo.InvariantCulture), Id.ToString("N", CultureInfo.InvariantCulture));
            }

            var idString = Id.ToString("N", CultureInfo.InvariantCulture);

            basePath = System.IO.Path.Combine(basePath, "library");

            return System.IO.Path.Combine(basePath, idString.Substring(0, 2), idString);
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected virtual string CreateSortName()
        {
            if (Name == null) return null; //some items may not have name filled in properly

            if (!EnableAlphaNumericSorting)
            {
                return Name.TrimStart();
            }

            var sortable = Name.Trim().ToLowerInvariant();

            foreach (var removeChar in ConfigurationManager.Configuration.SortRemoveCharacters)
            {
                sortable = sortable.Replace(removeChar, string.Empty);
            }

            foreach (var replaceChar in ConfigurationManager.Configuration.SortReplaceCharacters)
            {
                sortable = sortable.Replace(replaceChar, " ");
            }

            foreach (var search in ConfigurationManager.Configuration.SortRemoveWords)
            {
                // Remove from beginning if a space follows
                if (sortable.StartsWith(search + " "))
                {
                    sortable = sortable.Remove(0, search.Length + 1);
                }
                // Remove from middle if surrounded by spaces
                sortable = sortable.Replace(" " + search + " ", " ");

                // Remove from end if followed by a space
                if (sortable.EndsWith(" " + search))
                {
                    sortable = sortable.Remove(sortable.Length - (search.Length + 1));
                }
            }

            return ModifySortChunks(sortable);
        }

        private string ModifySortChunks(string name)
        {
            var chunks = GetSortChunks(name);

            var builder = new StringBuilder();

            foreach (var chunk in chunks)
            {
                var chunkBuilder = chunk.Item1;

                // This chunk is numeric
                if (chunk.Item2)
                {
                    while (chunkBuilder.Length < 10)
                    {
                        chunkBuilder.Insert(0, '0');
                    }
                }

                builder.Append(chunkBuilder);
            }
            //logger.LogDebug("ModifySortChunks Start: {0} End: {1}", name, builder.ToString());
            return builder.ToString().RemoveDiacritics();
        }

        [JsonIgnore]
        public bool EnableMediaSourceDisplay
        {
            get
            {
                if (SourceType == SourceType.Channel)
                {
                    return ChannelManager.EnableMediaSourceDisplay(this);
                }

                return true;
            }
        }

        [JsonIgnore]
        public Guid ParentId { get; set; }

        /// <summary>
        /// Gets or sets the parent.
        /// </summary>
        /// <value>The parent.</value>
        [JsonIgnore]
        public Folder Parent
        {
            get => GetParent() as Folder;
            set
            {

            }
        }

        public void SetParent(Folder parent)
        {
            ParentId = parent == null ? Guid.Empty : parent.Id;
        }

        public BaseItem GetParent()
        {
            var parentId = ParentId;
            if (!parentId.Equals(Guid.Empty))
            {
                return LibraryManager.GetItemById(parentId);
            }

            return null;
        }

        public IEnumerable<BaseItem> GetParents()
        {
            var parent = GetParent();

            while (parent != null)
            {
                yield return parent;

                parent = parent.GetParent();
            }
        }

        /// <summary>
        /// Finds a parent of a given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T FindParent<T>()
            where T : Folder
        {
            foreach (var parent in GetParents())
            {
                var item = parent as T;
                if (item != null)
                {
                    return item;
                }
            }
            return null;
        }

        [JsonIgnore]
        public virtual Guid DisplayParentId
        {
            get
            {
                var parentId = ParentId;
                return parentId;
            }
        }

        [JsonIgnore]
        public BaseItem DisplayParent
        {
            get
            {
                var id = DisplayParentId;
                if (id.Equals(Guid.Empty))
                {
                    return null;
                }
                return LibraryManager.GetItemById(id);
            }
        }

        /// <summary>
        /// When the item first debuted. For movies this could be premiere date, episodes would be first aired
        /// </summary>
        /// <value>The premiere date.</value>
        [JsonIgnore]
        public DateTime? PremiereDate { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        /// <value>The end date.</value>
        [JsonIgnore]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the official rating.
        /// </summary>
        /// <value>The official rating.</value>
        [JsonIgnore]
        public string OfficialRating { get; set; }

        [JsonIgnore]
        public int InheritedParentalRatingValue { get; set; }

        /// <summary>
        /// Gets or sets the critic rating.
        /// </summary>
        /// <value>The critic rating.</value>
        [JsonIgnore]
        public float? CriticRating { get; set; }

        /// <summary>
        /// Gets or sets the custom rating.
        /// </summary>
        /// <value>The custom rating.</value>
        [JsonIgnore]
        public string CustomRating { get; set; }

        /// <summary>
        /// Gets or sets the overview.
        /// </summary>
        /// <value>The overview.</value>
        [JsonIgnore]
        public string Overview { get; set; }

        /// <summary>
        /// Gets or sets the studios.
        /// </summary>
        /// <value>The studios.</value>
        [JsonIgnore]
        public string[] Studios { get; set; }

        /// <summary>
        /// Gets or sets the genres.
        /// </summary>
        /// <value>The genres.</value>
        [JsonIgnore]
        public string[] Genres { get; set; }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        [JsonIgnore]
        public string[] Tags { get; set; }

        [JsonIgnore]
        public string[] ProductionLocations { get; set; }

        /// <summary>
        /// Gets or sets the home page URL.
        /// </summary>
        /// <value>The home page URL.</value>
        [JsonIgnore]
        public string HomePageUrl { get; set; }

        /// <summary>
        /// Gets or sets the community rating.
        /// </summary>
        /// <value>The community rating.</value>
        [JsonIgnore]
        public float? CommunityRating { get; set; }

        /// <summary>
        /// Gets or sets the run time ticks.
        /// </summary>
        /// <value>The run time ticks.</value>
        [JsonIgnore]
        public long? RunTimeTicks { get; set; }

        /// <summary>
        /// Gets or sets the production year.
        /// </summary>
        /// <value>The production year.</value>
        [JsonIgnore]
        public int? ProductionYear { get; set; }

        /// <summary>
        /// If the item is part of a series, this is it's number in the series.
        /// This could be episode number, album track number, etc.
        /// </summary>
        /// <value>The index number.</value>
        [JsonIgnore]
        public int? IndexNumber { get; set; }

        /// <summary>
        /// For an episode this could be the season number, or for a song this could be the disc number.
        /// </summary>
        /// <value>The parent index number.</value>
        [JsonIgnore]
        public int? ParentIndexNumber { get; set; }

        [JsonIgnore]
        public virtual bool HasLocalAlternateVersions => false;

        [JsonIgnore]
        public string OfficialRatingForComparison
        {
            get
            {
                var officialRating = OfficialRating;
                if (!string.IsNullOrEmpty(officialRating))
                {
                    return officialRating;
                }

                var parent = DisplayParent;
                if (parent != null)
                {
                    return parent.OfficialRatingForComparison;
                }

                return null;
            }
        }

        [JsonIgnore]
        public string CustomRatingForComparison
        {
            get
            {
                var customRating = CustomRating;
                if (!string.IsNullOrEmpty(customRating))
                {
                    return customRating;
                }

                var parent = DisplayParent;
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
            if (!user.Policy.EnableMediaPlayback)
            {
                return PlayAccess.None;
            }

            //if (!user.IsParentalScheduleAllowed())
            //{
            //    return PlayAccess.None;
            //}

            return PlayAccess.Full;
        }

        public virtual List<MediaStream> GetMediaStreams()
        {
            return MediaSourceManager.GetMediaStreams(new MediaStreamQuery
            {
                ItemId = Id
            });
        }

        protected virtual bool IsActiveRecording()
        {
            return false;
        }

        public virtual List<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution)
        {
            if (SourceType == SourceType.Channel)
            {
                var sources = ChannelManager.GetStaticMediaSources(this, CancellationToken.None)
                           .ToList();

                if (sources.Count > 0)
                {
                    return sources;
                }
            }

            var list = GetAllItemsForMediaSources();
            var result = list.Select(i => GetVersionInfo(enablePathSubstitution, i.Item1, i.Item2)).ToList();

            if (IsActiveRecording())
            {
                foreach (var mediaSource in result)
                {
                    mediaSource.Type = MediaSourceType.Placeholder;
                }
            }

            return result.OrderBy(i =>
            {
                if (i.VideoType == VideoType.VideoFile)
                {
                    return 0;
                }

                return 1;

            }).ThenBy(i => i.Video3DFormat.HasValue ? 1 : 0)
            .ThenByDescending(i =>
            {
                var stream = i.VideoStream;

                return stream == null || stream.Width == null ? 0 : stream.Width.Value;
            })
            .ToList();
        }

        protected virtual List<Tuple<BaseItem, MediaSourceType>> GetAllItemsForMediaSources()
        {
            return new List<Tuple<BaseItem, MediaSourceType>>();
        }

        private MediaSourceInfo GetVersionInfo(bool enablePathSubstitution, BaseItem item, MediaSourceType type)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var protocol = item.PathProtocol;

            var info = new MediaSourceInfo
            {
                Id = item.Id.ToString("N", CultureInfo.InvariantCulture),
                Protocol = protocol ?? MediaProtocol.File,
                MediaStreams = MediaSourceManager.GetMediaStreams(item.Id),
                MediaAttachments = MediaSourceManager.GetMediaAttachments(item.Id),
                Name = GetMediaSourceName(item),
                Path = enablePathSubstitution ? GetMappedPath(item, item.Path, protocol) : item.Path,
                RunTimeTicks = item.RunTimeTicks,
                Container = item.Container,
                Size = item.Size,
                Type = type
            };

            if (string.IsNullOrEmpty(info.Path))
            {
                info.Type = MediaSourceType.Placeholder;
            }

            if (info.Protocol == MediaProtocol.File)
            {
                info.ETag = item.DateModified.Ticks.ToString(CultureInfo.InvariantCulture).GetMD5().ToString("N", CultureInfo.InvariantCulture);
            }

            var video = item as Video;
            if (video != null)
            {
                info.IsoType = video.IsoType;
                info.VideoType = video.VideoType;
                info.Video3DFormat = video.Video3DFormat;
                info.Timestamp = video.Timestamp;

                if (video.IsShortcut)
                {
                    info.IsRemote = true;
                    info.Path = video.ShortcutPath;
                    info.Protocol = MediaSourceManager.GetPathProtocol(info.Path);
                }

                if (string.IsNullOrEmpty(info.Container))
                {
                    if (video.VideoType == VideoType.VideoFile || video.VideoType == VideoType.Iso)
                    {
                        if (protocol.HasValue && protocol.Value == MediaProtocol.File)
                        {
                            info.Container = System.IO.Path.GetExtension(item.Path).TrimStart('.');
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(info.Container))
            {
                if (protocol.HasValue && protocol.Value == MediaProtocol.File)
                {
                    info.Container = System.IO.Path.GetExtension(item.Path).TrimStart('.');
                }
            }

            if (info.SupportsDirectStream && !string.IsNullOrEmpty(info.Path))
            {
                info.SupportsDirectStream = MediaSourceManager.SupportsDirectStream(info.Path, info.Protocol);
            }

            if (video != null && video.VideoType != VideoType.VideoFile)
            {
                info.SupportsDirectStream = false;
            }

            info.Bitrate = item.TotalBitrate;
            info.InferTotalBitrate();

            return info;
        }

        private string GetMediaSourceName(BaseItem item)
        {
            var terms = new List<string>();

            var path = item.Path;
            if (item.IsFileProtocol && !string.IsNullOrEmpty(path))
            {
                if (HasLocalAlternateVersions)
                {
                    var displayName = System.IO.Path.GetFileNameWithoutExtension(path)
                        .Replace(System.IO.Path.GetFileName(ContainingFolderPath), string.Empty, StringComparison.OrdinalIgnoreCase)
                        .TrimStart(new char[] { ' ', '-' });

                    if (!string.IsNullOrEmpty(displayName))
                    {
                        terms.Add(displayName);
                    }
                }

                if (terms.Count == 0)
                {
                    var displayName = System.IO.Path.GetFileNameWithoutExtension(path);
                    terms.Add(displayName);
                }
            }

            if (terms.Count == 0)
            {
                terms.Add(item.Name);
            }

            var video = item as Video;
            if (video != null)
            {
                if (video.Video3DFormat.HasValue)
                {
                    terms.Add("3D");
                }

                if (video.VideoType == VideoType.BluRay)
                {
                    terms.Add("Bluray");
                }
                else if (video.VideoType == VideoType.Dvd)
                {
                    terms.Add("DVD");
                }
                else if (video.VideoType == VideoType.Iso)
                {
                    if (video.IsoType.HasValue)
                    {
                        if (video.IsoType.Value == Model.Entities.IsoType.BluRay)
                        {
                            terms.Add("Bluray");
                        }
                        else if (video.IsoType.Value == Model.Entities.IsoType.Dvd)
                        {
                            terms.Add("DVD");
                        }
                    }
                    else
                    {
                        terms.Add("ISO");
                    }
                }
            }

            return string.Join("/", terms.ToArray());
        }

        /// <summary>
        /// Loads the theme songs.
        /// </summary>
        /// <returns>List{Audio.Audio}.</returns>
        private static Audio.Audio[] LoadThemeSongs(List<FileSystemMetadata> fileSystemChildren, IDirectoryService directoryService)
        {
            var files = fileSystemChildren.Where(i => i.IsDirectory)
                .Where(i => string.Equals(i.Name, ThemeSongsFolderName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => FileSystem.GetFiles(i.FullName))
                .ToList();

            // Support plex/xbmc convention
            files.AddRange(fileSystemChildren
                .Where(i => !i.IsDirectory && string.Equals(FileSystem.GetFileNameWithoutExtension(i), ThemeSongFilename, StringComparison.OrdinalIgnoreCase))
                );

            return LibraryManager.ResolvePaths(files, directoryService, null, new LibraryOptions())
                .OfType<Audio.Audio>()
                .Select(audio =>
                {
                    // Try to retrieve it from the db. If we don't find it, use the resolved version
                    var dbItem = LibraryManager.GetItemById(audio.Id) as Audio.Audio;

                    if (dbItem != null)
                    {
                        audio = dbItem;
                    }
                    else
                    {
                        // item is new
                        audio.ExtraType = MediaBrowser.Model.Entities.ExtraType.ThemeSong;
                    }

                    return audio;

                    // Sort them so that the list can be easily compared for changes
                }).OrderBy(i => i.Path).ToArray();
        }

        /// <summary>
        /// Loads the video backdrops.
        /// </summary>
        /// <returns>List{Video}.</returns>
        private static Video[] LoadThemeVideos(IEnumerable<FileSystemMetadata> fileSystemChildren, IDirectoryService directoryService)
        {
            var files = fileSystemChildren.Where(i => i.IsDirectory)
                .Where(i => string.Equals(i.Name, ThemeVideosFolderName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => FileSystem.GetFiles(i.FullName));

            return LibraryManager.ResolvePaths(files, directoryService, null, new LibraryOptions())
                .OfType<Video>()
                .Select(item =>
                {
                    // Try to retrieve it from the db. If we don't find it, use the resolved version

                    if (LibraryManager.GetItemById(item.Id) is Video dbItem)
                    {
                        item = dbItem;
                    }
                    else
                    {
                        // item is new
                        item.ExtraType = Model.Entities.ExtraType.ThemeVideo;
                    }

                    return item;

                    // Sort them so that the list can be easily compared for changes
                }).OrderBy(i => i.Path).ToArray();
        }

        protected virtual BaseItem[] LoadExtras(List<FileSystemMetadata> fileSystemChildren, IDirectoryService directoryService)
        {
            var extras = new List<Video>();

            var folders = fileSystemChildren.Where(i => i.IsDirectory).ToArray();
            foreach (var extraFolderName in AllExtrasTypesFolderNames)
            {
                var files = folders
                    .Where(i => string.Equals(i.Name, extraFolderName, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(i => FileSystem.GetFiles(i.FullName));

                extras.AddRange(LibraryManager.ResolvePaths(files, directoryService, null, new LibraryOptions())
                    .OfType<Video>()
                    .Select(item =>
                    {
                        // Try to retrieve it from the db. If we don't find it, use the resolved version
                        if (LibraryManager.GetItemById(item.Id) is Video dbItem)
                        {
                            item = dbItem;
                        }

                        // Use some hackery to get the extra type based on foldername
                        item.ExtraType = Enum.TryParse(extraFolderName.Replace(" ", string.Empty), true, out ExtraType extraType)
                            ? extraType
                            : Model.Entities.ExtraType.Unknown;

                        return item;

                        // Sort them so that the list can be easily compared for changes
                    }).OrderBy(i => i.Path));
            }

            return extras.ToArray();
        }

        public Task RefreshMetadata(CancellationToken cancellationToken)
        {
            return RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(FileSystem)), cancellationToken);
        }

        protected virtual void TriggerOnRefreshStart()
        {

        }

        protected virtual void TriggerOnRefreshComplete()
        {

        }

        /// <summary>
        /// Overrides the base implementation to refresh metadata for local trailers
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>true if a provider reports we changed</returns>
        public async Task<ItemUpdateType> RefreshMetadata(MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            TriggerOnRefreshStart();

            var requiresSave = false;

            if (SupportsOwnedItems)
            {
                try
                {
                    var files = IsFileProtocol ?
                        GetFileSystemChildren(options.DirectoryService).ToList() :
                        new List<FileSystemMetadata>();

                    var ownedItemsChanged = await RefreshedOwnedItems(options, files, cancellationToken).ConfigureAwait(false);
                    LibraryManager.UpdateImages(this); // ensure all image properties in DB are fresh

                    if (ownedItemsChanged)
                    {
                        requiresSave = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error refreshing owned items for {path}", Path ?? Name);
                }
            }

            try
            {
                var refreshOptions = requiresSave
                    ? new MetadataRefreshOptions(options)
                    {
                        ForceSave = true
                    }
                    : options;

                return await ProviderManager.RefreshSingleItem(this, refreshOptions, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                TriggerOnRefreshComplete();
            }
        }

        [JsonIgnore]
        protected virtual bool SupportsOwnedItems => !ParentId.Equals(Guid.Empty) && IsFileProtocol;

        [JsonIgnore]
        public virtual bool SupportsPeople => false;

        [JsonIgnore]
        public virtual bool SupportsThemeMedia => false;

        /// <summary>
        /// Refreshes owned items such as trailers, theme videos, special features, etc.
        /// Returns true or false indicating if changes were found.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="fileSystemChildren"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task<bool> RefreshedOwnedItems(MetadataRefreshOptions options, List<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
        {
            var themeSongsChanged = false;

            var themeVideosChanged = false;

            var extrasChanged = false;

            var localTrailersChanged = false;

            if (IsFileProtocol && SupportsOwnedItems)
            {
                if (SupportsThemeMedia)
                {
                    if (!IsInMixedFolder)
                    {
                        themeSongsChanged = await RefreshThemeSongs(this, options, fileSystemChildren, cancellationToken).ConfigureAwait(false);

                        themeVideosChanged = await RefreshThemeVideos(this, options, fileSystemChildren, cancellationToken).ConfigureAwait(false);

                        extrasChanged = await RefreshExtras(this, options, fileSystemChildren, cancellationToken).ConfigureAwait(false);
                    }
                }

                var hasTrailers = this as IHasTrailers;
                if (hasTrailers != null)
                {
                    localTrailersChanged = await RefreshLocalTrailers(hasTrailers, options, fileSystemChildren, cancellationToken).ConfigureAwait(false);
                }
            }

            return themeSongsChanged || themeVideosChanged || extrasChanged || localTrailersChanged;
        }

        protected virtual FileSystemMetadata[] GetFileSystemChildren(IDirectoryService directoryService)
        {
            var path = ContainingFolderPath;

            return directoryService.GetFileSystemEntries(path);
        }

        private async Task<bool> RefreshLocalTrailers(IHasTrailers item, MetadataRefreshOptions options, List<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
        {
            var newItems = LibraryManager.FindTrailers(this, fileSystemChildren, options.DirectoryService);

            var newItemIds = newItems.Select(i => i.Id);

            var itemsChanged = !item.LocalTrailerIds.SequenceEqual(newItemIds);
            var ownerId = item.Id;

            var tasks = newItems.Select(i =>
            {
                var subOptions = new MetadataRefreshOptions(options);

                if (i.ExtraType != Model.Entities.ExtraType.Trailer ||
                    i.OwnerId != ownerId ||
                    !i.ParentId.Equals(Guid.Empty))
                {
                    i.ExtraType = Model.Entities.ExtraType.Trailer;
                    i.OwnerId = ownerId;
                    i.ParentId = Guid.Empty;
                    subOptions.ForceSave = true;
                }

                return RefreshMetadataForOwnedItem(i, true, subOptions, cancellationToken);
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            item.LocalTrailerIds = newItemIds.ToArray();

            return itemsChanged;
        }

        private async Task<bool> RefreshExtras(BaseItem item, MetadataRefreshOptions options, List<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
        {
            var extras = LoadExtras(fileSystemChildren, options.DirectoryService);
            var themeVideos = LoadThemeVideos(fileSystemChildren, options.DirectoryService);
            var themeSongs = LoadThemeSongs(fileSystemChildren, options.DirectoryService);
            var newExtras = new BaseItem[extras.Length + themeVideos.Length + themeSongs.Length];
            extras.CopyTo(newExtras, 0);
            themeVideos.CopyTo(newExtras, extras.Length);
            themeSongs.CopyTo(newExtras, extras.Length + themeVideos.Length);

            var newExtraIds = newExtras.Select(i => i.Id).ToArray();

            var extrasChanged = !item.ExtraIds.SequenceEqual(newExtraIds);

            if (extrasChanged)
            {
                var ownerId = item.Id;

                var tasks = newExtras.Select(i =>
                {
                    var subOptions = new MetadataRefreshOptions(options);
                    if (i.OwnerId != ownerId || i.ParentId != Guid.Empty)
                    {
                        i.OwnerId = ownerId;
                        i.ParentId = Guid.Empty;
                        subOptions.ForceSave = true;
                    }

                    return RefreshMetadataForOwnedItem(i, true, subOptions, cancellationToken);
                });

                await Task.WhenAll(tasks).ConfigureAwait(false);

                item.ExtraIds = newExtraIds;
            }

            return extrasChanged;
        }

        private async Task<bool> RefreshThemeVideos(BaseItem item, MetadataRefreshOptions options, IEnumerable<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
        {
            var newThemeVideos = LoadThemeVideos(fileSystemChildren, options.DirectoryService);

            var newThemeVideoIds = newThemeVideos.Select(i => i.Id).ToArray();

            var themeVideosChanged = !item.ThemeVideoIds.SequenceEqual(newThemeVideoIds);

            var ownerId = item.Id;

            var tasks = newThemeVideos.Select(i =>
            {
                var subOptions = new MetadataRefreshOptions(options);

                if (!i.ExtraType.HasValue ||
                    i.ExtraType.Value != Model.Entities.ExtraType.ThemeVideo ||
                    i.OwnerId != ownerId ||
                    !i.ParentId.Equals(Guid.Empty))
                {
                    i.ExtraType = Model.Entities.ExtraType.ThemeVideo;
                    i.OwnerId = ownerId;
                    i.ParentId = Guid.Empty;
                    subOptions.ForceSave = true;
                }

                return RefreshMetadataForOwnedItem(i, true, subOptions, cancellationToken);
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            item.ThemeVideoIds = newThemeVideoIds;

            return themeVideosChanged;
        }

        /// <summary>
        /// Refreshes the theme songs.
        /// </summary>
        private async Task<bool> RefreshThemeSongs(BaseItem item, MetadataRefreshOptions options, List<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
        {
            var newThemeSongs = LoadThemeSongs(fileSystemChildren, options.DirectoryService);
            var newThemeSongIds = newThemeSongs.Select(i => i.Id).ToArray();

            var themeSongsChanged = !item.ThemeSongIds.SequenceEqual(newThemeSongIds);

            var ownerId = item.Id;

            var tasks = newThemeSongs.Select(i =>
            {
                var subOptions = new MetadataRefreshOptions(options);

                if (!i.ExtraType.HasValue ||
                    i.ExtraType.Value != Model.Entities.ExtraType.ThemeSong ||
                    i.OwnerId != ownerId ||
                    !i.ParentId.Equals(Guid.Empty))
                {
                    i.ExtraType = Model.Entities.ExtraType.ThemeSong;
                    i.OwnerId = ownerId;
                    i.ParentId = Guid.Empty;
                    subOptions.ForceSave = true;
                }

                return RefreshMetadataForOwnedItem(i, true, subOptions, cancellationToken);
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            item.ThemeSongIds = newThemeSongIds;

            return themeSongsChanged;
        }

        /// <summary>
        /// Gets or sets the provider ids.
        /// </summary>
        /// <value>The provider ids.</value>
        [JsonIgnore]
        public Dictionary<string, string> ProviderIds { get; set; }

        [JsonIgnore]
        public virtual Folder LatestItemsIndexContainer => null;

        public virtual double GetDefaultPrimaryImageAspectRatio()
        {
            return 0;
        }

        public virtual string CreatePresentationUniqueKey()
        {
            return Id.ToString("N", CultureInfo.InvariantCulture);
        }

        [JsonIgnore]
        public string PresentationUniqueKey { get; set; }

        public string GetPresentationUniqueKey()
        {
            return PresentationUniqueKey ?? CreatePresentationUniqueKey();
        }

        public virtual bool RequiresRefresh()
        {
            return false;
        }

        public virtual List<string> GetUserDataKeys()
        {
            var list = new List<string>();

            if (SourceType == SourceType.Channel)
            {
                if (!string.IsNullOrEmpty(ExternalId))
                {
                    list.Add(ExternalId);
                }
            }

            list.Add(Id.ToString());
            return list;
        }

        internal virtual ItemUpdateType UpdateFromResolvedItem(BaseItem newItem)
        {
            var updateType = ItemUpdateType.None;

            if (IsInMixedFolder != newItem.IsInMixedFolder)
            {
                IsInMixedFolder = newItem.IsInMixedFolder;
                updateType |= ItemUpdateType.MetadataImport;
            }

            return updateType;
        }

        public void AfterMetadataRefresh()
        {
            _sortName = null;
        }

        /// <summary>
        /// Gets the preferred metadata language.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetPreferredMetadataLanguage()
        {
            string lang = PreferredMetadataLanguage;

            if (string.IsNullOrEmpty(lang))
            {
                lang = GetParents()
                    .Select(i => i.PreferredMetadataLanguage)
                    .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            }

            if (string.IsNullOrEmpty(lang))
            {
                lang = LibraryManager.GetCollectionFolders(this)
                    .Select(i => i.PreferredMetadataLanguage)
                    .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            }

            if (string.IsNullOrEmpty(lang))
            {
                lang = LibraryManager.GetLibraryOptions(this).PreferredMetadataLanguage;
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
            string lang = PreferredMetadataCountryCode;

            if (string.IsNullOrEmpty(lang))
            {
                lang = GetParents()
                    .Select(i => i.PreferredMetadataCountryCode)
                    .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            }

            if (string.IsNullOrEmpty(lang))
            {
                lang = LibraryManager.GetCollectionFolders(this)
                    .Select(i => i.PreferredMetadataCountryCode)
                    .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            }

            if (string.IsNullOrEmpty(lang))
            {
                lang = LibraryManager.GetLibraryOptions(this).MetadataCountryCode;
            }

            if (string.IsNullOrEmpty(lang))
            {
                lang = ConfigurationManager.Configuration.MetadataCountryCode;
            }

            return lang;
        }

        public virtual bool IsSaveLocalMetadataEnabled()
        {
            if (SourceType == SourceType.Channel)
            {
                return false;
            }

            var libraryOptions = LibraryManager.GetLibraryOptions(this);

            return libraryOptions.SaveLocalMetadata;
        }

        /// <summary>
        /// Determines if a given user has access to this item
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if [is parental allowed] [the specified user]; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public bool IsParentalAllowed(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (!IsVisibleViaTags(user))
            {
                return false;
            }

            var maxAllowedRating = user.Policy.MaxParentalRating;

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
                return !GetBlockUnratedValue(user.Policy);
            }

            var value = LocalizationManager.GetRatingLevel(rating);

            // Could not determine the integer value
            if (!value.HasValue)
            {
                var isAllowed = !GetBlockUnratedValue(user.Policy);

                if (!isAllowed)
                {
                    Logger.LogDebug("{0} has an unrecognized parental rating of {1}.", Name, rating);
                }

                return isAllowed;
            }

            return value.Value <= maxAllowedRating.Value;
        }

        public int? GetParentalRatingValue()
        {
            var rating = CustomRating;

            if (string.IsNullOrEmpty(rating))
            {
                rating = OfficialRating;
            }

            if (string.IsNullOrEmpty(rating))
            {
                return null;
            }

            return LocalizationManager.GetRatingLevel(rating);
        }

        public int? GetInheritedParentalRatingValue()
        {
            var rating = CustomRatingForComparison;

            if (string.IsNullOrEmpty(rating))
            {
                rating = OfficialRatingForComparison;
            }

            if (string.IsNullOrEmpty(rating))
            {
                return null;
            }

            return LocalizationManager.GetRatingLevel(rating);
        }

        public List<string> GetInheritedTags()
        {
            var list = new List<string>();
            list.AddRange(Tags);

            foreach (var parent in GetParents())
            {
                list.AddRange(parent.Tags);
            }

            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private bool IsVisibleViaTags(User user)
        {
            var policy = user.Policy;
            if (policy.BlockedTags.Any(i => Tags.Contains(i, StringComparer.OrdinalIgnoreCase)))
            {
                return false;
            }

            return true;
        }

        protected virtual bool IsAllowTagFilterEnforced()
        {
            return true;
        }

        public virtual UnratedItem GetBlockUnratedType()
        {
            if (SourceType == SourceType.Channel)
            {
                return UnratedItem.ChannelContent;
            }

            return UnratedItem.Other;
        }

        /// <summary>
        /// Gets the block unrated value.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected virtual bool GetBlockUnratedValue(UserPolicy config)
        {
            // Don't block plain folders that are unrated. Let the media underneath get blocked
            // Special folders like series and albums will override this method.
            if (IsFolder)
            {
                return false;
            }
            if (this is IItemByName)
            {
                return false;
            }

            return config.BlockUnratedItems.Contains(GetBlockUnratedType());
        }

        /// <summary>
        /// Determines if this folder should be visible to a given user.
        /// Default is just parental allowed. Can be overridden for more functionality.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns><c>true</c> if the specified user is visible; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public virtual bool IsVisible(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return IsParentalAllowed(user);
        }

        public virtual bool IsVisibleStandalone(User user)
        {
            if (SourceType == SourceType.Channel)
            {
                return IsVisibleStandaloneInternal(user, false) && Channel.IsChannelVisible(this, user);
            }

            return IsVisibleStandaloneInternal(user, true);
        }

        [JsonIgnore]
        public virtual bool SupportsInheritedParentImages => false;

        protected bool IsVisibleStandaloneInternal(User user, bool checkFolders)
        {
            if (!IsVisible(user))
            {
                return false;
            }

            if (GetParents().Any(i => !i.IsVisible(user)))
            {
                return false;
            }

            if (checkFolders)
            {
                var topParent = GetParents().LastOrDefault() ?? this;

                if (string.IsNullOrEmpty(topParent.Path))
                {
                    return true;
                }

                var itemCollectionFolders = LibraryManager.GetCollectionFolders(this).Select(i => i.Id).ToList();

                if (itemCollectionFolders.Count > 0)
                {
                    var userCollectionFolders = LibraryManager.GetUserRootFolder().GetChildren(user, true).Select(i => i.Id).ToList();
                    if (!itemCollectionFolders.Any(userCollectionFolders.Contains))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is folder.
        /// </summary>
        /// <value><c>true</c> if this instance is folder; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public virtual bool IsFolder => false;

        [JsonIgnore]
        public virtual bool IsDisplayedAsFolder => false;

        public virtual string GetClientTypeName()
        {
            if (IsFolder && SourceType == SourceType.Channel && !(this is Channel))
            {
                return "ChannelFolderItem";
            }

            return GetType().Name;
        }

        /// <summary>
        /// Gets the linked child.
        /// </summary>
        /// <param name="info">The info.</param>
        /// <returns>BaseItem.</returns>
        protected BaseItem GetLinkedChild(LinkedChild info)
        {
            // First get using the cached Id
            if (info.ItemId.HasValue)
            {
                if (info.ItemId.Value.Equals(Guid.Empty))
                {
                    return null;
                }

                var itemById = LibraryManager.GetItemById(info.ItemId.Value);

                if (itemById != null)
                {
                    return itemById;
                }
            }

            var item = FindLinkedChild(info);

            // If still null, log
            if (item == null)
            {
                // Don't keep searching over and over
                info.ItemId = Guid.Empty;
            }
            else
            {
                // Cache the id for next time
                info.ItemId = item.Id;
            }

            return item;
        }

        private BaseItem FindLinkedChild(LinkedChild info)
        {
            var path = info.Path;

            if (!string.IsNullOrEmpty(path))
            {
                path = FileSystem.MakeAbsolutePath(ContainingFolderPath, path);

                var itemByPath = LibraryManager.FindByPath(path, null);

                if (itemByPath == null)
                {
                    Logger.LogWarning("Unable to find linked item at path {0}", info.Path);
                }

                return itemByPath;
            }

            if (!string.IsNullOrEmpty(info.LibraryItemId))
            {
                var item = LibraryManager.GetItemById(info.LibraryItemId);

                if (item == null)
                {
                    Logger.LogWarning("Unable to find linked item at path {0}", info.Path);
                }

                return item;
            }

            return null;
        }

        [JsonIgnore]
        public virtual bool EnableRememberingTrackSelections => true;

        /// <summary>
        /// Adds a studio to the item
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddStudio(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var current = Studios;

            if (!current.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                int curLen = current.Length;
                if (curLen == 0)
                {
                    Studios = new[] { name };
                }
                else
                {
                    var newArr = new string[curLen + 1];
                    current.CopyTo(newArr, 0);
                    newArr[curLen] = name;
                    Studios = newArr;
                }
            }
        }

        public void SetStudios(IEnumerable<string> names)
        {
            Studios = names.Distinct().ToArray();
        }

        /// <summary>
        /// Adds a genre to the item
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddGenre(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var genres = Genres;
            if (!genres.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                var list = genres.ToList();
                list.Add(name);
                Genres = list.ToArray();
            }
        }

        /// <summary>
        /// Marks the played.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="datePlayed">The date played.</param>
        /// <param name="resetPosition">if set to <c>true</c> [reset position].</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual void MarkPlayed(User user,
            DateTime? datePlayed,
            bool resetPosition)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var data = UserDataManager.GetUserData(user, this);

            if (datePlayed.HasValue)
            {
                // Increment
                data.PlayCount++;
            }

            // Ensure it's at least one
            data.PlayCount = Math.Max(data.PlayCount, 1);

            if (resetPosition)
            {
                data.PlaybackPositionTicks = 0;
            }

            data.LastPlayedDate = datePlayed ?? data.LastPlayedDate ?? DateTime.UtcNow;
            data.Played = true;

            UserDataManager.SaveUserData(user.Id, this, data, UserDataSaveReason.TogglePlayed, CancellationToken.None);
        }

        /// <summary>
        /// Marks the unplayed.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual void MarkUnplayed(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var data = UserDataManager.GetUserData(user, this);

            //I think it is okay to do this here.
            // if this is only called when a user is manually forcing something to un-played
            // then it probably is what we want to do...
            data.PlayCount = 0;
            data.PlaybackPositionTicks = 0;
            data.LastPlayedDate = null;
            data.Played = false;

            UserDataManager.SaveUserData(user.Id, this, data, UserDataSaveReason.TogglePlayed, CancellationToken.None);
        }

        /// <summary>
        /// Do whatever refreshing is necessary when the filesystem pertaining to this item has changed.
        /// </summary>
        public virtual void ChangedExternally()
        {
            ProviderManager.QueueRefresh(Id, new MetadataRefreshOptions(new DirectoryService(FileSystem)), RefreshPriority.High);
        }

        /// <summary>
        /// Gets an image
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns><c>true</c> if the specified type has image; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Backdrops should be accessed using Item.Backdrops</exception>
        public bool HasImage(ImageType type, int imageIndex)
        {
            return GetImageInfo(type, imageIndex) != null;
        }

        public void SetImage(ItemImageInfo image, int index)
        {
            if (image.Type == ImageType.Chapter)
            {
                throw new ArgumentException("Cannot set chapter images using SetImagePath");
            }

            var existingImage = GetImageInfo(image.Type, index);

            if (existingImage != null)
            {
                existingImage.Path = image.Path;
                existingImage.DateModified = image.DateModified;
                existingImage.Width = image.Width;
                existingImage.Height = image.Height;
                existingImage.BlurHash = image.BlurHash;
            }
            else
            {
                var current = ImageInfos;
                var currentCount = current.Length;
                var newArr = new ItemImageInfo[currentCount + 1];
                current.CopyTo(newArr, 0);
                newArr[currentCount] = image;
                ImageInfos = newArr;
            }
        }

        public void SetImagePath(ImageType type, int index, FileSystemMetadata file)
        {
            if (type == ImageType.Chapter)
            {
                throw new ArgumentException("Cannot set chapter images using SetImagePath");
            }

            var image = GetImageInfo(type, index);

            if (image == null)
            {
                ImageInfos = ImageInfos.Concat(new[] { GetImageInfo(file, type) }).ToArray();
            }
            else
            {
                var imageInfo = GetImageInfo(file, type);

                image.Path = file.FullName;
                image.DateModified = imageInfo.DateModified;

                // reset these values
                image.Width = 0;
                image.Height = 0;
            }
        }

        /// <summary>
        /// Deletes the image.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="index">The index.</param>
        public void DeleteImage(ImageType type, int index)
        {
            var info = GetImageInfo(type, index);

            if (info == null)
            {
                // Nothing to do
                return;
            }

            // Remove it from the item
            RemoveImage(info);

            if (info.IsLocalFile)
            {
                FileSystem.DeleteFile(info.Path);
            }

            UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None);
        }

        public void RemoveImage(ItemImageInfo image)
        {
            RemoveImages(new List<ItemImageInfo> { image });
        }

        public void RemoveImages(List<ItemImageInfo> deletedImages)
        {
            ImageInfos = ImageInfos.Except(deletedImages).ToArray();
        }

        public virtual void UpdateToRepository(ItemUpdateType updateReason, CancellationToken cancellationToken)
        {
            LibraryManager.UpdateItem(this, GetParent(), updateReason, cancellationToken);
        }

        /// <summary>
        /// Validates that images within the item are still on the filesystem.
        /// </summary>
        public bool ValidateImages(IDirectoryService directoryService)
        {
            var allFiles = ImageInfos
                .Where(i => i.IsLocalFile)
                .Select(i => System.IO.Path.GetDirectoryName(i.Path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .SelectMany(i => directoryService.GetFilePaths(i))
                .ToList();

            var deletedImages = ImageInfos
                .Where(image => image.IsLocalFile && !allFiles.Contains(image.Path, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (deletedImages.Count > 0)
            {
                ImageInfos = ImageInfos.Except(deletedImages).ToArray();
            }

            return deletedImages.Count > 0;
        }

        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="InvalidOperationException">
        /// </exception>
        /// <exception cref="ArgumentNullException">item</exception>
        public string GetImagePath(ImageType imageType, int imageIndex)
            => GetImageInfo(imageType, imageIndex)?.Path;

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
                var chapter = ItemRepository.GetChapter(this, imageIndex);

                if (chapter == null)
                {
                    return null;
                }

                var path = chapter.ImagePath;

                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                return new ItemImageInfo
                {
                    Path = path,
                    DateModified = chapter.ImageDateModified,
                    Type = imageType
                };
            }

            return GetImages(imageType)
                .ElementAtOrDefault(imageIndex);
        }

        /// <summary>
        /// Computes image index for given image or raises if no matching image found.
        /// </summary>
        /// <param name="image">Image to compute index for.</param>
        /// <exception cref="ArgumentException">Image index cannot be computed as no matching image found.
        /// </exception>
        /// <returns>Image index.</returns>
        public int GetImageIndex(ItemImageInfo image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            if (image.Type == ImageType.Chapter)
            {
                var chapters = ItemRepository.GetChapters(this);
                for (var i = 0; i < chapters.Count; i++)
                {
                    if (chapters[i].ImagePath == image.Path)
                    {
                        return i;
                    }
                }

                throw new ArgumentException("No chapter index found for image path", image.Path);
            }

            var images = GetImages(image.Type).ToArray();
            for (var i = 0; i < images.Length; i++)
            {
                if (images[i].Path == image.Path)
                {
                    return i;
                }
            }

            throw new ArgumentException("No image index found for image path", image.Path);
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
        /// <exception cref="ArgumentException">Cannot call AddImages with chapter images</exception>
        public bool AddImages(ImageType imageType, List<FileSystemMetadata> images)
        {
            if (imageType == ImageType.Chapter)
            {
                throw new ArgumentException("Cannot call AddImages with chapter images");
            }

            var existingImages = GetImages(imageType)
                .ToList();

            var newImageList = new List<FileSystemMetadata>();
            var imageAdded = false;
            var imageUpdated = false;

            foreach (var newImage in images)
            {
                if (newImage == null)
                {
                    throw new ArgumentException("null image found in list");
                }

                var existing = existingImages
                    .FirstOrDefault(i => string.Equals(i.Path, newImage.FullName, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    newImageList.Add(newImage);
                    imageAdded = true;
                }
                else
                {
                    if (existing.IsLocalFile)
                    {
                        var newDateModified = FileSystem.GetLastWriteTimeUtc(newImage);

                        // If date changed then we need to reset saved image dimensions
                        if (existing.DateModified != newDateModified && (existing.Width > 0 || existing.Height > 0))
                        {
                            existing.Width = 0;
                            existing.Height = 0;
                            imageUpdated = true;
                        }

                        existing.DateModified = newDateModified;
                    }
                }
            }

            if (imageAdded || images.Count != existingImages.Count)
            {
                var newImagePaths = images.Select(i => i.FullName).ToList();

                var deleted = existingImages
                    .Where(i => i.IsLocalFile && !newImagePaths.Contains(i.Path, StringComparer.OrdinalIgnoreCase) && !File.Exists(i.Path))
                    .ToList();

                if (deleted.Count > 0)
                {
                    ImageInfos = ImageInfos.Except(deleted).ToArray();
                }
            }

            if (newImageList.Count > 0)
            {
                ImageInfos = ImageInfos.Concat(newImageList.Select(i => GetImageInfo(i, imageType))).ToArray();
            }

            return imageUpdated || newImageList.Count > 0;
        }

        private ItemImageInfo GetImageInfo(FileSystemMetadata file, ImageType type)
        {
            return new ItemImageInfo
            {
                Path = file.FullName,
                Type = type,
                DateModified = FileSystem.GetLastWriteTimeUtc(file)
            };
        }

        /// <summary>
        /// Gets the file system path to delete when the item is to be deleted
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<FileSystemMetadata> GetDeletePaths()
        {
            return new[] {
                new FileSystemMetadata
                {
                    FullName = Path,
                    IsDirectory = IsFolder
                }
            }.Concat(GetLocalMetadataFilesToDelete());
        }

        protected List<FileSystemMetadata> GetLocalMetadataFilesToDelete()
        {
            if (IsFolder || !IsInMixedFolder)
            {
                return new List<FileSystemMetadata>();
            }

            var filename = System.IO.Path.GetFileNameWithoutExtension(Path);

            return FileSystem.GetFiles(System.IO.Path.GetDirectoryName(Path), _supportedExtensions, false, false)
                .Where(i => System.IO.Path.GetFileNameWithoutExtension(i.FullName).StartsWith(filename, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        public bool AllowsMultipleImages(ImageType type)
        {
            return type == ImageType.Backdrop || type == ImageType.Screenshot || type == ImageType.Chapter;
        }

        public void SwapImages(ImageType type, int index1, int index2)
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
                return;
            }

            if (!info1.IsLocalFile || !info2.IsLocalFile)
            {
                // TODO: Not supported  yet
                return;
            }

            var path1 = info1.Path;
            var path2 = info2.Path;

            FileSystem.SwapFiles(path1, path2);

            // Refresh these values
            info1.DateModified = FileSystem.GetLastWriteTimeUtc(info1.Path);
            info2.DateModified = FileSystem.GetLastWriteTimeUtc(info2.Path);

            info1.Width = 0;
            info1.Height = 0;
            info2.Width = 0;
            info2.Height = 0;

            UpdateToRepository(ItemUpdateType.ImageUpdate, CancellationToken.None);
        }

        public virtual bool IsPlayed(User user)
        {
            var userdata = UserDataManager.GetUserData(user, this);

            return userdata != null && userdata.Played;
        }

        public bool IsFavoriteOrLiked(User user)
        {
            var userdata = UserDataManager.GetUserData(user, this);

            return userdata != null && (userdata.IsFavorite || (userdata.Likes ?? false));
        }

        public virtual bool IsUnplayed(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var userdata = UserDataManager.GetUserData(user, this);

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
                Name = GetNameForMetadataLookup(),
                ProviderIds = ProviderIds,
                IndexNumber = IndexNumber,
                ParentIndexNumber = ParentIndexNumber,
                Year = ProductionYear,
                PremiereDate = PremiereDate
            };
        }

        protected virtual string GetNameForMetadataLookup()
        {
            return Name;
        }

        /// <summary>
        /// This is called before any metadata refresh and returns true if changes were made.
        /// </summary>
        public virtual bool BeforeMetadataRefresh(bool replaceAllMetdata)
        {
            _sortName = null;

            var hasChanges = false;

            if (string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Path))
            {
                Name = System.IO.Path.GetFileNameWithoutExtension(Path);
                hasChanges = true;
            }

            return hasChanges;
        }

        protected static string GetMappedPath(BaseItem item, string path, MediaProtocol? protocol)
        {
            if (protocol.HasValue && protocol.Value == MediaProtocol.File)
            {
                return LibraryManager.GetPathAfterNetworkSubstitution(path, item);
            }

            return path;
        }

        public virtual void FillUserDataDtoValues(UserItemDataDto dto, UserItemData userData, BaseItemDto itemDto, User user, DtoOptions fields)
        {
            if (RunTimeTicks.HasValue)
            {
                double pct = RunTimeTicks.Value;

                if (pct > 0)
                {
                    pct = userData.PlaybackPositionTicks / pct;

                    if (pct > 0)
                    {
                        dto.PlayedPercentage = 100 * pct;
                    }
                }
            }
        }

        protected Task RefreshMetadataForOwnedItem(BaseItem ownedItem, bool copyTitleMetadata, MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var newOptions = new MetadataRefreshOptions(options);
            newOptions.SearchResult = null;

            var item = this;

            if (copyTitleMetadata)
            {
                // Take some data from the main item, for querying purposes
                if (!item.Genres.SequenceEqual(ownedItem.Genres, StringComparer.Ordinal))
                {
                    newOptions.ForceSave = true;
                    ownedItem.Genres = item.Genres;
                }

                if (!item.Studios.SequenceEqual(ownedItem.Studios, StringComparer.Ordinal))
                {
                    newOptions.ForceSave = true;
                    ownedItem.Studios = item.Studios;
                }

                if (!item.ProductionLocations.SequenceEqual(ownedItem.ProductionLocations, StringComparer.Ordinal))
                {
                    newOptions.ForceSave = true;
                    ownedItem.ProductionLocations = item.ProductionLocations;
                }

                if (item.CommunityRating != ownedItem.CommunityRating)
                {
                    ownedItem.CommunityRating = item.CommunityRating;
                    newOptions.ForceSave = true;
                }

                if (item.CriticRating != ownedItem.CriticRating)
                {
                    ownedItem.CriticRating = item.CriticRating;
                    newOptions.ForceSave = true;
                }

                if (!string.Equals(item.Overview, ownedItem.Overview, StringComparison.Ordinal))
                {
                    ownedItem.Overview = item.Overview;
                    newOptions.ForceSave = true;
                }

                if (!string.Equals(item.OfficialRating, ownedItem.OfficialRating, StringComparison.Ordinal))
                {
                    ownedItem.OfficialRating = item.OfficialRating;
                    newOptions.ForceSave = true;
                }

                if (!string.Equals(item.CustomRating, ownedItem.CustomRating, StringComparison.Ordinal))
                {
                    ownedItem.CustomRating = item.CustomRating;
                    newOptions.ForceSave = true;
                }
            }

            return ownedItem.RefreshMetadata(newOptions, cancellationToken);
        }

        protected Task RefreshMetadataForOwnedVideo(MetadataRefreshOptions options, bool copyTitleMetadata, string path, CancellationToken cancellationToken)
        {
            var newOptions = new MetadataRefreshOptions(options);
            newOptions.SearchResult = null;

            var id = LibraryManager.GetNewItemId(path, typeof(Video));

            // Try to retrieve it from the db. If we don't find it, use the resolved version
            var video = LibraryManager.GetItemById(id) as Video;

            if (video == null)
            {
                video = LibraryManager.ResolvePath(FileSystem.GetFileSystemInfo(path)) as Video;

                newOptions.ForceSave = true;
            }

            //var parentId = Id;
            //if (!video.IsOwnedItem || video.ParentId != parentId)
            //{
            //    video.IsOwnedItem = true;
            //    video.ParentId = parentId;
            //    newOptions.ForceSave = true;
            //}

            if (video == null)
            {
                return Task.FromResult(true);
            }

            return RefreshMetadataForOwnedItem(video, copyTitleMetadata, newOptions, cancellationToken);
        }

        public string GetEtag(User user)
        {
            var list = GetEtagValues(user);

            return string.Join("|", list).GetMD5().ToString("N", CultureInfo.InvariantCulture);
        }

        protected virtual List<string> GetEtagValues(User user)
        {
            return new List<string>
            {
                DateLastSaved.Ticks.ToString(CultureInfo.InvariantCulture)
            };
        }

        public virtual IEnumerable<Guid> GetAncestorIds()
        {
            return GetParents().Select(i => i.Id).Concat(LibraryManager.GetCollectionFolders(this).Select(i => i.Id));
        }

        public BaseItem GetTopParent()
        {
            if (IsTopParent)
            {
                return this;
            }

            foreach (var parent in GetParents())
            {
                if (parent.IsTopParent)
                {
                    return parent;
                }
            }
            return null;
        }

        [JsonIgnore]
        public virtual bool IsTopParent
        {
            get
            {
                if (this is BasePluginFolder || this is Channel)
                {
                    return true;
                }

                if (this is IHasCollectionType view)
                {
                    if (string.Equals(view.CollectionType, CollectionType.LiveTv, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                if (GetParent() is AggregateFolder)
                {
                    return true;
                }

                return false;
            }
        }

        [JsonIgnore]
        public virtual bool SupportsAncestors => true;

        [JsonIgnore]
        public virtual bool StopRefreshIfLocalMetadataFound => true;

        public virtual IEnumerable<Guid> GetIdsForAncestorQuery()
        {
            return new[] { Id };
        }

        public virtual List<ExternalUrl> GetRelatedUrls()
        {
            return new List<ExternalUrl>();
        }

        public virtual double? GetRefreshProgress()
        {
            return null;
        }

        public virtual ItemUpdateType OnMetadataChanged()
        {
            var updateType = ItemUpdateType.None;

            var item = this;

            var inheritedParentalRatingValue = item.GetInheritedParentalRatingValue() ?? 0;
            if (inheritedParentalRatingValue != item.InheritedParentalRatingValue)
            {
                item.InheritedParentalRatingValue = inheritedParentalRatingValue;
                updateType |= ItemUpdateType.MetadataImport;
            }

            return updateType;
        }

        /// <summary>
        /// Updates the official rating based on content and returns true or false indicating if it changed.
        /// </summary>
        /// <returns></returns>
        public bool UpdateRatingToItems(IList<BaseItem> children)
        {
            var currentOfficialRating = OfficialRating;

            // Gather all possible ratings
            var ratings = children
                .Select(i => i.OfficialRating)
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(i => new Tuple<string, int?>(i, LocalizationManager.GetRatingLevel(i)))
                .OrderBy(i => i.Item2 ?? 1000)
                .Select(i => i.Item1);

            OfficialRating = ratings.FirstOrDefault() ?? currentOfficialRating;

            return !string.Equals(currentOfficialRating ?? string.Empty, OfficialRating ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        public IEnumerable<BaseItem> GetThemeSongs()
        {
            return ThemeVideoIds.Select(LibraryManager.GetItemById).Where(i => i.ExtraType.Equals(Model.Entities.ExtraType.ThemeSong)).OrderBy(i => i.SortName);
        }

        public IEnumerable<BaseItem> GetThemeVideos()
        {
            return ThemeVideoIds.Select(LibraryManager.GetItemById).Where(i => i.ExtraType.Equals(Model.Entities.ExtraType.ThemeVideo)).OrderBy(i => i.SortName);
        }

        /// <summary>
        /// Gets or sets the remote trailers.
        /// </summary>
        /// <value>The remote trailers.</value>
        public IReadOnlyList<MediaUrl> RemoteTrailers { get; set; }

        /// <summary>
        /// Get all extras associated with this item, sorted by <see cref="SortName"/>.
        /// </summary>
        /// <returns>An enumerable containing the items.</returns>
        public IEnumerable<BaseItem> GetExtras()
        {
            return ExtraIds
                .Select(LibraryManager.GetItemById)
                .Where(i => i != null)
                .OrderBy(i => i.SortName);
        }

        /// <summary>
        /// Get all extras with specific types that are associated with this item.
        /// </summary>
        /// <param name="extraTypes">The types of extras to retrieve.</param>
        /// <returns>An enumerable containing the extras.</returns>
        public IEnumerable<BaseItem> GetExtras(IReadOnlyCollection<ExtraType> extraTypes)
        {
            return ExtraIds
                .Select(LibraryManager.GetItemById)
                .Where(i => i != null)
                .Where(i => i.ExtraType.HasValue && extraTypes.Contains(i.ExtraType.Value));
        }

        public IEnumerable<BaseItem> GetTrailers()
        {
            if (this is IHasTrailers)
                return ((IHasTrailers)this).LocalTrailerIds.Select(LibraryManager.GetItemById).Where(i => i != null).OrderBy(i => i.SortName);
            else
                return Array.Empty<BaseItem>();
        }

        public virtual bool IsHD => Height >= 720;

        public bool IsShortcut { get; set; }

        public string ShortcutPath { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public Guid[] ExtraIds { get; set; }

        public virtual long GetRunTimeTicksForPlayState()
        {
            return RunTimeTicks ?? 0;
        }

        /// <summary>
        /// Extra types that should be counted and displayed as "Special Features" in the UI.
        /// </summary>
        public static readonly IReadOnlyCollection<ExtraType> DisplayExtraTypes = new HashSet<ExtraType>
        {
            Model.Entities.ExtraType.Unknown,
            Model.Entities.ExtraType.BehindTheScenes,
            Model.Entities.ExtraType.Clip,
            Model.Entities.ExtraType.DeletedScene,
            Model.Entities.ExtraType.Interview,
            Model.Entities.ExtraType.Sample,
            Model.Entities.ExtraType.Scene
        };

        public virtual bool SupportsExternalTransfer => false;

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is BaseItem baseItem && this.Equals(baseItem);
        }

        /// <inheritdoc />
        public bool Equals(BaseItem item) => Object.Equals(Id, item?.Id);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Id);
    }
}
