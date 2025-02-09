#nullable disable

#pragma warning disable CS1591, SA1401

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class BaseItem.
    /// </summary>
    public abstract class BaseItem : IHasProviderIds, IHasLookupInfo<ItemLookupInfo>, IEquatable<BaseItem>
    {
        private BaseItemKind? _baseItemKind;

        public const string ThemeSongFileName = "theme";

        /// <summary>
        /// The supported image extensions.
        /// </summary>
        public static readonly string[] SupportedImageExtensions
            = new[] { ".png", ".jpg", ".jpeg", ".webp", ".tbn", ".gif", ".svg" };

        private static readonly List<string> _supportedExtensions = new List<string>(SupportedImageExtensions)
        {
            ".nfo",
            ".xml",
            ".srt",
            ".vtt",
            ".sub",
            ".sup",
            ".idx",
            ".txt",
            ".edl",
            ".bif",
            ".smi",
            ".ttml",
            ".lrc",
            ".elrc"
        };

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
            Model.Entities.ExtraType.Scene,
            Model.Entities.ExtraType.Featurette,
            Model.Entities.ExtraType.Short
        };

        private string _sortName;

        private string _forcedSortName;

        private string _name;

        public const char SlugChar = '-';

        protected BaseItem()
        {
            Tags = Array.Empty<string>();
            Genres = Array.Empty<string>();
            Studios = Array.Empty<string>();
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            LockedFields = Array.Empty<MetadataField>();
            ImageInfos = Array.Empty<ItemImageInfo>();
            ProductionLocations = Array.Empty<string>();
            RemoteTrailers = Array.Empty<MediaUrl>();
            ExtraIds = Array.Empty<Guid>();
        }

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
        /// Gets or sets the LUFS value.
        /// </summary>
        /// <value>The LUFS Value.</value>
        [JsonIgnore]
        public float? LUFS { get; set; }

        /// <summary>
        /// Gets or sets the gain required for audio normalization.
        /// </summary>
        /// <value>The gain required for audio normalization.</value>
        [JsonIgnore]
        public float? NormalizationGain { get; set; }

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
        /// Gets or sets a value indicating whether this instance is in mixed folder.
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
        /// Gets the id that should be used to key display prefs for this item.
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
                if (!ChannelId.IsEmpty())
                {
                    return SourceType.Channel;
                }

                return SourceType.Library;
            }
        }

        /// <summary>
        /// Gets the folder containing the item.
        /// If the item is a folder, it returns the folder itself.
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
        /// Gets or sets the external id.
        /// </summary>
        /// <remarks>
        /// If this content came from an external service, the id of the content on that service.
        /// </remarks>
        [JsonIgnore]
        public string ExternalId { get; set; }

        [JsonIgnore]
        public string ExternalSeriesId { get; set; }

        [JsonIgnore]
        public virtual bool IsHidden => false;

        /// <summary>
        /// Gets the type of the location.
        /// </summary>
        /// <value>The type of the location.</value>
        [JsonIgnore]
        public virtual LocationType LocationType
        {
            get
            {
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

        [JsonIgnore]
        public bool IsFileProtocol => PathProtocol == MediaProtocol.File;

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

        public virtual bool IsHD => Height >= 720;

        public bool IsShortcut { get; set; }

        public string ShortcutPath { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public Guid[] ExtraIds { get; set; }

        /// <summary>
        /// Gets the primary image path.
        /// </summary>
        /// <remarks>
        /// This is just a helper for convenience.
        /// </remarks>
        /// <value>The primary image path.</value>
        [JsonIgnore]
        public string PrimaryImagePath => this.GetImagePath(ImageType.Primary);

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

        [JsonIgnore]
        public bool IsLocked { get; set; }

        /// <summary>
        /// Gets or sets the locked fields.
        /// </summary>
        /// <value>The locked fields.</value>
        [JsonIgnore]
        public MetadataField[] LockedFields { get; set; }

        /// <summary>
        /// Gets the type of the media.
        /// </summary>
        /// <value>The type of the media.</value>
        [JsonIgnore]
        public virtual MediaType MediaType => MediaType.Unknown;

        [JsonIgnore]
        public virtual string[] PhysicalLocations
        {
            get
            {
                if (!IsFileProtocol)
                {
                    return Array.Empty<string>();
                }

                return new[] { Path };
            }
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
        /// Gets or sets the logger.
        /// </summary>
        public static ILogger<BaseItem> Logger { get; set; }

        public static ILibraryManager LibraryManager { get; set; }

        public static IServerConfigurationManager ConfigurationManager { get; set; }

        public static IProviderManager ProviderManager { get; set; }

        public static ILocalizationManager LocalizationManager { get; set; }

        public static IItemRepository ItemRepository { get; set; }

        public static IFileSystem FileSystem { get; set; }

        public static IUserDataManager UserDataManager { get; set; }

        public static IChannelManager ChannelManager { get; set; }

        public static IMediaSourceManager MediaSourceManager { get; set; }

        public static IMediaSegmentManager MediaSegmentManager { get; set; }

        /// <summary>
        /// Gets or sets the name of the forced sort.
        /// </summary>
        /// <value>The name of the forced sort.</value>
        [JsonIgnore]
        public string ForcedSortName
        {
            get => _forcedSortName;
            set
            {
                _forcedSortName = value;
                _sortName = null;
            }
        }

        /// <summary>
        /// Gets or sets the name of the sort.
        /// </summary>
        /// <value>The name of the sort.</value>
        [JsonIgnore]
        public string SortName
        {
            get
            {
                if (_sortName is null)
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

        [JsonIgnore]
        public virtual Guid DisplayParentId => ParentId;

        [JsonIgnore]
        public BaseItem DisplayParent
        {
            get
            {
                var id = DisplayParentId;
                if (id.IsEmpty())
                {
                    return null;
                }

                return LibraryManager.GetItemById(id);
            }
        }

        /// <summary>
        /// Gets or sets the date that the item first debuted. For movies this could be premiere date, episodes would be first aired.
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
        public int? InheritedParentalRatingValue { get; set; }

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
        /// Gets or sets the index number. If the item is part of a series, this is it's number in the series.
        /// This could be episode number, album track number, etc.
        /// </summary>
        /// <value>The index number.</value>
        [JsonIgnore]
        public int? IndexNumber { get; set; }

        /// <summary>
        /// Gets or sets the parent index number. For an episode this could be the season number, or for a song this could be the disc number.
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
                if (parent is not null)
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
                if (parent is not null)
                {
                    return parent.CustomRatingForComparison;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets or sets the provider ids.
        /// </summary>
        /// <value>The provider ids.</value>
        [JsonIgnore]
        public Dictionary<string, string> ProviderIds { get; set; }

        [JsonIgnore]
        public virtual Folder LatestItemsIndexContainer => null;

        [JsonIgnore]
        public string PresentationUniqueKey { get; set; }

        [JsonIgnore]
        public virtual bool EnableRememberingTrackSelections => true;

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
                    if (view.CollectionType == CollectionType.livetv)
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
        protected virtual bool SupportsOwnedItems => !ParentId.IsEmpty() && IsFileProtocol;

        [JsonIgnore]
        public virtual bool SupportsPeople => false;

        [JsonIgnore]
        public virtual bool SupportsThemeMedia => false;

        [JsonIgnore]
        public virtual bool SupportsInheritedParentImages => false;

        /// <summary>
        /// Gets a value indicating whether this instance is folder.
        /// </summary>
        /// <value><c>true</c> if this instance is folder; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public virtual bool IsFolder => false;

        [JsonIgnore]
        public virtual bool IsDisplayedAsFolder => false;

        /// <summary>
        /// Gets or sets the remote trailers.
        /// </summary>
        /// <value>The remote trailers.</value>
        public IReadOnlyList<MediaUrl> RemoteTrailers { get; set; }

        public virtual double GetDefaultPrimaryImageAspectRatio()
        {
            return 0;
        }

        public virtual string CreatePresentationUniqueKey()
        {
            return Id.ToString("N", CultureInfo.InvariantCulture);
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
            if (user.HasPermission(PermissionKind.EnableContentDeletion))
            {
                return true;
            }

            var allowed = user.GetPreferenceValues<Guid>(PreferenceKind.EnableContentDeletionFromFolders);

            if (SourceType == SourceType.Channel)
            {
                return allowed.Contains(ChannelId);
            }

            var collectionFolders = LibraryManager.GetCollectionFolders(this, allCollectionFolders);

            foreach (var folder in collectionFolders)
            {
                if (allowed.Contains(folder.Id))
                {
                    return true;
                }
            }

            return false;
        }

        public BaseItem GetOwner()
        {
            var ownerId = OwnerId;
            return ownerId.IsEmpty() ? null : LibraryManager.GetItemById(ownerId);
        }

        public bool CanDelete(User user, List<Folder> allCollectionFolders)
        {
            return CanDelete() && IsAuthorizedToDelete(user, allCollectionFolders);
        }

        public virtual bool CanDelete(User user)
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
            return user.HasPermission(PermissionKind.EnableContentDownloading);
        }

        public bool CanDownload(User user)
        {
            return CanDownload() && IsAuthorizedToDownload(user);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }

        public virtual string GetInternalMetadataPath()
        {
            var basePath = ConfigurationManager.ApplicationPaths.InternalMetadataPath;

            return GetInternalMetadataPath(basePath);
        }

        protected virtual string GetInternalMetadataPath(string basePath)
        {
            if (SourceType == SourceType.Channel)
            {
                return System.IO.Path.Join(basePath, "channels", ChannelId.ToString("N", CultureInfo.InvariantCulture), Id.ToString("N", CultureInfo.InvariantCulture));
            }

            ReadOnlySpan<char> idString = Id.ToString("N", CultureInfo.InvariantCulture);

            return System.IO.Path.Join(basePath, "library", idString[..2], idString);
        }

        /// <summary>
        /// Creates the name of the sort.
        /// </summary>
        /// <returns>System.String.</returns>
        protected virtual string CreateSortName()
        {
            if (Name is null)
            {
                return null; // some items may not have name filled in properly
            }

            if (!EnableAlphaNumericSorting)
            {
                return Name.TrimStart();
            }

            var sortable = Name.Trim().ToLowerInvariant();

            foreach (var search in ConfigurationManager.Configuration.SortRemoveWords)
            {
                // Remove from beginning if a space follows
                if (sortable.StartsWith(search + " ", StringComparison.Ordinal))
                {
                    sortable = sortable.Remove(0, search.Length + 1);
                }

                // Remove from middle if surrounded by spaces
                sortable = sortable.Replace(" " + search + " ", " ", StringComparison.Ordinal);

                // Remove from end if followed by a space
                if (sortable.EndsWith(" " + search, StringComparison.Ordinal))
                {
                    sortable = sortable.Remove(sortable.Length - (search.Length + 1));
                }
            }

            foreach (var removeChar in ConfigurationManager.Configuration.SortRemoveCharacters)
            {
                sortable = sortable.Replace(removeChar, string.Empty, StringComparison.Ordinal);
            }

            foreach (var replaceChar in ConfigurationManager.Configuration.SortReplaceCharacters)
            {
                sortable = sortable.Replace(replaceChar, " ", StringComparison.Ordinal);
            }

            return ModifySortChunks(sortable);
        }

        internal static string ModifySortChunks(ReadOnlySpan<char> name)
        {
            static void AppendChunk(StringBuilder builder, bool isDigitChunk, ReadOnlySpan<char> chunk)
            {
                if (isDigitChunk && chunk.Length < 10)
                {
                    builder.Append('0', 10 - chunk.Length);
                }

                builder.Append(chunk);
            }

            if (name.IsEmpty)
            {
                return string.Empty;
            }

            var builder = new StringBuilder(name.Length);

            int chunkStart = 0;
            bool isDigitChunk = char.IsDigit(name[0]);
            for (int i = 0; i < name.Length; i++)
            {
                var isDigit = char.IsDigit(name[i]);
                if (isDigit != isDigitChunk)
                {
                    AppendChunk(builder, isDigitChunk, name.Slice(chunkStart, i - chunkStart));
                    chunkStart = i;
                    isDigitChunk = isDigit;
                }
            }

            AppendChunk(builder, isDigitChunk, name.Slice(chunkStart));

            // logger.LogDebug("ModifySortChunks Start: {0} End: {1}", name, builder.ToString());
            var result = builder.ToString().RemoveDiacritics();
            if (!result.All(char.IsAscii))
            {
                result = result.Transliterated();
            }

            return result;
        }

        public BaseItem GetParent()
        {
            var parentId = ParentId;
            if (parentId.IsEmpty())
            {
                return null;
            }

            return LibraryManager.GetItemById(parentId);
        }

        public IEnumerable<BaseItem> GetParents()
        {
            var parent = GetParent();

            while (parent is not null)
            {
                yield return parent;

                parent = parent.GetParent();
            }
        }

        /// <summary>
        /// Finds a parent of a given type.
        /// </summary>
        /// <typeparam name="T">Type of parent.</typeparam>
        /// <returns>``0.</returns>
        public T FindParent<T>()
            where T : Folder
        {
            foreach (var parent in GetParents())
            {
                if (parent is T item)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the play access.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>PlayAccess.</returns>
        public PlayAccess GetPlayAccess(User user)
        {
            if (!user.HasPermission(PermissionKind.EnableMediaPlayback))
            {
                return PlayAccess.None;
            }

            // if (!user.IsParentalScheduleAllowed())
            // {
            //    return PlayAccess.None;
            // }

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
            var result = list.Select(i => GetVersionInfo(enablePathSubstitution, i.Item, i.MediaSourceType)).ToList();

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
            .ThenByDescending(i => i, new MediaSourceWidthComparator())
            .ToList();
        }

        protected virtual IEnumerable<(BaseItem Item, MediaSourceType MediaSourceType)> GetAllItemsForMediaSources()
        {
            return Enumerable.Empty<(BaseItem, MediaSourceType)>();
        }

        private MediaSourceInfo GetVersionInfo(bool enablePathSubstitution, BaseItem item, MediaSourceType type)
        {
            ArgumentNullException.ThrowIfNull(item);

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
                Type = type,
                HasSegments = MediaSegmentManager.IsTypeSupported(item)
                    && (protocol is null or MediaProtocol.File)
                    && MediaSegmentManager.HasSegments(item.Id)
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
            if (video is not null)
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

            if (video is not null && video.VideoType != VideoType.VideoFile)
            {
                info.SupportsDirectStream = false;
            }

            info.Bitrate = item.TotalBitrate;
            info.InferTotalBitrate();

            return info;
        }

        internal string GetMediaSourceName(BaseItem item)
        {
            var terms = new List<string>();

            var path = item.Path;
            if (item.IsFileProtocol && !string.IsNullOrEmpty(path))
            {
                var displayName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (HasLocalAlternateVersions)
                {
                    var containingFolderName = System.IO.Path.GetFileName(ContainingFolderPath);
                    if (displayName.Length > containingFolderName.Length && displayName.StartsWith(containingFolderName, StringComparison.OrdinalIgnoreCase))
                    {
                        var name = displayName.AsSpan(containingFolderName.Length).TrimStart([' ', '-']);
                        if (!name.IsWhiteSpace())
                        {
                            terms.Add(name.ToString());
                        }
                    }
                }

                if (terms.Count == 0)
                {
                    terms.Add(displayName);
                }
            }

            if (terms.Count == 0)
            {
                terms.Add(item.Name);
            }

            if (item is Video video)
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
                        if (video.IsoType.Value == IsoType.BluRay)
                        {
                            terms.Add("Bluray");
                        }
                        else if (video.IsoType.Value == IsoType.Dvd)
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

            return string.Join('/', terms);
        }

        public Task RefreshMetadata(CancellationToken cancellationToken)
        {
            return RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(FileSystem)), cancellationToken);
        }

        /// <summary>
        /// Overrides the base implementation to refresh metadata for local trailers.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>true if a provider reports we changed.</returns>
        public async Task<ItemUpdateType> RefreshMetadata(MetadataRefreshOptions options, CancellationToken cancellationToken)
        {
            var requiresSave = false;

            if (SupportsOwnedItems)
            {
                try
                {
                    if (IsFileProtocol)
                    {
                        requiresSave = await RefreshedOwnedItems(options, GetFileSystemChildren(options.DirectoryService), cancellationToken).ConfigureAwait(false);
                    }

                    await LibraryManager.UpdateImagesAsync(this).ConfigureAwait(false); // ensure all image properties in DB are fresh
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error refreshing owned items for {Path}", Path ?? Name);
                }
            }

            var refreshOptions = requiresSave
                ? new MetadataRefreshOptions(options)
                {
                    ForceSave = true
                }
                : options;

            return await ProviderManager.RefreshSingleItem(this, refreshOptions, cancellationToken).ConfigureAwait(false);
        }

        protected bool IsVisibleStandaloneInternal(User user, bool checkFolders)
        {
            if (!IsVisible(user))
            {
                return false;
            }

            if (GetParents().Any(i => !i.IsVisible(user, true)))
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

        public void SetParent(Folder parent)
        {
            ParentId = parent is null ? Guid.Empty : parent.Id;
        }

        /// <summary>
        /// Refreshes owned items such as trailers, theme videos, special features, etc.
        /// Returns true or false indicating if changes were found.
        /// </summary>
        /// <param name="options">The metadata refresh options.</param>
        /// <param name="fileSystemChildren">The list of filesystem children.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><c>true</c> if any items have changed, else <c>false</c>.</returns>
        protected virtual async Task<bool> RefreshedOwnedItems(MetadataRefreshOptions options, IReadOnlyList<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
        {
            if (!IsFileProtocol || !SupportsOwnedItems || IsInMixedFolder || this is ICollectionFolder or UserRootFolder or AggregateFolder || this.GetType() == typeof(Folder))
            {
                return false;
            }

            return await RefreshExtras(this, options, fileSystemChildren, cancellationToken).ConfigureAwait(false);
        }

        protected virtual FileSystemMetadata[] GetFileSystemChildren(IDirectoryService directoryService)
        {
            var path = ContainingFolderPath;

            return directoryService.GetFileSystemEntries(path);
        }

        private async Task<bool> RefreshExtras(BaseItem item, MetadataRefreshOptions options, IReadOnlyList<FileSystemMetadata> fileSystemChildren, CancellationToken cancellationToken)
        {
            var extras = LibraryManager.FindExtras(item, fileSystemChildren, options.DirectoryService).ToArray();
            var newExtraIds = Array.ConvertAll(extras, x => x.Id);
            var extrasChanged = !item.ExtraIds.SequenceEqual(newExtraIds);

            if (!extrasChanged && !options.ReplaceAllMetadata && options.MetadataRefreshMode != MetadataRefreshMode.FullRefresh)
            {
                return false;
            }

            var ownerId = item.Id;

            var tasks = extras.Select(i =>
            {
                var subOptions = new MetadataRefreshOptions(options);
                if (!i.OwnerId.Equals(ownerId) || !i.ParentId.IsEmpty())
                {
                    i.OwnerId = ownerId;
                    i.ParentId = Guid.Empty;
                    subOptions.ForceSave = true;
                }

                return RefreshMetadataForOwnedItem(i, true, subOptions, cancellationToken);
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);

            item.ExtraIds = newExtraIds;

            return true;
        }

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
        /// Determines if a given user has access to this item.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="skipAllowedTagsCheck">Don't check for allowed tags.</param>
        /// <returns><c>true</c> if [is parental allowed] [the specified user]; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">If user is null.</exception>
        public bool IsParentalAllowed(User user, bool skipAllowedTagsCheck)
        {
            ArgumentNullException.ThrowIfNull(user);

            if (!IsVisibleViaTags(user, skipAllowedTagsCheck))
            {
                return false;
            }

            var maxAllowedRating = user.MaxParentalAgeRating;
            var rating = CustomRatingForComparison;

            if (string.IsNullOrEmpty(rating))
            {
                rating = OfficialRatingForComparison;
            }

            if (string.IsNullOrEmpty(rating))
            {
                Logger.LogDebug("{0} has no parental rating set.", Name);
                return !GetBlockUnratedValue(user);
            }

            var value = LocalizationManager.GetRatingLevel(rating);

            // Could not determine rating level
            if (!value.HasValue)
            {
                var isAllowed = !GetBlockUnratedValue(user);

                if (!isAllowed)
                {
                    Logger.LogDebug("{0} has an unrecognized parental rating of {1}.", Name, rating);
                }

                return isAllowed;
            }

            return !maxAllowedRating.HasValue || value.Value <= maxAllowedRating.Value;
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

            foreach (var folder in LibraryManager.GetCollectionFolders(this))
            {
                list.AddRange(folder.Tags);
            }

            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private bool IsVisibleViaTags(User user, bool skipAllowedTagsCheck)
        {
            var allTags = GetInheritedTags();
            if (user.GetPreference(PreferenceKind.BlockedTags).Any(i => allTags.Contains(i, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            var parent = GetParents().FirstOrDefault() ?? this;
            if (parent is UserRootFolder or AggregateFolder or UserView)
            {
                return true;
            }

            var allowedTagsPreference = user.GetPreference(PreferenceKind.AllowedTags);
            if (!skipAllowedTagsCheck && allowedTagsPreference.Length != 0 && !allowedTagsPreference.Any(i => allTags.Contains(i, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

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
        /// Gets a bool indicating if access to the unrated item is blocked or not.
        /// </summary>
        /// <param name="user">The configuration.</param>
        /// <returns><c>true</c> if blocked, <c>false</c> otherwise.</returns>
        protected virtual bool GetBlockUnratedValue(User user)
        {
            // Don't block plain folders that are unrated. Let the media underneath get blocked
            // Special folders like series and albums will override this method.
            if (IsFolder || this is IItemByName)
            {
                return false;
            }

            return user.GetPreferenceValues<UnratedItem>(PreferenceKind.BlockUnratedItems).Contains(GetBlockUnratedType());
        }

        /// <summary>
        /// Determines if this folder should be visible to a given user.
        /// Default is just parental allowed. Can be overridden for more functionality.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="skipAllowedTagsCheck">Don't check for allowed tags.</param>
        /// <returns><c>true</c> if the specified user is visible; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="user" /> is <c>null</c>.</exception>
        public virtual bool IsVisible(User user, bool skipAllowedTagsCheck = false)
        {
            ArgumentNullException.ThrowIfNull(user);

            return IsParentalAllowed(user, skipAllowedTagsCheck);
        }

        public virtual bool IsVisibleStandalone(User user)
        {
            if (SourceType == SourceType.Channel)
            {
                return IsVisibleStandaloneInternal(user, false) && Channel.IsChannelVisible(this, user);
            }

            return IsVisibleStandaloneInternal(user, true);
        }

        public virtual string GetClientTypeName()
        {
            if (IsFolder && SourceType == SourceType.Channel && this is not Channel)
            {
                return "ChannelFolderItem";
            }

            return GetType().Name;
        }

        public BaseItemKind GetBaseItemKind()
        {
            return _baseItemKind ??= Enum.Parse<BaseItemKind>(GetClientTypeName());
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
                if (info.ItemId.Value.IsEmpty())
                {
                    return null;
                }

                var itemById = LibraryManager.GetItemById(info.ItemId.Value);

                if (itemById is not null)
                {
                    return itemById;
                }
            }

            var item = FindLinkedChild(info);

            // If still null, log
            if (item is null)
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

                if (itemByPath is null)
                {
                    Logger.LogWarning("Unable to find linked item at path {0}", info.Path);
                }

                return itemByPath;
            }

            if (!string.IsNullOrEmpty(info.LibraryItemId))
            {
                var item = LibraryManager.GetItemById(info.LibraryItemId);

                if (item is null)
                {
                    Logger.LogWarning("Unable to find linked item at path {0}", info.Path);
                }

                return item;
            }

            return null;
        }

        /// <summary>
        /// Adds a studio to the item.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="ArgumentNullException">Throws if name is null.</exception>
        public void AddStudio(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            var current = Studios;

            if (!current.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                int curLen = current.Length;
                if (curLen == 0)
                {
                    Studios = [name];
                }
                else
                {
                    Studios = [..current, name];
                }
            }
        }

        public void SetStudios(IEnumerable<string> names)
        {
            Studios = names.Distinct().ToArray();
        }

        /// <summary>
        /// Adds a genre to the item.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="ArgumentNullException">Throwns if name is null.</exception>
        public void AddGenre(string name)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            var genres = Genres;
            if (!genres.Contains(name, StringComparison.OrdinalIgnoreCase))
            {
                Genres = [..genres, name];
            }
        }

        /// <summary>
        /// Marks the played.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="datePlayed">The date played.</param>
        /// <param name="resetPosition">if set to <c>true</c> [reset position].</param>
        /// <exception cref="ArgumentNullException">Throws if user is null.</exception>
        public virtual void MarkPlayed(
            User user,
            DateTime? datePlayed,
            bool resetPosition)
        {
            ArgumentNullException.ThrowIfNull(user);

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

            UserDataManager.SaveUserData(user, this, data, UserDataSaveReason.TogglePlayed, CancellationToken.None);
        }

        /// <summary>
        /// Marks the unplayed.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <exception cref="ArgumentNullException">Throws if user is null.</exception>
        public virtual void MarkUnplayed(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var data = UserDataManager.GetUserData(user, this);

            // I think it is okay to do this here.
            // if this is only called when a user is manually forcing something to un-played
            // then it probably is what we want to do...
            data.PlayCount = 0;
            data.PlaybackPositionTicks = 0;
            data.LastPlayedDate = null;
            data.Played = false;

            UserDataManager.SaveUserData(user, this, data, UserDataSaveReason.TogglePlayed, CancellationToken.None);
        }

        /// <summary>
        /// Do whatever refreshing is necessary when the filesystem pertaining to this item has changed.
        /// </summary>
        public virtual void ChangedExternally()
        {
            ProviderManager.QueueRefresh(Id, new MetadataRefreshOptions(new DirectoryService(FileSystem)), RefreshPriority.High);
        }

        /// <summary>
        /// Gets an image.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns><c>true</c> if the specified type has image; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentException">Backdrops should be accessed using Item.Backdrops.</exception>
        public bool HasImage(ImageType type, int imageIndex)
        {
            return GetImageInfo(type, imageIndex) is not null;
        }

        public void SetImage(ItemImageInfo image, int index)
        {
            if (image.Type == ImageType.Chapter)
            {
                throw new ArgumentException("Cannot set chapter images using SetImagePath");
            }

            var existingImage = GetImageInfo(image.Type, index);

            if (existingImage is null)
            {
                AddImage(image);
            }
            else
            {
                existingImage.Path = image.Path;
                existingImage.DateModified = image.DateModified;
                existingImage.Width = image.Width;
                existingImage.Height = image.Height;
                existingImage.BlurHash = image.BlurHash;
            }
        }

        public void SetImagePath(ImageType type, int index, FileSystemMetadata file)
        {
            if (type == ImageType.Chapter)
            {
                throw new ArgumentException("Cannot set chapter images using SetImagePath");
            }

            var image = GetImageInfo(type, index);

            if (image is null)
            {
                AddImage(GetImageInfo(file, type));
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
        /// <returns>A task.</returns>
        public async Task DeleteImageAsync(ImageType type, int index)
        {
            var info = GetImageInfo(type, index);

            if (info is null)
            {
                // Nothing to do
                return;
            }

            // Remove from file system
            if (info.IsLocalFile)
            {
                FileSystem.DeleteFile(info.Path);
            }

            // Remove from item
            RemoveImage(info);

            await UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, CancellationToken.None).ConfigureAwait(false);
        }

        public void RemoveImage(ItemImageInfo image)
        {
            RemoveImages(new[] { image });
        }

        public void RemoveImages(IEnumerable<ItemImageInfo> deletedImages)
        {
            ImageInfos = ImageInfos.Except(deletedImages).ToArray();
        }

        public void AddImage(ItemImageInfo image)
        {
            ImageInfos = [..ImageInfos, image];
        }

        public virtual Task UpdateToRepositoryAsync(ItemUpdateType updateReason, CancellationToken cancellationToken)
         => LibraryManager.UpdateItemAsync(this, GetParent(), updateReason, cancellationToken);

        /// <summary>
        /// Validates that images within the item are still on the filesystem.
        /// </summary>
        /// <returns><c>true</c> if the images validate, <c>false</c> if not.</returns>
        public bool ValidateImages()
        {
            List<ItemImageInfo> deletedImages = null;
            foreach (var imageInfo in ImageInfos)
            {
                if (!imageInfo.IsLocalFile)
                {
                    continue;
                }

                if (File.Exists(imageInfo.Path))
                {
                    continue;
                }

                (deletedImages ??= new List<ItemImageInfo>()).Add(imageInfo);
            }

            var anyImagesRemoved = deletedImages?.Count > 0;
            if (anyImagesRemoved)
            {
                RemoveImages(deletedImages);
            }

            return anyImagesRemoved;
        }

        /// <summary>
        /// Gets the image path.
        /// </summary>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="imageIndex">Index of the image.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">Item is null.</exception>
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

                if (chapter is null)
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

            // Music albums usually don't have dedicated backdrops, so return one from the artist instead
            if (GetType() == typeof(MusicAlbum) && imageType == ImageType.Backdrop)
            {
                var artist = FindParent<MusicArtist>();

                if (artist is not null)
                {
                    return artist.GetImages(imageType).ElementAtOrDefault(imageIndex);
                }
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
            ArgumentNullException.ThrowIfNull(image);

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

            // Yield return is more performant than LINQ Where on an Array
            for (var i = 0; i < ImageInfos.Length; i++)
            {
                var imageInfo = ImageInfos[i];
                if (imageInfo.Type == imageType)
                {
                    yield return imageInfo;
                }
            }
        }

        /// <summary>
        /// Adds the images, updating metadata if they already are part of this item.
        /// </summary>
        /// <param name="imageType">Type of the image.</param>
        /// <param name="images">The images.</param>
        /// <returns><c>true</c> if images were added or updated, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentException">Cannot call AddImages with chapter images.</exception>
        public bool AddImages(ImageType imageType, List<FileSystemMetadata> images)
        {
            if (imageType == ImageType.Chapter)
            {
                throw new ArgumentException("Cannot call AddImages with chapter images");
            }

            var existingImages = GetImages(imageType)
                .ToList();

            var newImageList = new List<FileSystemMetadata>();
            var imageUpdated = false;

            foreach (var newImage in images)
            {
                if (newImage is null)
                {
                    throw new ArgumentException("null image found in list");
                }

                var existing = existingImages
                    .Find(i => string.Equals(i.Path, newImage.FullName, StringComparison.OrdinalIgnoreCase));

                if (existing is null)
                {
                    newImageList.Add(newImage);
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
        /// Gets the file system path to delete when the item is to be deleted.
        /// </summary>
        /// <returns>The metadata for the deleted paths.</returns>
        public virtual IEnumerable<FileSystemMetadata> GetDeletePaths()
        {
            return new[]
            {
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
            return type == ImageType.Backdrop || type == ImageType.Chapter;
        }

        public Task SwapImagesAsync(ImageType type, int index1, int index2)
        {
            if (!AllowsMultipleImages(type))
            {
                throw new ArgumentException("The change index operation is only applicable to backdrops and screen shots");
            }

            var info1 = GetImageInfo(type, index1);
            var info2 = GetImageInfo(type, index2);

            if (info1 is null || info2 is null)
            {
                // Nothing to do
                return Task.CompletedTask;
            }

            if (!info1.IsLocalFile || !info2.IsLocalFile)
            {
                // TODO: Not supported  yet
                return Task.CompletedTask;
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

            return UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, CancellationToken.None);
        }

        public virtual bool IsPlayed(User user)
        {
            var userdata = UserDataManager.GetUserData(user, this);

            return userdata is not null && userdata.Played;
        }

        public bool IsFavoriteOrLiked(User user)
        {
            var userdata = UserDataManager.GetUserData(user, this);

            return userdata is not null && (userdata.IsFavorite || (userdata.Likes ?? false));
        }

        public virtual bool IsUnplayed(User user)
        {
            ArgumentNullException.ThrowIfNull(user);

            var userdata = UserDataManager.GetUserData(user, this);

            return userdata is null || !userdata.Played;
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
                Path = Path,
                MetadataCountryCode = GetPreferredMetadataCountryCode(),
                MetadataLanguage = GetPreferredMetadataLanguage(),
                Name = GetNameForMetadataLookup(),
                OriginalTitle = OriginalTitle,
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
        /// <param name="replaceAllMetadata">Whether to replace all metadata.</param>
        /// <returns>true if the item has change, else false.</returns>
        public virtual bool BeforeMetadataRefresh(bool replaceAllMetadata)
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
            if (protocol == MediaProtocol.File)
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
            var newOptions = new MetadataRefreshOptions(options)
            {
                SearchResult = null
            };

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
            var newOptions = new MetadataRefreshOptions(options)
            {
                SearchResult = null
            };

            var id = LibraryManager.GetNewItemId(path, typeof(Video));

            // Try to retrieve it from the db. If we don't find it, use the resolved version
            var video = LibraryManager.GetItemById(id) as Video;

            if (video is null)
            {
                video = LibraryManager.ResolvePath(FileSystem.GetFileSystemInfo(path)) as Video;

                newOptions.ForceSave = true;
            }

            if (video is null)
            {
                return Task.FromResult(true);
            }

            if (video.OwnerId.IsEmpty())
            {
                video.OwnerId = this.Id;
            }

            return RefreshMetadataForOwnedItem(video, copyTitleMetadata, newOptions, cancellationToken);
        }

        public string GetEtag(User user)
        {
            var list = GetEtagValues(user);

            return string.Join('|', list).GetMD5().ToString("N", CultureInfo.InvariantCulture);
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

            return GetParents().FirstOrDefault(parent => parent.IsTopParent);
        }

        public virtual IEnumerable<Guid> GetIdsForAncestorQuery()
        {
            return new[] { Id };
        }

        public virtual double? GetRefreshProgress()
        {
            return null;
        }

        public virtual ItemUpdateType OnMetadataChanged()
        {
            var updateType = ItemUpdateType.None;

            var item = this;

            var inheritedParentalRatingValue = item.GetInheritedParentalRatingValue() ?? null;
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
        /// <param name="children">Media children.</param>
        /// <returns><c>true</c> if the rating was updated; otherwise <c>false</c>.</returns>
        public bool UpdateRatingToItems(IList<BaseItem> children)
        {
            var currentOfficialRating = OfficialRating;

            // Gather all possible ratings
            var ratings = children
                .Select(i => i.OfficialRating)
                .Where(i => !string.IsNullOrEmpty(i))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(rating => (rating, LocalizationManager.GetRatingLevel(rating)))
                .OrderBy(i => i.Item2 ?? 1000)
                .Select(i => i.rating);

            OfficialRating = ratings.FirstOrDefault() ?? currentOfficialRating;

            return !string.Equals(
                currentOfficialRating ?? string.Empty,
                OfficialRating ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        public IReadOnlyList<BaseItem> GetThemeSongs(User user = null)
        {
            return GetThemeSongs(user, Array.Empty<(ItemSortBy, SortOrder)>());
        }

        public IReadOnlyList<BaseItem> GetThemeSongs(User user, IEnumerable<(ItemSortBy SortBy, SortOrder SortOrder)> orderBy)
        {
            return LibraryManager.Sort(GetExtras().Where(e => e.ExtraType == Model.Entities.ExtraType.ThemeSong), user, orderBy).ToArray();
        }

        public IReadOnlyList<BaseItem> GetThemeVideos(User user = null)
        {
            return GetThemeVideos(user, Array.Empty<(ItemSortBy, SortOrder)>());
        }

        public IReadOnlyList<BaseItem> GetThemeVideos(User user, IEnumerable<(ItemSortBy SortBy, SortOrder SortOrder)> orderBy)
        {
            return LibraryManager.Sort(GetExtras().Where(e => e.ExtraType == Model.Entities.ExtraType.ThemeVideo), user, orderBy).ToArray();
        }

        /// <summary>
        /// Get all extras associated with this item, sorted by <see cref="SortName"/>.
        /// </summary>
        /// <returns>An enumerable containing the items.</returns>
        public IEnumerable<BaseItem> GetExtras()
        {
            return ExtraIds
                .Select(LibraryManager.GetItemById)
                .Where(i => i is not null)
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
                .Where(i => i is not null)
                .Where(i => i.ExtraType.HasValue && extraTypes.Contains(i.ExtraType.Value))
                .OrderBy(i => i.SortName);
        }

        public virtual long GetRunTimeTicksForPlayState()
        {
            return RunTimeTicks ?? 0;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is BaseItem baseItem && this.Equals(baseItem);
        }

        /// <inheritdoc />
        public bool Equals(BaseItem other) => other is not null && other.Id.Equals(Id);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(Id);
    }
}
