using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Server.Implementations.Devices;
using MediaBrowser.Server.Implementations.Playlists;
using MediaBrowser.Model.Reflection;
using SQLitePCL.pretty;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Threading;

namespace Emby.Server.Implementations.Data
{
    /// <summary>
    /// Class SQLiteItemRepository
    /// </summary>
    public class SqliteItemRepository : BaseSqliteRepository, IItemRepository
    {
        private readonly TypeMapper _typeMapper;

        /// <summary>
        /// Gets the name of the repository
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return "SQLite";
            }
        }

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IServerConfigurationManager _config;

        private readonly string _criticReviewsPath;

        private readonly IMemoryStreamFactory _memoryStreamProvider;
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentInfo _environmentInfo;
        private readonly ITimerFactory _timerFactory;
        private ITimer _shrinkMemoryTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteItemRepository"/> class.
        /// </summary>
        public SqliteItemRepository(IServerConfigurationManager config, IJsonSerializer jsonSerializer, ILogger logger, IMemoryStreamFactory memoryStreamProvider, IAssemblyInfo assemblyInfo, IFileSystem fileSystem, IEnvironmentInfo environmentInfo, ITimerFactory timerFactory)
            : base(logger)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }

            _config = config;
            _jsonSerializer = jsonSerializer;
            _memoryStreamProvider = memoryStreamProvider;
            _fileSystem = fileSystem;
            _environmentInfo = environmentInfo;
            _timerFactory = timerFactory;
            _typeMapper = new TypeMapper(assemblyInfo);

            _criticReviewsPath = Path.Combine(_config.ApplicationPaths.DataPath, "critic-reviews");
            DbFilePath = Path.Combine(_config.ApplicationPaths.DataPath, "library.db");
        }

        private const string ChaptersTableName = "Chapters2";

        protected override int? CacheSize
        {
            get
            {
                return 20000;
            }
        }

        protected override bool EnableTempStoreMemory
        {
            get
            {
                return true;
            }
        }

        protected override void CloseConnection()
        {
            base.CloseConnection();

            if (_shrinkMemoryTimer != null)
            {
                _shrinkMemoryTimer.Dispose();
                _shrinkMemoryTimer = null;
            }
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize(SqliteUserDataRepository userDataRepo)
        {
            using (var connection = CreateConnection())
            {
                RunDefaultInitialization(connection);

                var createMediaStreamsTableCommand
                   = "create table if not exists mediastreams (ItemId GUID, StreamIndex INT, StreamType TEXT, Codec TEXT, Language TEXT, ChannelLayout TEXT, Profile TEXT, AspectRatio TEXT, Path TEXT, IsInterlaced BIT, BitRate INT NULL, Channels INT NULL, SampleRate INT NULL, IsDefault BIT, IsForced BIT, IsExternal BIT, Height INT NULL, Width INT NULL, AverageFrameRate FLOAT NULL, RealFrameRate FLOAT NULL, Level FLOAT NULL, PixelFormat TEXT, BitDepth INT NULL, IsAnamorphic BIT NULL, RefFrames INT NULL, CodecTag TEXT NULL, Comment TEXT NULL, NalLengthSize TEXT NULL, IsAvc BIT NULL, Title TEXT NULL, TimeBase TEXT NULL, CodecTimeBase TEXT NULL, PRIMARY KEY (ItemId, StreamIndex))";

                string[] queries = {
                                "PRAGMA locking_mode=NORMAL",

                                "create table if not exists TypedBaseItems (guid GUID primary key NOT NULL, type TEXT NOT NULL, data BLOB NULL, ParentId GUID NULL, Path TEXT NULL)",

                                "create table if not exists AncestorIds (ItemId GUID, AncestorId GUID, AncestorIdText TEXT, PRIMARY KEY (ItemId, AncestorId))",
                                "create index if not exists idx_AncestorIds1 on AncestorIds(AncestorId)",
                                "create index if not exists idx_AncestorIds2 on AncestorIds(AncestorIdText)",

                                "create table if not exists ItemValues (ItemId GUID, Type INT, Value TEXT, CleanValue TEXT)",

                                "create table if not exists People (ItemId GUID, Name TEXT NOT NULL, Role TEXT, PersonType TEXT, SortOrder int, ListOrder int)",

                                "drop index if exists idxPeopleItemId",
                                "create index if not exists idxPeopleItemId1 on People(ItemId,ListOrder)",
                                "create index if not exists idxPeopleName on People(Name)",

                                "create table if not exists "+ChaptersTableName+" (ItemId GUID, ChapterIndex INT, StartPositionTicks BIGINT, Name TEXT, ImagePath TEXT, PRIMARY KEY (ItemId, ChapterIndex))",

                                createMediaStreamsTableCommand,

                                "create index if not exists idx_mediastreams1 on mediastreams(ItemId)",

                                "pragma shrink_memory"

                               };

                connection.RunQueries(queries);

                connection.RunInTransaction(db =>
                {
                    var existingColumnNames = GetColumnNames(db, "AncestorIds");
                    AddColumn(db, "AncestorIds", "AncestorIdText", "Text", existingColumnNames);

                    existingColumnNames = GetColumnNames(db, "TypedBaseItems");

                    AddColumn(db, "TypedBaseItems", "Path", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "StartDate", "DATETIME", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "EndDate", "DATETIME", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ChannelId", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsMovie", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsSports", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsKids", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "CommunityRating", "Float", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "CustomRating", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IndexNumber", "INT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsLocked", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Name", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "OfficialRating", "Text", existingColumnNames);

                    AddColumn(db, "TypedBaseItems", "MediaType", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Overview", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ParentIndexNumber", "INT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "PremiereDate", "DATETIME", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ProductionYear", "INT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ParentId", "GUID", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Genres", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "SortName", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "RunTimeTicks", "BIGINT", existingColumnNames);

                    AddColumn(db, "TypedBaseItems", "OfficialRatingDescription", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "HomePageUrl", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "VoteCount", "INT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "DisplayMediaType", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "DateCreated", "DATETIME", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "DateModified", "DATETIME", existingColumnNames);

                    AddColumn(db, "TypedBaseItems", "ForcedSortName", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "LocationType", "Text", existingColumnNames);

                    AddColumn(db, "TypedBaseItems", "IsSeries", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsLive", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsNews", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsPremiere", "BIT", existingColumnNames);

                    AddColumn(db, "TypedBaseItems", "EpisodeTitle", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsRepeat", "BIT", existingColumnNames);

                    AddColumn(db, "TypedBaseItems", "PreferredMetadataLanguage", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "PreferredMetadataCountryCode", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsHD", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ExternalEtag", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "DateLastRefreshed", "DATETIME", existingColumnNames);

                    AddColumn(db, "TypedBaseItems", "DateLastSaved", "DATETIME", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsInMixedFolder", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "LockedFields", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Studios", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Audio", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ExternalServiceId", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Tags", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsFolder", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "InheritedParentalRatingValue", "INT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "UnratedType", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "TopParentId", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsItemByName", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "SourceType", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "TrailerTypes", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "CriticRating", "Float", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "CriticRatingSummary", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "InheritedTags", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "CleanName", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "PresentationUniqueKey", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "SlugName", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "OriginalTitle", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "PrimaryVersionId", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "DateLastMediaAdded", "DATETIME", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Album", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsVirtualItem", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "SeriesName", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "UserDataKey", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "SeasonName", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "SeasonId", "GUID", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "SeriesId", "GUID", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "SeriesSortName", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ExternalSeriesId", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Tagline", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Keywords", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ProviderIds", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Images", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ProductionLocations", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ThemeSongIds", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ThemeVideoIds", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "TotalBitrate", "INT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ExtraType", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Artists", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "AlbumArtists", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ExternalId", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "SeriesPresentationUniqueKey", "Text", existingColumnNames);

                    existingColumnNames = GetColumnNames(db, "ItemValues");
                    AddColumn(db, "ItemValues", "CleanValue", "Text", existingColumnNames);

                    existingColumnNames = GetColumnNames(db, ChaptersTableName);
                    AddColumn(db, ChaptersTableName, "ImageDateModified", "DATETIME", existingColumnNames);

                    existingColumnNames = GetColumnNames(db, "MediaStreams");
                    AddColumn(db, "MediaStreams", "IsAvc", "BIT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "TimeBase", "TEXT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "CodecTimeBase", "TEXT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "Title", "TEXT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "NalLengthSize", "TEXT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "Comment", "TEXT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "CodecTag", "TEXT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "PixelFormat", "TEXT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "BitDepth", "INT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "RefFrames", "INT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "KeyFrames", "TEXT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "IsAnamorphic", "BIT", existingColumnNames);
                }, TransactionMode);

                string[] postQueries =

                                    {
                // obsolete
                "drop index if exists idx_TypedBaseItems",
                "drop index if exists idx_mediastreams",
                "drop index if exists idx_"+ChaptersTableName,
                "drop index if exists idx_UserDataKeys1",
                "drop index if exists idx_UserDataKeys2",
                "drop index if exists idx_TypeTopParentId3",
                "drop index if exists idx_TypeTopParentId2",
                "drop index if exists idx_TypeTopParentId4",
                "drop index if exists idx_Type",
                "drop index if exists idx_TypeTopParentId",
                "drop index if exists idx_GuidType",
                "drop index if exists idx_TopParentId",
                "drop index if exists idx_TypeTopParentId6",
                "drop index if exists idx_ItemValues2",
                "drop index if exists Idx_ProviderIds",
                "drop index if exists idx_ItemValues3",
                "drop index if exists idx_ItemValues4",
                "drop index if exists idx_ItemValues5",
                "drop index if exists idx_UserDataKeys3",
                "drop table if exists UserDataKeys",
                "drop table if exists ProviderIds",
                "drop index if exists Idx_ProviderIds1",
                "drop table if exists Images",
                "drop index if exists idx_Images",
                "drop index if exists idx_TypeSeriesPresentationUniqueKey",
                "drop index if exists idx_SeriesPresentationUniqueKey",
                "drop index if exists idx_TypeSeriesPresentationUniqueKey2",

                "create index if not exists idx_PathTypedBaseItems on TypedBaseItems(Path)",
                "create index if not exists idx_ParentIdTypedBaseItems on TypedBaseItems(ParentId)",

                "create index if not exists idx_PresentationUniqueKey on TypedBaseItems(PresentationUniqueKey)",
                "create index if not exists idx_GuidTypeIsFolderIsVirtualItem on TypedBaseItems(Guid,Type,IsFolder,IsVirtualItem)",
                //"create index if not exists idx_GuidMediaTypeIsFolderIsVirtualItem on TypedBaseItems(Guid,MediaType,IsFolder,IsVirtualItem)",
                "create index if not exists idx_CleanNameType on TypedBaseItems(CleanName,Type)",

                // covering index
                "create index if not exists idx_TopParentIdGuid on TypedBaseItems(TopParentId,Guid)",

                // series
                "create index if not exists idx_TypeSeriesPresentationUniqueKey1 on TypedBaseItems(Type,SeriesPresentationUniqueKey,PresentationUniqueKey,SortName)",

                // series counts
                // seriesdateplayed sort order
                "create index if not exists idx_TypeSeriesPresentationUniqueKey3 on TypedBaseItems(SeriesPresentationUniqueKey,Type,IsFolder,IsVirtualItem)",

                // live tv programs
                "create index if not exists idx_TypeTopParentIdStartDate on TypedBaseItems(Type,TopParentId,StartDate)",

                // covering index for getitemvalues
                "create index if not exists idx_TypeTopParentIdGuid on TypedBaseItems(Type,TopParentId,Guid)",

                // used by movie suggestions
                "create index if not exists idx_TypeTopParentIdGroup on TypedBaseItems(Type,TopParentId,PresentationUniqueKey)",
                "create index if not exists idx_TypeTopParentId5 on TypedBaseItems(TopParentId,IsVirtualItem)",

                // latest items
                "create index if not exists idx_TypeTopParentId9 on TypedBaseItems(TopParentId,Type,IsVirtualItem,PresentationUniqueKey,DateCreated)",
                "create index if not exists idx_TypeTopParentId8 on TypedBaseItems(TopParentId,IsFolder,IsVirtualItem,PresentationUniqueKey,DateCreated)",

                // resume
                "create index if not exists idx_TypeTopParentId7 on TypedBaseItems(TopParentId,MediaType,IsVirtualItem,PresentationUniqueKey)",

                // items by name
                "create index if not exists idx_ItemValues6 on ItemValues(ItemId,Type,CleanValue)",
                "create index if not exists idx_ItemValues7 on ItemValues(Type,CleanValue,ItemId)"
                };

                connection.RunQueries(postQueries);

                //await Vacuum(_connection).ConfigureAwait(false);
            }

            userDataRepo.Initialize(WriteLock, _connection);

            _shrinkMemoryTimer = _timerFactory.Create(OnShrinkMemoryTimerCallback, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(30));
        }

        private void OnShrinkMemoryTimerCallback(object state)
        {
            try
            {
                using (WriteLock.Write())
                {
                    using (var connection = CreateConnection())
                    {
                        connection.RunQueries(new string[]
                        {
                            "pragma shrink_memory"
                        });
                    }
                }

                GC.Collect();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error running shrink memory", ex);
            }
        }

        private readonly string[] _retriveItemColumns =
        {
            "type",
            "data",
            "StartDate",
            "EndDate",
            "ChannelId",
            "IsMovie",
            "IsSports",
            "IsKids",
            "IsSeries",
            "IsLive",
            "IsNews",
            "IsPremiere",
            "EpisodeTitle",
            "IsRepeat",
            "CommunityRating",
            "CustomRating",
            "IndexNumber",
            "IsLocked",
            "PreferredMetadataLanguage",
            "PreferredMetadataCountryCode",
            "IsHD",
            "ExternalEtag",
            "DateLastRefreshed",
            "Name",
            "Path",
            "PremiereDate",
            "Overview",
            "ParentIndexNumber",
            "ProductionYear",
            "OfficialRating",
            "OfficialRatingDescription",
            "HomePageUrl",
            "DisplayMediaType",
            "ForcedSortName",
            "RunTimeTicks",
            "VoteCount",
            "DateCreated",
            "DateModified",
            "guid",
            "Genres",
            "ParentId",
            "Audio",
            "ExternalServiceId",
            "IsInMixedFolder",
            "DateLastSaved",
            "LockedFields",
            "Studios",
            "Tags",
            "SourceType",
            "TrailerTypes",
            "OriginalTitle",
            "PrimaryVersionId",
            "DateLastMediaAdded",
            "Album",
            "CriticRating",
            "CriticRatingSummary",
            "IsVirtualItem",
            "SeriesName",
            "SeasonName",
            "SeasonId",
            "SeriesId",
            "SeriesSortName",
            "PresentationUniqueKey",
            "InheritedParentalRatingValue",
            "InheritedTags",
            "ExternalSeriesId",
            "Tagline",
            "Keywords",
            "ProviderIds",
            "Images",
            "ProductionLocations",
            "ThemeSongIds",
            "ThemeVideoIds",
            "TotalBitrate",
            "ExtraType",
            "Artists",
            "AlbumArtists",
            "ExternalId",
            "SeriesPresentationUniqueKey"
        };

        private readonly string[] _mediaStreamSaveColumns =
        {
            "ItemId",
            "StreamIndex",
            "StreamType",
            "Codec",
            "Language",
            "ChannelLayout",
            "Profile",
            "AspectRatio",
            "Path",
            "IsInterlaced",
            "BitRate",
            "Channels",
            "SampleRate",
            "IsDefault",
            "IsForced",
            "IsExternal",
            "Height",
            "Width",
            "AverageFrameRate",
            "RealFrameRate",
            "Level",
            "PixelFormat",
            "BitDepth",
            "IsAnamorphic",
            "RefFrames",
            "CodecTag",
            "Comment",
            "NalLengthSize",
            "IsAvc",
            "Title",
            "TimeBase",
            "CodecTimeBase"
        };

        private string GetSaveItemCommandText()
        {
            var saveColumns = new List<string>
            {
                "guid",
                "type",
                "data",
                "Path",
                "StartDate",
                "EndDate",
                "ChannelId",
                "IsKids",
                "IsMovie",
                "IsSports",
                "IsSeries",
                "IsLive",
                "IsNews",
                "IsPremiere",
                "EpisodeTitle",
                "IsRepeat",
                "CommunityRating",
                "CustomRating",
                "IndexNumber",
                "IsLocked",
                "Name",
                "OfficialRating",
                "MediaType",
                "Overview",
                "ParentIndexNumber",
                "PremiereDate",
                "ProductionYear",
                "ParentId",
                "Genres",
                "InheritedParentalRatingValue",
                "SortName",
                "RunTimeTicks",
                "OfficialRatingDescription",
                "HomePageUrl",
                "VoteCount",
                "DisplayMediaType",
                "DateCreated",
                "DateModified",
                "ForcedSortName",
                "LocationType",
                "PreferredMetadataLanguage",
                "PreferredMetadataCountryCode",
                "IsHD",
                "ExternalEtag",
                "DateLastRefreshed",
                "DateLastSaved",
                "IsInMixedFolder",
                "LockedFields",
                "Studios",
                "Audio",
                "ExternalServiceId",
                "Tags",
                "IsFolder",
                "UnratedType",
                "TopParentId",
                "IsItemByName",
                "SourceType",
                "TrailerTypes",
                "CriticRating",
                "CriticRatingSummary",
                "InheritedTags",
                "CleanName",
                "PresentationUniqueKey",
                "SlugName",
                "OriginalTitle",
                "PrimaryVersionId",
                "DateLastMediaAdded",
                "Album",
                "IsVirtualItem",
                "SeriesName",
                "UserDataKey",
                "SeasonName",
                "SeasonId",
                "SeriesId",
                "SeriesSortName",
                "ExternalSeriesId",
                "Tagline",
                "Keywords",
                "ProviderIds",
                "Images",
                "ProductionLocations",
                "ThemeSongIds",
                "ThemeVideoIds",
                "TotalBitrate",
                "ExtraType",
                "Artists",
                "AlbumArtists",
                "ExternalId",
            "SeriesPresentationUniqueKey"
            };

            var saveItemCommandCommandText = "replace into TypedBaseItems (" + string.Join(",", saveColumns.ToArray()) + ") values (";

            for (var i = 0; i < saveColumns.Count; i++)
            {
                if (i > 0)
                {
                    saveItemCommandCommandText += ",";
                }
                saveItemCommandCommandText += "@" + saveColumns[i];
            }
            saveItemCommandCommandText += ")";
            return saveItemCommandCommandText;
        }

        /// <summary>
        /// Save a standard item in the repo
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Task SaveItem(BaseItem item, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            return SaveItems(new List<BaseItem> { item }, cancellationToken);
        }

        /// <summary>
        /// Saves the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// items
        /// or
        /// cancellationToken
        /// </exception>
        public async Task SaveItems(List<BaseItem> items, CancellationToken cancellationToken)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            cancellationToken.ThrowIfCancellationRequested();

            CheckDisposed();

            var tuples = new List<Tuple<BaseItem, List<Guid>, BaseItem, string>>();
            foreach (var item in items)
            {
                var ancestorIds = item.SupportsAncestors ?
                    item.GetAncestorIds().Distinct().ToList() :
                    null;

                var topParent = item.GetTopParent();

                var userdataKey = item.GetUserDataKeys().FirstOrDefault();

                tuples.Add(new Tuple<BaseItem, List<Guid>, BaseItem, string>(item, ancestorIds, topParent, userdataKey));
            }

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        SaveItemsInTranscation(db, tuples);
                    }, TransactionMode);
                }
            }
        }

        private void SaveItemsInTranscation(IDatabaseConnection db, List<Tuple<BaseItem, List<Guid>, BaseItem, string>> tuples)
        {
            var requiresReset = false;

            var statements = PrepareAllSafe(db, new string[]
            {
                GetSaveItemCommandText(),
                "delete from AncestorIds where ItemId=@ItemId",
                "insert into AncestorIds (ItemId, AncestorId, AncestorIdText) values (@ItemId, @AncestorId, @AncestorIdText)"
            }).ToList();

            using (var saveItemStatement = statements[0])
            {
                using (var deleteAncestorsStatement = statements[1])
                {
                    using (var updateAncestorsStatement = statements[2])
                    {
                        foreach (var tuple in tuples)
                        {
                            if (requiresReset)
                            {
                                saveItemStatement.Reset();
                            }

                            var item = tuple.Item1;
                            var topParent = tuple.Item3;
                            var userDataKey = tuple.Item4;

                            SaveItem(item, topParent, userDataKey, saveItemStatement);
                            //Logger.Debug(_saveItemCommand.CommandText);

                            if (item.SupportsAncestors)
                            {
                                UpdateAncestors(item.Id, tuple.Item2, db, deleteAncestorsStatement, updateAncestorsStatement);
                            }

                            UpdateItemValues(item.Id, GetItemValuesToSave(item), db);

                            requiresReset = true;
                        }
                    }
                }
            }
        }

        private void SaveItem(BaseItem item, BaseItem topParent, string userDataKey, IStatement saveItemStatement)
        {
            saveItemStatement.TryBind("@guid", item.Id);
            saveItemStatement.TryBind("@type", item.GetType().FullName);

            if (TypeRequiresDeserialization(item.GetType()))
            {
                saveItemStatement.TryBind("@data", _jsonSerializer.SerializeToBytes(item, _memoryStreamProvider));
            }
            else
            {
                saveItemStatement.TryBindNull("@data");
            }

            saveItemStatement.TryBind("@Path", item.Path);

            var hasStartDate = item as IHasStartDate;
            if (hasStartDate != null)
            {
                saveItemStatement.TryBind("@StartDate", hasStartDate.StartDate);
            }
            else
            {
                saveItemStatement.TryBindNull("@StartDate");
            }

            if (item.EndDate.HasValue)
            {
                saveItemStatement.TryBind("@EndDate", item.EndDate.Value);
            }
            else
            {
                saveItemStatement.TryBindNull("@EndDate");
            }

            saveItemStatement.TryBind("@ChannelId", item.ChannelId);

            var hasProgramAttributes = item as IHasProgramAttributes;
            if (hasProgramAttributes != null)
            {
                saveItemStatement.TryBind("@IsKids", hasProgramAttributes.IsKids);
                saveItemStatement.TryBind("@IsMovie", hasProgramAttributes.IsMovie);
                saveItemStatement.TryBind("@IsSports", hasProgramAttributes.IsSports);
                saveItemStatement.TryBind("@IsSeries", hasProgramAttributes.IsSeries);
                saveItemStatement.TryBind("@IsLive", hasProgramAttributes.IsLive);
                saveItemStatement.TryBind("@IsNews", hasProgramAttributes.IsNews);
                saveItemStatement.TryBind("@IsPremiere", hasProgramAttributes.IsPremiere);
                saveItemStatement.TryBind("@EpisodeTitle", hasProgramAttributes.EpisodeTitle);
                saveItemStatement.TryBind("@IsRepeat", hasProgramAttributes.IsRepeat);
            }
            else
            {
                saveItemStatement.TryBindNull("@IsKids");
                saveItemStatement.TryBindNull("@IsMovie");
                saveItemStatement.TryBindNull("@IsSports");
                saveItemStatement.TryBindNull("@IsSeries");
                saveItemStatement.TryBindNull("@IsLive");
                saveItemStatement.TryBindNull("@IsNews");
                saveItemStatement.TryBindNull("@IsPremiere");
                saveItemStatement.TryBindNull("@EpisodeTitle");
                saveItemStatement.TryBindNull("@IsRepeat");
            }

            saveItemStatement.TryBind("@CommunityRating", item.CommunityRating);
            saveItemStatement.TryBind("@CustomRating", item.CustomRating);
            saveItemStatement.TryBind("@IndexNumber", item.IndexNumber);
            saveItemStatement.TryBind("@IsLocked", item.IsLocked);
            saveItemStatement.TryBind("@Name", item.Name);
            saveItemStatement.TryBind("@OfficialRating", item.OfficialRating);
            saveItemStatement.TryBind("@MediaType", item.MediaType);
            saveItemStatement.TryBind("@Overview", item.Overview);
            saveItemStatement.TryBind("@ParentIndexNumber", item.ParentIndexNumber);
            saveItemStatement.TryBind("@PremiereDate", item.PremiereDate);
            saveItemStatement.TryBind("@ProductionYear", item.ProductionYear);

            if (item.ParentId == Guid.Empty)
            {
                saveItemStatement.TryBindNull("@ParentId");
            }
            else
            {
                saveItemStatement.TryBind("@ParentId", item.ParentId);
            }

            if (item.Genres.Count > 0)
            {
                saveItemStatement.TryBind("@Genres", string.Join("|", item.Genres.ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@Genres");
            }

            saveItemStatement.TryBind("@InheritedParentalRatingValue", item.InheritedParentalRatingValue);

            saveItemStatement.TryBind("@SortName", item.SortName);
            saveItemStatement.TryBind("@RunTimeTicks", item.RunTimeTicks);

            saveItemStatement.TryBind("@OfficialRatingDescription", item.OfficialRatingDescription);
            saveItemStatement.TryBind("@HomePageUrl", item.HomePageUrl);
            saveItemStatement.TryBind("@VoteCount", item.VoteCount);
            saveItemStatement.TryBind("@DisplayMediaType", item.DisplayMediaType);
            saveItemStatement.TryBind("@DateCreated", item.DateCreated);
            saveItemStatement.TryBind("@DateModified", item.DateModified);

            saveItemStatement.TryBind("@ForcedSortName", item.ForcedSortName);
            saveItemStatement.TryBind("@LocationType", item.LocationType.ToString());

            saveItemStatement.TryBind("@PreferredMetadataLanguage", item.PreferredMetadataLanguage);
            saveItemStatement.TryBind("@PreferredMetadataCountryCode", item.PreferredMetadataCountryCode);
            saveItemStatement.TryBind("@IsHD", item.IsHD);
            saveItemStatement.TryBind("@ExternalEtag", item.ExternalEtag);

            if (item.DateLastRefreshed != default(DateTime))
            {
                saveItemStatement.TryBind("@DateLastRefreshed", item.DateLastRefreshed);
            }
            else
            {
                saveItemStatement.TryBindNull("@DateLastRefreshed");
            }

            if (item.DateLastSaved != default(DateTime))
            {
                saveItemStatement.TryBind("@DateLastSaved", item.DateLastSaved);
            }
            else
            {
                saveItemStatement.TryBindNull("@DateLastSaved");
            }

            saveItemStatement.TryBind("@IsInMixedFolder", item.IsInMixedFolder);

            if (item.LockedFields.Count > 0)
            {
                saveItemStatement.TryBind("@LockedFields", string.Join("|", item.LockedFields.Select(i => i.ToString()).ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@LockedFields");
            }

            if (item.Studios.Count > 0)
            {
                saveItemStatement.TryBind("@Studios", string.Join("|", item.Studios.ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@Studios");
            }

            if (item.Audio.HasValue)
            {
                saveItemStatement.TryBind("@Audio", item.Audio.Value.ToString());
            }
            else
            {
                saveItemStatement.TryBindNull("@Audio");
            }

            saveItemStatement.TryBind("@ExternalServiceId", item.ServiceName);

            if (item.Tags.Count > 0)
            {
                saveItemStatement.TryBind("@Tags", string.Join("|", item.Tags.ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@Tags");
            }

            saveItemStatement.TryBind("@IsFolder", item.IsFolder);

            saveItemStatement.TryBind("@UnratedType", item.GetBlockUnratedType().ToString());

            if (topParent != null)
            {
                //Logger.Debug("Item {0} has top parent {1}", item.Id, topParent.Id);
                saveItemStatement.TryBind("@TopParentId", topParent.Id.ToString("N"));
            }
            else
            {
                //Logger.Debug("Item {0} has null top parent", item.Id);
                saveItemStatement.TryBindNull("@TopParentId");
            }

            var isByName = false;
            var byName = item as IItemByName;
            if (byName != null)
            {
                var dualAccess = item as IHasDualAccess;
                isByName = dualAccess == null || dualAccess.IsAccessedByName;
            }
            saveItemStatement.TryBind("@IsItemByName", isByName);
            saveItemStatement.TryBind("@SourceType", item.SourceType.ToString());

            var trailer = item as Trailer;
            if (trailer != null && trailer.TrailerTypes.Count > 0)
            {
                saveItemStatement.TryBind("@TrailerTypes", string.Join("|", trailer.TrailerTypes.Select(i => i.ToString()).ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@TrailerTypes");
            }

            saveItemStatement.TryBind("@CriticRating", item.CriticRating);
            saveItemStatement.TryBind("@CriticRatingSummary", item.CriticRatingSummary);

            var inheritedTags = item.InheritedTags;
            if (inheritedTags.Count > 0)
            {
                saveItemStatement.TryBind("@InheritedTags", string.Join("|", inheritedTags.ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@InheritedTags");
            }

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                saveItemStatement.TryBindNull("@CleanName");
            }
            else
            {
                saveItemStatement.TryBind("@CleanName", GetCleanValue(item.Name));
            }

            saveItemStatement.TryBind("@PresentationUniqueKey", item.PresentationUniqueKey);
            saveItemStatement.TryBind("@SlugName", item.SlugName);
            saveItemStatement.TryBind("@OriginalTitle", item.OriginalTitle);

            var video = item as Video;
            if (video != null)
            {
                saveItemStatement.TryBind("@PrimaryVersionId", video.PrimaryVersionId);
            }
            else
            {
                saveItemStatement.TryBindNull("@PrimaryVersionId");
            }

            var folder = item as Folder;
            if (folder != null && folder.DateLastMediaAdded.HasValue)
            {
                saveItemStatement.TryBind("@DateLastMediaAdded", folder.DateLastMediaAdded.Value);
            }
            else
            {
                saveItemStatement.TryBindNull("@DateLastMediaAdded");
            }

            saveItemStatement.TryBind("@Album", item.Album);
            saveItemStatement.TryBind("@IsVirtualItem", item.IsVirtualItem);

            var hasSeries = item as IHasSeries;
            if (hasSeries != null)
            {
                saveItemStatement.TryBind("@SeriesName", hasSeries.SeriesName);
            }
            else
            {
                saveItemStatement.TryBindNull("@SeriesName");
            }

            if (string.IsNullOrWhiteSpace(userDataKey))
            {
                saveItemStatement.TryBindNull("@UserDataKey");
            }
            else
            {
                saveItemStatement.TryBind("@UserDataKey", userDataKey);
            }

            var episode = item as Episode;
            if (episode != null)
            {
                saveItemStatement.TryBind("@SeasonName", episode.SeasonName);
                saveItemStatement.TryBind("@SeasonId", episode.SeasonId);
            }
            else
            {
                saveItemStatement.TryBindNull("@SeasonName");
                saveItemStatement.TryBindNull("@SeasonId");
            }

            if (hasSeries != null)
            {
                saveItemStatement.TryBind("@SeriesId", hasSeries.SeriesId);
                saveItemStatement.TryBind("@SeriesSortName", hasSeries.SeriesSortName);
                saveItemStatement.TryBind("@SeriesPresentationUniqueKey", hasSeries.SeriesPresentationUniqueKey);
            }
            else
            {
                saveItemStatement.TryBindNull("@SeriesId");
                saveItemStatement.TryBindNull("@SeriesSortName");
                saveItemStatement.TryBindNull("@SeriesPresentationUniqueKey");
            }

            saveItemStatement.TryBind("@ExternalSeriesId", item.ExternalSeriesId);
            saveItemStatement.TryBind("@Tagline", item.Tagline);

            if (item.Keywords.Count > 0)
            {
                saveItemStatement.TryBind("@Keywords", string.Join("|", item.Keywords.ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@Keywords");
            }

            saveItemStatement.TryBind("@ProviderIds", SerializeProviderIds(item));
            saveItemStatement.TryBind("@Images", SerializeImages(item));

            if (item.ProductionLocations.Count > 0)
            {
                saveItemStatement.TryBind("@ProductionLocations", string.Join("|", item.ProductionLocations.ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@ProductionLocations");
            }

            if (item.ThemeSongIds.Count > 0)
            {
                saveItemStatement.TryBind("@ThemeSongIds", string.Join("|", item.ThemeSongIds.ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@ThemeSongIds");
            }

            if (item.ThemeVideoIds.Count > 0)
            {
                saveItemStatement.TryBind("@ThemeVideoIds", string.Join("|", item.ThemeVideoIds.ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@ThemeVideoIds");
            }

            saveItemStatement.TryBind("@TotalBitrate", item.TotalBitrate);
            if (item.ExtraType.HasValue)
            {
                saveItemStatement.TryBind("@ExtraType", item.ExtraType.Value.ToString());
            }
            else
            {
                saveItemStatement.TryBindNull("@ExtraType");
            }

            string artists = null;
            var hasArtists = item as IHasArtist;
            if (hasArtists != null)
            {
                if (hasArtists.Artists.Count > 0)
                {
                    artists = string.Join("|", hasArtists.Artists.ToArray());
                }
            }
            saveItemStatement.TryBind("@Artists", artists);

            string albumArtists = null;
            var hasAlbumArtists = item as IHasAlbumArtist;
            if (hasAlbumArtists != null)
            {
                if (hasAlbumArtists.AlbumArtists.Count > 0)
                {
                    albumArtists = string.Join("|", hasAlbumArtists.AlbumArtists.ToArray());
                }
            }
            saveItemStatement.TryBind("@AlbumArtists", albumArtists);
            saveItemStatement.TryBind("@ExternalId", item.ExternalId);

            saveItemStatement.MoveNext();
        }

        private string SerializeProviderIds(BaseItem item)
        {
            // Ideally we shouldn't need this IsNullOrWhiteSpace check but we're seeing some cases of bad data slip through
            var ids = item.ProviderIds
                .Where(i => !string.IsNullOrWhiteSpace(i.Value))
                .ToList();

            if (ids.Count == 0)
            {
                return null;
            }

            return string.Join("|", ids.Select(i => i.Key + "=" + i.Value).ToArray());
        }

        private void DeserializeProviderIds(string value, BaseItem item)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (item.ProviderIds.Count > 0)
            {
                return;
            }

            var parts = value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var idParts = part.Split('=');

                if (idParts.Length == 2)
                {
                    item.SetProviderId(idParts[0], idParts[1]);
                }
            }
        }

        private string SerializeImages(BaseItem item)
        {
            var images = item.ImageInfos.ToList();

            if (images.Count == 0)
            {
                return null;
            }

            var imageStrings = images.Where(i => !string.IsNullOrWhiteSpace(i.Path)).Select(ToValueString).ToArray();

            return string.Join("|", imageStrings);
        }

        private void DeserializeImages(string value, BaseItem item)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            if (item.ImageInfos.Count > 0)
            {
                return;
            }

            var parts = value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var image = ItemImageInfoFromValueString(part);

                if (image != null)
                {
                    item.ImageInfos.Add(image);
                }
            }
        }

        public string ToValueString(ItemImageInfo image)
        {
            var delimeter = "*";

            var path = image.Path;

            if (path == null)
            {
                path = string.Empty;
            }

            return path +
                delimeter +
                image.DateModified.Ticks.ToString(CultureInfo.InvariantCulture) +
                delimeter +
                image.Type +
                delimeter +
                image.IsPlaceholder;
        }

        public ItemImageInfo ItemImageInfoFromValueString(string value)
        {
            var parts = value.Split(new[] { '*' }, StringSplitOptions.None);

            if (parts.Length != 4)
            {
                return null;
            }

            var image = new ItemImageInfo();

            image.Path = parts[0];
            image.DateModified = new DateTime(long.Parse(parts[1], CultureInfo.InvariantCulture), DateTimeKind.Utc);
            image.Type = (ImageType)Enum.Parse(typeof(ImageType), parts[2], true);
            image.IsPlaceholder = string.Equals(parts[3], true.ToString(), StringComparison.OrdinalIgnoreCase);

            return image;
        }

        /// <summary>
        /// Internal retrieve from items or users table
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        /// <exception cref="System.ArgumentException"></exception>
        public BaseItem RetrieveItem(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            CheckDisposed();
            //Logger.Info("Retrieving item {0}", id.ToString("N"));
            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = PrepareStatementSafe(connection, "select " + string.Join(",", _retriveItemColumns) + " from TypedBaseItems where guid = @guid"))
                    {
                        statement.TryBind("@guid", id);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            return GetItem(row);
                        }
                    }

                    return null;
                }
            }
        }

        private BaseItem GetItem(IReadOnlyList<IResultSetValue> reader)
        {
            return GetItem(reader, new InternalItemsQuery());
        }

        private bool TypeRequiresDeserialization(Type type)
        {
            if (_config.Configuration.SkipDeserializationForBasicTypes)
            {
                if (type == typeof(MusicGenre))
                {
                    return false;
                }
                if (type == typeof(GameGenre))
                {
                    return false;
                }
                if (type == typeof(Genre))
                {
                    return false;
                }
                if (type == typeof(Studio))
                {
                    return false;
                }
                if (type == typeof(Year))
                {
                    return false;
                }
                if (type == typeof(Book))
                {
                    return false;
                }
                if (type == typeof(Person))
                {
                    return false;
                }
                if (type == typeof(RecordingGroup))
                {
                    return false;
                }
                if (type == typeof(Channel))
                {
                    return false;
                }
                if (type == typeof(ManualCollectionsFolder))
                {
                    return false;
                }
                if (type == typeof(CameraUploadsFolder))
                {
                    return false;
                }
                if (type == typeof(PlaylistsFolder))
                {
                    return false;
                }
                if (type == typeof(UserRootFolder))
                {
                    return false;
                }
                if (type == typeof(PhotoAlbum))
                {
                    return false;
                }
                if (type == typeof(Season))
                {
                    return false;
                }
                if (type == typeof(MusicArtist))
                {
                    return false;
                }
            }
            if (_config.Configuration.SkipDeserializationForPrograms)
            {
                if (type == typeof(LiveTvProgram))
                {
                    return false;
                }
            }
            if (_config.Configuration.SkipDeserializationForAudio)
            {
                if (type == typeof(Audio))
                {
                    return false;
                }
                if (type == typeof(LiveTvAudioRecording))
                {
                    return false;
                }
                if (type == typeof(AudioPodcast))
                {
                    return false;
                }
                if (type == typeof(AudioBook))
                {
                    return false;
                }
                if (type == typeof(MusicAlbum))
                {
                    return false;
                }
            }

            return true;
        }

        private BaseItem GetItem(IReadOnlyList<IResultSetValue> reader, InternalItemsQuery query)
        {
            var typeString = reader.GetString(0);

            var type = _typeMapper.GetType(typeString);

            if (type == null)
            {
                //Logger.Debug("Unknown type {0}", typeString);

                return null;
            }

            BaseItem item = null;

            if (TypeRequiresDeserialization(type))
            {
                using (var stream = _memoryStreamProvider.CreateNew(reader[1].ToBlob()))
                {
                    stream.Position = 0;

                    try
                    {
                        item = _jsonSerializer.DeserializeFromStream(stream, type) as BaseItem;
                    }
                    catch (SerializationException ex)
                    {
                        Logger.ErrorException("Error deserializing item", ex);
                    }
                }
            }

            if (item == null)
            {
                try
                {
                    item = Activator.CreateInstance(type) as BaseItem;
                }
                catch
                {
                }
            }

            if (item == null)
            {
                return null;
            }

            if (!reader.IsDBNull(2))
            {
                var hasStartDate = item as IHasStartDate;
                if (hasStartDate != null)
                {
                    hasStartDate.StartDate = reader[2].ReadDateTime();
                }
            }

            if (!reader.IsDBNull(3))
            {
                item.EndDate = reader[3].ReadDateTime();
            }

            if (!reader.IsDBNull(4))
            {
                item.ChannelId = reader.GetString(4);
            }

            var index = 5;

            var hasProgramAttributes = item as IHasProgramAttributes;
            if (hasProgramAttributes != null)
            {
                if (!reader.IsDBNull(index))
                {
                    hasProgramAttributes.IsMovie = reader.GetBoolean(index);
                }
                index++;

                if (!reader.IsDBNull(index))
                {
                    hasProgramAttributes.IsSports = reader.GetBoolean(index);
                }
                index++;

                if (!reader.IsDBNull(index))
                {
                    hasProgramAttributes.IsKids = reader.GetBoolean(index);
                }
                index++;

                if (!reader.IsDBNull(index))
                {
                    hasProgramAttributes.IsSeries = reader.GetBoolean(index);
                }
                index++;

                if (!reader.IsDBNull(index))
                {
                    hasProgramAttributes.IsLive = reader.GetBoolean(index);
                }
                index++;

                if (!reader.IsDBNull(index))
                {
                    hasProgramAttributes.IsNews = reader.GetBoolean(index);
                }
                index++;

                if (!reader.IsDBNull(index))
                {
                    hasProgramAttributes.IsPremiere = reader.GetBoolean(index);
                }
                index++;

                if (!reader.IsDBNull(index))
                {
                    hasProgramAttributes.EpisodeTitle = reader.GetString(index);
                }
                index++;

                if (!reader.IsDBNull(index))
                {
                    hasProgramAttributes.IsRepeat = reader.GetBoolean(index);
                }
                index++;
            }
            else
            {
                index += 9;
            }

            if (!reader.IsDBNull(index))
            {
                item.CommunityRating = reader.GetFloat(index);
            }
            index++;

            if (query.HasField(ItemFields.CustomRating))
            {
                if (!reader.IsDBNull(index))
                {
                    item.CustomRating = reader.GetString(index);
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.IndexNumber = reader.GetInt32(index);
            }
            index++;

            if (query.HasField(ItemFields.Settings))
            {
                if (!reader.IsDBNull(index))
                {
                    item.IsLocked = reader.GetBoolean(index);
                }
                index++;

                if (!reader.IsDBNull(index))
                {
                    item.PreferredMetadataLanguage = reader.GetString(index);
                }
                index++;

                if (!reader.IsDBNull(index))
                {
                    item.PreferredMetadataCountryCode = reader.GetString(index);
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.IsHD = reader.GetBoolean(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.ExternalEtag = reader.GetString(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.DateLastRefreshed = reader[index].ReadDateTime();
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.Name = reader.GetString(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.Path = reader.GetString(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.PremiereDate = reader[index].ReadDateTime();
            }
            index++;

            if (query.HasField(ItemFields.Overview))
            {
                if (!reader.IsDBNull(index))
                {
                    item.Overview = reader.GetString(index);
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.ParentIndexNumber = reader.GetInt32(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.ProductionYear = reader.GetInt32(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.OfficialRating = reader.GetString(index);
            }
            index++;

            if (query.HasField(ItemFields.OfficialRatingDescription))
            {
                if (!reader.IsDBNull(index))
                {
                    item.OfficialRatingDescription = reader.GetString(index);
                }
                index++;
            }

            if (query.HasField(ItemFields.HomePageUrl))
            {
                if (!reader.IsDBNull(index))
                {
                    item.HomePageUrl = reader.GetString(index);
                }
                index++;
            }

            if (query.HasField(ItemFields.DisplayMediaType))
            {
                if (!reader.IsDBNull(index))
                {
                    item.DisplayMediaType = reader.GetString(index);
                }
                index++;
            }

            if (query.HasField(ItemFields.SortName))
            {
                if (!reader.IsDBNull(index))
                {
                    item.ForcedSortName = reader.GetString(index);
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.RunTimeTicks = reader.GetInt64(index);
            }
            index++;

            if (query.HasField(ItemFields.VoteCount))
            {
                if (!reader.IsDBNull(index))
                {
                    item.VoteCount = reader.GetInt32(index);
                }
                index++;
            }

            if (query.HasField(ItemFields.DateCreated))
            {
                if (!reader.IsDBNull(index))
                {
                    item.DateCreated = reader[index].ReadDateTime();
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.DateModified = reader[index].ReadDateTime();
            }
            index++;

            item.Id = reader.GetGuid(index);
            index++;

            if (query.HasField(ItemFields.Genres))
            {
                if (!reader.IsDBNull(index))
                {
                    item.Genres = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.ParentId = reader.GetGuid(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.Audio = (ProgramAudio)Enum.Parse(typeof(ProgramAudio), reader.GetString(index), true);
            }
            index++;

            // TODO: Even if not needed by apps, the server needs it internally
            // But get this excluded from contexts where it is not needed
            if (!reader.IsDBNull(index))
            {
                item.ServiceName = reader.GetString(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.IsInMixedFolder = reader.GetBoolean(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.DateLastSaved = reader[index].ReadDateTime();
            }
            index++;

            if (query.HasField(ItemFields.Settings))
            {
                if (!reader.IsDBNull(index))
                {
                    item.LockedFields = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => (MetadataFields)Enum.Parse(typeof(MetadataFields), i, true)).ToList();
                }
                index++;
            }

            if (query.HasField(ItemFields.Studios))
            {
                if (!reader.IsDBNull(index))
                {
                    item.Studios = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                }
                index++;
            }

            if (query.HasField(ItemFields.Tags))
            {
                if (!reader.IsDBNull(index))
                {
                    item.Tags = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.SourceType = (SourceType)Enum.Parse(typeof(SourceType), reader.GetString(index), true);
            }
            index++;

            var trailer = item as Trailer;
            if (trailer != null)
            {
                if (!reader.IsDBNull(index))
                {
                    trailer.TrailerTypes = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => (TrailerType)Enum.Parse(typeof(TrailerType), i, true)).ToList();
                }
            }
            index++;

            if (query.HasField(ItemFields.OriginalTitle))
            {
                if (!reader.IsDBNull(index))
                {
                    item.OriginalTitle = reader.GetString(index);
                }
                index++;
            }

            var video = item as Video;
            if (video != null)
            {
                if (!reader.IsDBNull(index))
                {
                    video.PrimaryVersionId = reader.GetString(index);
                }
            }
            index++;

            if (query.HasField(ItemFields.DateLastMediaAdded))
            {
                var folder = item as Folder;
                if (folder != null && !reader.IsDBNull(index))
                {
                    folder.DateLastMediaAdded = reader[index].ReadDateTime();
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.Album = reader.GetString(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.CriticRating = reader.GetFloat(index);
            }
            index++;

            if (query.HasField(ItemFields.CriticRatingSummary))
            {
                if (!reader.IsDBNull(index))
                {
                    item.CriticRatingSummary = reader.GetString(index);
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.IsVirtualItem = reader.GetBoolean(index);
            }
            index++;

            var hasSeries = item as IHasSeries;
            if (hasSeries != null)
            {
                if (!reader.IsDBNull(index))
                {
                    hasSeries.SeriesName = reader.GetString(index);
                }
            }
            index++;

            var episode = item as Episode;
            if (episode != null)
            {
                if (!reader.IsDBNull(index))
                {
                    episode.SeasonName = reader.GetString(index);
                }
                index++;
                if (!reader.IsDBNull(index))
                {
                    episode.SeasonId = reader.GetGuid(index);
                }
            }
            else
            {
                index++;
            }
            index++;

            if (hasSeries != null)
            {
                if (!reader.IsDBNull(index))
                {
                    hasSeries.SeriesId = reader.GetGuid(index);
                }
            }
            index++;

            if (hasSeries != null)
            {
                if (!reader.IsDBNull(index))
                {
                    hasSeries.SeriesSortName = reader.GetString(index);
                }
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.PresentationUniqueKey = reader.GetString(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.InheritedParentalRatingValue = reader.GetInt32(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.InheritedTags = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.ExternalSeriesId = reader.GetString(index);
            }
            index++;

            if (query.HasField(ItemFields.Taglines))
            {
                if (!reader.IsDBNull(index))
                {
                    item.Tagline = reader.GetString(index);
                }
                index++;
            }

            if (query.HasField(ItemFields.Keywords))
            {
                if (!reader.IsDBNull(index))
                {
                    item.Keywords = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                DeserializeProviderIds(reader.GetString(index), item);
            }
            index++;

            if (query.DtoOptions.EnableImages)
            {
                if (!reader.IsDBNull(index))
                {
                    DeserializeImages(reader.GetString(index), item);
                }
                index++;
            }

            if (query.HasField(ItemFields.ProductionLocations))
            {
                if (!reader.IsDBNull(index))
                {
                    item.ProductionLocations = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
                }
                index++;
            }

            if (query.HasField(ItemFields.ThemeSongIds))
            {
                if (!reader.IsDBNull(index))
                {
                    item.ThemeSongIds = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => new Guid(i)).ToList();
                }
                index++;
            }

            if (query.HasField(ItemFields.ThemeVideoIds))
            {
                if (!reader.IsDBNull(index))
                {
                    item.ThemeVideoIds = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => new Guid(i)).ToList();
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.TotalBitrate = reader.GetInt32(index);
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.ExtraType = (ExtraType)Enum.Parse(typeof(ExtraType), reader.GetString(index), true);
            }
            index++;

            var hasArtists = item as IHasArtist;
            if (hasArtists != null && !reader.IsDBNull(index))
            {
                hasArtists.Artists = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            }
            index++;

            var hasAlbumArtists = item as IHasAlbumArtist;
            if (hasAlbumArtists != null && !reader.IsDBNull(index))
            {
                hasAlbumArtists.AlbumArtists = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.ExternalId = reader.GetString(index);
            }
            index++;

            if (hasSeries != null)
            {
                if (!reader.IsDBNull(index))
                {
                    hasSeries.SeriesPresentationUniqueKey = reader.GetString(index);
                }
            }
            index++;

            if (string.IsNullOrWhiteSpace(item.Tagline))
            {
                var movie = item as Movie;
                if (movie != null && movie.Taglines.Count > 0)
                {
                    movie.Tagline = movie.Taglines[0];
                }
            }

            if (type == typeof(Person) && item.ProductionLocations.Count == 0)
            {
                var person = (Person)item;
                if (!string.IsNullOrWhiteSpace(person.PlaceOfBirth))
                {
                    item.ProductionLocations = new List<string> { person.PlaceOfBirth };
                }
            }

            return item;
        }

        /// <summary>
        /// Gets the critic reviews.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <returns>Task{IEnumerable{ItemReview}}.</returns>
        public IEnumerable<ItemReview> GetCriticReviews(Guid itemId)
        {
            try
            {
                var path = Path.Combine(_criticReviewsPath, itemId + ".json");

                return _jsonSerializer.DeserializeFromFile<List<ItemReview>>(path);
            }
            catch (FileNotFoundException)
            {
                return new List<ItemReview>();
            }
            catch (IOException)
            {
                return new List<ItemReview>();
            }
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        /// <summary>
        /// Saves the critic reviews.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="criticReviews">The critic reviews.</param>
        /// <returns>Task.</returns>
        public Task SaveCriticReviews(Guid itemId, IEnumerable<ItemReview> criticReviews)
        {
            _fileSystem.CreateDirectory(_criticReviewsPath);

            var path = Path.Combine(_criticReviewsPath, itemId + ".json");

            _jsonSerializer.SerializeToFile(criticReviews.ToList(), path);

            return _cachedTask;
        }

        /// <summary>
        /// Gets chapters for an item
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>IEnumerable{ChapterInfo}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public IEnumerable<ChapterInfo> GetChapters(Guid id)
        {
            CheckDisposed();
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var list = new List<ChapterInfo>();

                    using (var statement = PrepareStatementSafe(connection, "select StartPositionTicks,Name,ImagePath,ImageDateModified from " + ChaptersTableName + " where ItemId = @ItemId order by ChapterIndex asc"))
                    {
                        statement.TryBind("@ItemId", id);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            list.Add(GetChapter(row));
                        }
                    }

                    return list;
                }
            }
        }

        /// <summary>
        /// Gets a single chapter for an item
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="index">The index.</param>
        /// <returns>ChapterInfo.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public ChapterInfo GetChapter(Guid id, int index)
        {
            CheckDisposed();
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = PrepareStatementSafe(connection, "select StartPositionTicks,Name,ImagePath,ImageDateModified from " + ChaptersTableName + " where ItemId = @ItemId and ChapterIndex=@ChapterIndex"))
                    {
                        statement.TryBind("@ItemId", id);
                        statement.TryBind("@ChapterIndex", index);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            return GetChapter(row);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the chapter.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>ChapterInfo.</returns>
        private ChapterInfo GetChapter(IReadOnlyList<IResultSetValue> reader)
        {
            var chapter = new ChapterInfo
            {
                StartPositionTicks = reader.GetInt64(0)
            };

            if (!reader.IsDBNull(1))
            {
                chapter.Name = reader.GetString(1);
            }

            if (!reader.IsDBNull(2))
            {
                chapter.ImagePath = reader.GetString(2);
            }

            if (!reader.IsDBNull(3))
            {
                chapter.ImageDateModified = reader[3].ReadDateTime();
            }

            return chapter;
        }

        /// <summary>
        /// Saves the chapters.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="chapters">The chapters.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// id
        /// or
        /// chapters
        /// or
        /// cancellationToken
        /// </exception>
        public async Task SaveChapters(Guid id, List<ChapterInfo> chapters, CancellationToken cancellationToken)
        {
            CheckDisposed();

            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            if (chapters == null)
            {
                throw new ArgumentNullException("chapters");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var index = 0;

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        // First delete chapters
                        db.Execute("delete from " + ChaptersTableName + " where ItemId=@ItemId", id.ToGuidParamValue());

                        using (var saveChapterStatement = PrepareStatement(db, "replace into " + ChaptersTableName + " (ItemId, ChapterIndex, StartPositionTicks, Name, ImagePath, ImageDateModified) values (@ItemId, @ChapterIndex, @StartPositionTicks, @Name, @ImagePath, @ImageDateModified)"))
                        {
                            foreach (var chapter in chapters)
                            {
                                if (index > 0)
                                {
                                    saveChapterStatement.Reset();
                                }

                                saveChapterStatement.TryBind("@ItemId", id.ToGuidParamValue());
                                saveChapterStatement.TryBind("@ChapterIndex", index);
                                saveChapterStatement.TryBind("@StartPositionTicks", chapter.StartPositionTicks);
                                saveChapterStatement.TryBind("@Name", chapter.Name);
                                saveChapterStatement.TryBind("@ImagePath", chapter.ImagePath);
                                saveChapterStatement.TryBind("@ImageDateModified", chapter.ImageDateModified);

                                saveChapterStatement.MoveNext();

                                index++;
                            }
                        }
                    }, TransactionMode);
                }
            }
        }

        private bool EnableJoinUserData(InternalItemsQuery query)
        {
            if (query.User == null)
            {
                return false;
            }

            if (query.SimilarTo != null && query.User != null)
            {
                //return true;
            }

            var sortingFields = query.SortBy.ToList();
            sortingFields.AddRange(query.OrderBy.Select(i => i.Item1));

            if (sortingFields.Contains(ItemSortBy.IsFavoriteOrLiked, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            if (sortingFields.Contains(ItemSortBy.IsPlayed, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            if (sortingFields.Contains(ItemSortBy.IsUnplayed, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            if (sortingFields.Contains(ItemSortBy.PlayCount, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            if (sortingFields.Contains(ItemSortBy.DatePlayed, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }
            if (sortingFields.Contains(ItemSortBy.SeriesDatePlayed, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            if (query.IsFavoriteOrLiked.HasValue)
            {
                return true;
            }

            if (query.IsFavorite.HasValue)
            {
                return true;
            }

            if (query.IsResumable.HasValue)
            {
                return true;
            }

            if (query.IsPlayed.HasValue)
            {
                return true;
            }

            if (query.IsLiked.HasValue)
            {
                return true;
            }

            return false;
        }

        private List<ItemFields> allFields = Enum.GetNames(typeof(ItemFields))
                    .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
                    .ToList();

        private IEnumerable<string> GetColumnNamesFromField(ItemFields field)
        {
            if (field == ItemFields.Settings)
            {
                return new[] { "IsLocked", "PreferredMetadataCountryCode", "PreferredMetadataLanguage", "LockedFields" };
            }
            if (field == ItemFields.ServiceName)
            {
                return new[] { "ExternalServiceId" };
            }
            if (field == ItemFields.SortName)
            {
                return new[] { "ForcedSortName" };
            }
            if (field == ItemFields.Taglines)
            {
                return new[] { "Tagline" };
            }

            return new[] { field.ToString() };
        }

        private string[] GetFinalColumnsToSelect(InternalItemsQuery query, string[] startColumns)
        {
            var list = startColumns.ToList();

            foreach (var field in allFields)
            {
                if (!query.HasField(field))
                {
                    foreach (var fieldToRemove in GetColumnNamesFromField(field).ToList())
                    {
                        list.Remove(fieldToRemove);
                    }
                }
            }

            if (!query.DtoOptions.EnableImages)
            {
                list.Remove("Images");
            }

            if (EnableJoinUserData(query))
            {
                list.Add("UserData.UserId");
                list.Add("UserData.lastPlayedDate");
                list.Add("UserData.playbackPositionTicks");
                list.Add("UserData.playcount");
                list.Add("UserData.isFavorite");
                list.Add("UserData.played");
                list.Add("UserData.rating");
            }

            if (query.SimilarTo != null)
            {
                var item = query.SimilarTo;

                var builder = new StringBuilder();
                builder.Append("(");

                builder.Append("((OfficialRating=@ItemOfficialRating) * 10)");
                //builder.Append("+ ((ProductionYear=@ItemProductionYear) * 10)");

                builder.Append("+(Select Case When Abs(COALESCE(ProductionYear, 0) - @ItemProductionYear) < 10 Then 2 Else 0 End )");
                builder.Append("+(Select Case When Abs(COALESCE(ProductionYear, 0) - @ItemProductionYear) < 5 Then 2 Else 0 End )");

                //// genres, tags
                builder.Append("+ ((Select count(CleanValue) from ItemValues where ItemId=Guid and Type in (2,3,4,5) and CleanValue in (select CleanValue from itemvalues where ItemId=@SimilarItemId and Type in (2,3,4,5))) * 10)");

                //builder.Append("+ ((Select count(CleanValue) from ItemValues where ItemId=Guid and Type=3 and CleanValue in (select CleanValue from itemvalues where ItemId=@SimilarItemId and type=3)) * 3)");

                //builder.Append("+ ((Select count(Name) from People where ItemId=Guid and Name in (select Name from People where ItemId=@SimilarItemId)) * 3)");

                ////builder.Append("(select group_concat((Select Name from People where ItemId=Guid and Name in (Select Name from People where ItemId=@SimilarItemId)), '|'))");

                builder.Append(") as SimilarityScore");

                list.Add(builder.ToString());

                var excludeIds = query.ExcludeItemIds.ToList();
                excludeIds.Add(item.Id.ToString("N"));

                if (query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(typeof(Trailer).Name))
                {
                    var hasTrailers = item as IHasTrailers;
                    if (hasTrailers != null)
                    {
                        excludeIds.AddRange(hasTrailers.GetTrailerIds().Select(i => i.ToString("N")));
                    }
                }

                query.ExcludeItemIds = excludeIds.ToArray();
                query.ExcludeProviderIds = item.ProviderIds;
            }

            return list.ToArray();
        }

        private void BindSimilarParams(InternalItemsQuery query, IStatement statement)
        {
            var item = query.SimilarTo;

            if (item == null)
            {
                return;
            }

            statement.TryBind("@ItemOfficialRating", item.OfficialRating);
            statement.TryBind("@ItemProductionYear", item.ProductionYear ?? 0);
            statement.TryBind("@SimilarItemId", item.Id);
        }

        private string GetJoinUserDataText(InternalItemsQuery query)
        {
            if (!EnableJoinUserData(query))
            {
                return string.Empty;
            }

            return " left join UserData on UserDataKey=UserData.Key And (UserId=@UserId)";
        }

        private string GetGroupBy(InternalItemsQuery query)
        {
            var groups = new List<string>();

            if (EnableGroupByPresentationUniqueKey(query))
            {
                groups.Add("PresentationUniqueKey");
            }

            if (groups.Count > 0)
            {
                return " Group by " + string.Join(",", groups.ToArray());
            }

            return string.Empty;
        }

        private string GetFromText(string alias = "A")
        {
            return " from TypedBaseItems " + alias;
        }

        public int GetCount(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            //Logger.Info("GetItemList: " + _environmentInfo.StackTrace);

            var now = DateTime.UtcNow;

            // Hack for right now since we currently don't support filtering out these duplicates within a query
            if (query.Limit.HasValue && query.EnableGroupByMetadataKey)
            {
                query.Limit = query.Limit.Value + 4;
            }

            var commandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, new [] { "count(distinct PresentationUniqueKey)" })) + GetFromText();
            commandText += GetJoinUserDataText(query);

            var whereClauses = GetWhereClauses(query, null);

            var whereText = whereClauses.Count == 0 ?
                string.Empty :
                " where " + string.Join(" AND ", whereClauses.ToArray());

            commandText += whereText;

            //commandText += GetGroupBy(query);

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = PrepareStatementSafe(connection, commandText))
                    {
                        if (EnableJoinUserData(query))
                        {
                            statement.TryBind("@UserId", query.User.Id);
                        }

                        BindSimilarParams(query, statement);

                        // Running this again will bind the params
                        GetWhereClauses(query, statement);

                        var count = statement.ExecuteQuery().SelectScalarInt().First();
                        LogQueryTime("GetCount", commandText, now);
                        return count;
                    }
                }

            }
        }

        public List<BaseItem> GetItemList(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            //Logger.Info("GetItemList: " + _environmentInfo.StackTrace);

            var now = DateTime.UtcNow;

            // Hack for right now since we currently don't support filtering out these duplicates within a query
            if (query.Limit.HasValue && query.EnableGroupByMetadataKey)
            {
                query.Limit = query.Limit.Value + 4;
            }

            var commandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, _retriveItemColumns)) + GetFromText();
            commandText += GetJoinUserDataText(query);

            var whereClauses = GetWhereClauses(query, null);

            var whereText = whereClauses.Count == 0 ?
                string.Empty :
                " where " + string.Join(" AND ", whereClauses.ToArray());

            commandText += whereText;

            commandText += GetGroupBy(query);

            commandText += GetOrderByText(query);

            if (query.Limit.HasValue || query.StartIndex.HasValue)
            {
                var offset = query.StartIndex ?? 0;

                if (query.Limit.HasValue || offset > 0)
                {
                    commandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                }

                if (offset > 0)
                {
                    commandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                }
            }

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var list = new List<BaseItem>();

                    using (var statement = PrepareStatementSafe(connection, commandText))
                    {
                        if (EnableJoinUserData(query))
                        {
                            statement.TryBind("@UserId", query.User.Id);
                        }

                        BindSimilarParams(query, statement);

                        // Running this again will bind the params
                        GetWhereClauses(query, statement);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            var item = GetItem(row, query);
                            if (item != null)
                            {
                                list.Add(item);
                            }
                        }
                    }

                    // Hack for right now since we currently don't support filtering out these duplicates within a query
                    if (query.EnableGroupByMetadataKey)
                    {
                        var limit = query.Limit ?? int.MaxValue;
                        limit -= 4;
                        var newList = new List<BaseItem>();

                        foreach (var item in list)
                        {
                            AddItem(newList, item);

                            if (newList.Count >= limit)
                            {
                                break;
                            }
                        }

                        list = newList;
                    }

                    LogQueryTime("GetItemList", commandText, now);

                    return list;
                }
            }
        }

        private void AddItem(List<BaseItem> items, BaseItem newItem)
        {
            var providerIds = newItem.ProviderIds.ToList();

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                foreach (var providerId in providerIds)
                {
                    if (providerId.Key == MetadataProviders.TmdbCollection.ToString())
                    {
                        continue;
                    }
                    if (item.GetProviderId(providerId.Key) == providerId.Value)
                    {
                        if (newItem.SourceType == SourceType.Library)
                        {
                            items[i] = newItem;
                        }
                        return;
                    }
                }
            }

            items.Add(newItem);
        }

        private void LogQueryTime(string methodName, string commandText, DateTime startDate)
        {
            var elapsed = (DateTime.UtcNow - startDate).TotalMilliseconds;

            var slowThreshold = 1000;

#if DEBUG
            slowThreshold = 2;
#endif

            if (elapsed >= slowThreshold)
            {
                Logger.Debug("{2} query time (slow): {0}ms. Query: {1}",
                    Convert.ToInt32(elapsed),
                    commandText,
                    methodName);
            }
            else
            {
                //Logger.Debug("{2} query time: {0}ms. Query: {1}",
                //    Convert.ToInt32(elapsed),
                //    commandText,
                //    methodName);
            }
        }

        public QueryResult<BaseItem> GetItems(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            if (!query.EnableTotalRecordCount || (!query.Limit.HasValue && (query.StartIndex ?? 0) == 0))
            {
                var returnList = GetItemList(query);
                return new QueryResult<BaseItem>
                {
                    Items = returnList.ToArray(),
                    TotalRecordCount = returnList.Count
                };
            }
            //Logger.Info("GetItems: " + _environmentInfo.StackTrace);

            var now = DateTime.UtcNow;

            var list = new List<BaseItem>();

            // Hack for right now since we currently don't support filtering out these duplicates within a query
            if (query.Limit.HasValue && query.EnableGroupByMetadataKey)
            {
                query.Limit = query.Limit.Value + 4;
            }

            var commandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, _retriveItemColumns)) + GetFromText();
            commandText += GetJoinUserDataText(query);

            var whereClauses = GetWhereClauses(query, null);

            var whereText = whereClauses.Count == 0 ?
                string.Empty :
                " where " + string.Join(" AND ", whereClauses.ToArray());

            var whereTextWithoutPaging = whereText;

            commandText += whereText;

            commandText += GetGroupBy(query);

            commandText += GetOrderByText(query);

            if (query.Limit.HasValue || query.StartIndex.HasValue)
            {
                var offset = query.StartIndex ?? 0;

                if (query.Limit.HasValue || offset > 0)
                {
                    commandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                }

                if (offset > 0)
                {
                    commandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                }
            }

            var isReturningZeroItems = query.Limit.HasValue && query.Limit <= 0;

            var statementTexts = new List<string>();
            if (!isReturningZeroItems)
            {
                statementTexts.Add(commandText);
            }
            if (query.EnableTotalRecordCount)
            {
                commandText = string.Empty;

                if (EnableGroupByPresentationUniqueKey(query))
                {
                    commandText += " select count (distinct PresentationUniqueKey)" + GetFromText();
                }
                else
                {
                    commandText += " select count (guid)" + GetFromText();
                }

                commandText += GetJoinUserDataText(query);
                commandText += whereTextWithoutPaging;
                statementTexts.Add(commandText);
            }

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    return connection.RunInTransaction(db =>
                    {
                        var result = new QueryResult<BaseItem>();
                        var statements = PrepareAllSafe(db, statementTexts)
                            .ToList();

                        if (!isReturningZeroItems)
                        {
                            using (var statement = statements[0])
                            {
                                if (EnableJoinUserData(query))
                                {
                                    statement.TryBind("@UserId", query.User.Id);
                                }

                                BindSimilarParams(query, statement);

                                // Running this again will bind the params
                                GetWhereClauses(query, statement);

                                foreach (var row in statement.ExecuteQuery())
                                {
                                    var item = GetItem(row, query);
                                    if (item != null)
                                    {
                                        list.Add(item);
                                    }
                                }
                            }
                        }

                        if (query.EnableTotalRecordCount)
                        {
                            using (var statement = statements[statements.Count - 1])
                            {
                                if (EnableJoinUserData(query))
                                {
                                    statement.TryBind("@UserId", query.User.Id);
                                }

                                BindSimilarParams(query, statement);

                                // Running this again will bind the params
                                GetWhereClauses(query, statement);

                                result.TotalRecordCount = statement.ExecuteQuery().SelectScalarInt().First();
                            }
                        }

                        LogQueryTime("GetItems", commandText, now);

                        result.Items = list.ToArray();
                        return result;

                    }, ReadTransactionMode);
                }
            }
        }

        private string GetOrderByText(InternalItemsQuery query)
        {
            var orderBy = query.OrderBy.ToList();
            var enableOrderInversion = true;

            if (orderBy.Count == 0)
            {
                orderBy.AddRange(query.SortBy.Select(i => new Tuple<string, SortOrder>(i, query.SortOrder)));
            }
            else
            {
                enableOrderInversion = false;
            }

            if (query.SimilarTo != null)
            {
                if (orderBy.Count == 0)
                {
                    orderBy.Add(new Tuple<string, SortOrder>(ItemSortBy.Random, SortOrder.Ascending));
                    orderBy.Add(new Tuple<string, SortOrder>("SimilarityScore", SortOrder.Descending));
                    //orderBy.Add(new Tuple<string, SortOrder>(ItemSortBy.Random, SortOrder.Ascending));
                    query.SortOrder = SortOrder.Descending;
                    enableOrderInversion = false;
                }
            }

            query.OrderBy = orderBy;

            if (orderBy.Count == 0)
            {
                return string.Empty;
            }

            return " ORDER BY " + string.Join(",", orderBy.Select(i =>
            {
                var columnMap = MapOrderByField(i.Item1, query);
                var columnAscending = i.Item2 == SortOrder.Ascending;
                if (columnMap.Item2 && enableOrderInversion)
                {
                    columnAscending = !columnAscending;
                }

                var sortOrder = columnAscending ? "ASC" : "DESC";

                return columnMap.Item1 + " " + sortOrder;
            }).ToArray());
        }

        private Tuple<string, bool> MapOrderByField(string name, InternalItemsQuery query)
        {
            if (string.Equals(name, ItemSortBy.AirTime, StringComparison.OrdinalIgnoreCase))
            {
                // TODO
                return new Tuple<string, bool>("SortName", false);
            }
            if (string.Equals(name, ItemSortBy.Runtime, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("RuntimeTicks", false);
            }
            if (string.Equals(name, ItemSortBy.Random, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("RANDOM()", false);
            }
            if (string.Equals(name, ItemSortBy.DatePlayed, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("LastPlayedDate", false);
            }
            if (string.Equals(name, ItemSortBy.PlayCount, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("PlayCount", false);
            }
            if (string.Equals(name, ItemSortBy.IsFavoriteOrLiked, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("IsFavorite", true);
            }
            if (string.Equals(name, ItemSortBy.IsFolder, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("IsFolder", true);
            }
            if (string.Equals(name, ItemSortBy.IsPlayed, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("played", true);
            }
            if (string.Equals(name, ItemSortBy.IsUnplayed, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("played", false);
            }
            if (string.Equals(name, ItemSortBy.DateLastContentAdded, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("DateLastMediaAdded", false);
            }
            if (string.Equals(name, ItemSortBy.Artist, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("(select CleanValue from itemvalues where ItemId=Guid and Type=0 LIMIT 1)", false);
            }
            if (string.Equals(name, ItemSortBy.AlbumArtist, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("(select CleanValue from itemvalues where ItemId=Guid and Type=1 LIMIT 1)", false);
            }
            if (string.Equals(name, ItemSortBy.OfficialRating, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("InheritedParentalRatingValue", false);
            }
            if (string.Equals(name, ItemSortBy.Studio, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("(select CleanValue from itemvalues where ItemId=Guid and Type=3 LIMIT 1)", false);
            }
            if (string.Equals(name, ItemSortBy.SeriesDatePlayed, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("(Select MAX(LastPlayedDate) from TypedBaseItems B" + GetJoinUserDataText(query) + " where Played=1 and B.SeriesPresentationUniqueKey=A.PresentationUniqueKey)", false);
            }

            return new Tuple<string, bool>(name, false);
        }

        public List<Guid> GetItemIdsList(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();
            //Logger.Info("GetItemIdsList: " + _environmentInfo.StackTrace);

            var now = DateTime.UtcNow;

            var commandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "guid" })) + GetFromText();
            commandText += GetJoinUserDataText(query);

            var whereClauses = GetWhereClauses(query, null);

            var whereText = whereClauses.Count == 0 ?
                string.Empty :
                " where " + string.Join(" AND ", whereClauses.ToArray());

            commandText += whereText;

            commandText += GetGroupBy(query);

            commandText += GetOrderByText(query);

            if (query.Limit.HasValue || query.StartIndex.HasValue)
            {
                var offset = query.StartIndex ?? 0;

                if (query.Limit.HasValue || offset > 0)
                {
                    commandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                }

                if (offset > 0)
                {
                    commandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                }
            }

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var list = new List<Guid>();

                    using (var statement = PrepareStatementSafe(connection, commandText))
                    {
                        if (EnableJoinUserData(query))
                        {
                            statement.TryBind("@UserId", query.User.Id);
                        }

                        BindSimilarParams(query, statement);

                        // Running this again will bind the params
                        GetWhereClauses(query, statement);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            list.Add(row[0].ReadGuid());
                        }
                    }

                    LogQueryTime("GetItemList", commandText, now);

                    return list;
                }
            }
        }

        public List<Tuple<Guid, string>> GetItemIdsWithPath(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            var now = DateTime.UtcNow;

            var commandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "guid", "path" })) + GetFromText();

            var whereClauses = GetWhereClauses(query, null);

            var whereText = whereClauses.Count == 0 ?
                string.Empty :
                " where " + string.Join(" AND ", whereClauses.ToArray());

            commandText += whereText;

            commandText += GetGroupBy(query);

            commandText += GetOrderByText(query);

            if (query.Limit.HasValue || query.StartIndex.HasValue)
            {
                var offset = query.StartIndex ?? 0;

                if (query.Limit.HasValue || offset > 0)
                {
                    commandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                }

                if (offset > 0)
                {
                    commandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                }
            }

            var list = new List<Tuple<Guid, string>>();

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = PrepareStatementSafe(connection, commandText))
                    {
                        if (EnableJoinUserData(query))
                        {
                            statement.TryBind("@UserId", query.User.Id);
                        }

                        // Running this again will bind the params
                        GetWhereClauses(query, statement);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            var id = row.GetGuid(0);
                            string path = null;

                            if (!row.IsDBNull(1))
                            {
                                path = row.GetString(1);
                            }
                            list.Add(new Tuple<Guid, string>(id, path));
                        }
                    }
                }

                LogQueryTime("GetItemIdsWithPath", commandText, now);

                return list;
            }
        }

        public QueryResult<Guid> GetItemIds(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            if (!query.EnableTotalRecordCount || (!query.Limit.HasValue && (query.StartIndex ?? 0) == 0))
            {
                var returnList = GetItemIdsList(query);
                return new QueryResult<Guid>
                {
                    Items = returnList.ToArray(),
                    TotalRecordCount = returnList.Count
                };
            }
            //Logger.Info("GetItemIds: " + _environmentInfo.StackTrace);

            var now = DateTime.UtcNow;

            var commandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "guid" })) + GetFromText();
            commandText += GetJoinUserDataText(query);

            var whereClauses = GetWhereClauses(query, null);

            var whereText = whereClauses.Count == 0 ?
                string.Empty :
                " where " + string.Join(" AND ", whereClauses.ToArray());

            var whereTextWithoutPaging = whereText;

            commandText += whereText;

            commandText += GetGroupBy(query);

            commandText += GetOrderByText(query);

            if (query.Limit.HasValue || query.StartIndex.HasValue)
            {
                var offset = query.StartIndex ?? 0;

                if (query.Limit.HasValue || offset > 0)
                {
                    commandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                }

                if (offset > 0)
                {
                    commandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                }
            }

            var list = new List<Guid>();
            var isReturningZeroItems = query.Limit.HasValue && query.Limit <= 0;

            var statementTexts = new List<string>();
            if (!isReturningZeroItems)
            {
                statementTexts.Add(commandText);
            }
            if (query.EnableTotalRecordCount)
            {
                commandText = string.Empty;

                if (EnableGroupByPresentationUniqueKey(query))
                {
                    commandText += " select count (distinct PresentationUniqueKey)" + GetFromText();
                }
                else
                {
                    commandText += " select count (guid)" + GetFromText();
                }

                commandText += GetJoinUserDataText(query);
                commandText += whereTextWithoutPaging;
                statementTexts.Add(commandText);
            }

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    return connection.RunInTransaction(db =>
                    {
                        var result = new QueryResult<Guid>();

                        var statements = PrepareAllSafe(db, statementTexts)
                            .ToList();

                        if (!isReturningZeroItems)
                        {
                            using (var statement = statements[0])
                            {
                                if (EnableJoinUserData(query))
                                {
                                    statement.TryBind("@UserId", query.User.Id);
                                }

                                BindSimilarParams(query, statement);

                                // Running this again will bind the params
                                GetWhereClauses(query, statement);

                                foreach (var row in statement.ExecuteQuery())
                                {
                                    list.Add(row[0].ReadGuid());
                                }
                            }
                        }

                        if (query.EnableTotalRecordCount)
                        {
                            using (var statement = statements[statements.Count - 1])
                            {
                                if (EnableJoinUserData(query))
                                {
                                    statement.TryBind("@UserId", query.User.Id);
                                }

                                BindSimilarParams(query, statement);

                                // Running this again will bind the params
                                GetWhereClauses(query, statement);

                                result.TotalRecordCount = statement.ExecuteQuery().SelectScalarInt().First();
                            }
                        }

                        LogQueryTime("GetItemIds", commandText, now);

                        result.Items = list.ToArray();
                        return result;

                    }, ReadTransactionMode);
                }
            }
        }

        private List<string> GetWhereClauses(InternalItemsQuery query, IStatement statement, string paramSuffix = "")
        {
            if (query.IsResumable ?? false)
            {
                query.IsVirtualItem = false;
            }

            var whereClauses = new List<string>();

            if (EnableJoinUserData(query))
            {
                //whereClauses.Add("(UserId is null or UserId=@UserId)");
            }
            if (query.IsHD.HasValue)
            {
                whereClauses.Add("IsHD=@IsHD");
                if (statement != null)
                {
                    statement.TryBind("@IsHD", query.IsHD);
                }
            }
            if (query.IsLocked.HasValue)
            {
                whereClauses.Add("IsLocked=@IsLocked");
                if (statement != null)
                {
                    statement.TryBind("@IsLocked", query.IsLocked);
                }
            }

            var exclusiveProgramAttribtues = !(query.IsMovie ?? true) ||
                                             !(query.IsSports ?? true) ||
                                             !(query.IsKids ?? true) ||
                                             !(query.IsNews ?? true) ||
                                             !(query.IsSeries ?? true);

            if (exclusiveProgramAttribtues)
            {
                if (query.IsMovie.HasValue)
                {
                    var alternateTypes = new List<string>();
                    if (query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(typeof(Movie).Name))
                    {
                        alternateTypes.Add(typeof(Movie).FullName);
                    }
                    if (query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(typeof(Trailer).Name))
                    {
                        alternateTypes.Add(typeof(Trailer).FullName);
                    }

                    if (alternateTypes.Count == 0)
                    {
                        whereClauses.Add("IsMovie=@IsMovie");
                        if (statement != null)
                        {
                            statement.TryBind("@IsMovie", query.IsMovie);
                        }
                    }
                    else
                    {
                        whereClauses.Add("(IsMovie is null OR IsMovie=@IsMovie)");
                        if (statement != null)
                        {
                            statement.TryBind("@IsMovie", query.IsMovie);
                        }
                    }
                }
                if (query.IsSeries.HasValue)
                {
                    whereClauses.Add("IsSeries=@IsSeries");
                    if (statement != null)
                    {
                        statement.TryBind("@IsSeries", query.IsSeries);
                    }
                }
                if (query.IsNews.HasValue)
                {
                    whereClauses.Add("IsNews=@IsNews");
                    if (statement != null)
                    {
                        statement.TryBind("@IsNews", query.IsNews);
                    }
                }
                if (query.IsKids.HasValue)
                {
                    whereClauses.Add("IsKids=@IsKids");
                    if (statement != null)
                    {
                        statement.TryBind("@IsKids", query.IsKids);
                    }
                }
                if (query.IsSports.HasValue)
                {
                    whereClauses.Add("IsSports=@IsSports");
                    if (statement != null)
                    {
                        statement.TryBind("@IsSports", query.IsSports);
                    }
                }
            }
            else
            {
                var programAttribtues = new List<string>();
                if (query.IsMovie ?? false)
                {
                    var alternateTypes = new List<string>();
                    if (query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(typeof(Movie).Name))
                    {
                        alternateTypes.Add(typeof(Movie).FullName);
                    }
                    if (query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(typeof(Trailer).Name))
                    {
                        alternateTypes.Add(typeof(Trailer).FullName);
                    }

                    if (alternateTypes.Count == 0)
                    {
                        programAttribtues.Add("IsMovie=@IsMovie");
                    }
                    else
                    {
                        programAttribtues.Add("(IsMovie is null OR IsMovie=@IsMovie)");
                    }

                    if (statement != null)
                    {
                        statement.TryBind("@IsMovie", true);
                    }
                }
                if (query.IsSports ?? false)
                {
                    programAttribtues.Add("IsSports=@IsSports");
                    if (statement != null)
                    {
                        statement.TryBind("@IsSports", query.IsSports);
                    }
                }
                if (query.IsNews ?? false)
                {
                    programAttribtues.Add("IsNews=@IsNews");
                    if (statement != null)
                    {
                        statement.TryBind("@IsNews", query.IsNews);
                    }
                }
                if (query.IsSeries ?? false)
                {
                    programAttribtues.Add("IsSeries=@IsSeries");
                    if (statement != null)
                    {
                        statement.TryBind("@IsSeries", query.IsSeries);
                    }
                }
                if (query.IsKids ?? false)
                {
                    programAttribtues.Add("IsKids=@IsKids");
                    if (statement != null)
                    {
                        statement.TryBind("@IsKids", query.IsKids);
                    }
                }
                if (programAttribtues.Count > 0)
                {
                    whereClauses.Add("(" + string.Join(" OR ", programAttribtues.ToArray()) + ")");
                }
            }

            if (query.SimilarTo != null && query.MinSimilarityScore > 0)
            {
                whereClauses.Add("SimilarityScore > " + (query.MinSimilarityScore - 1).ToString(CultureInfo.InvariantCulture));
            }

            if (query.IsFolder.HasValue)
            {
                whereClauses.Add("IsFolder=@IsFolder");
                if (statement != null)
                {
                    statement.TryBind("@IsFolder", query.IsFolder);
                }
            }

            var includeTypes = query.IncludeItemTypes.SelectMany(MapIncludeItemTypes).ToArray();
            if (includeTypes.Length == 1)
            {
                whereClauses.Add("type=@type" + paramSuffix);
                if (statement != null)
                {
                    statement.TryBind("@type" + paramSuffix, includeTypes[0]);
                }
            }
            else if (includeTypes.Length > 1)
            {
                var inClause = string.Join(",", includeTypes.Select(i => "'" + i + "'").ToArray());
                whereClauses.Add(string.Format("type in ({0})", inClause));
            }

            var excludeTypes = query.ExcludeItemTypes.SelectMany(MapIncludeItemTypes).ToArray();
            if (excludeTypes.Length == 1)
            {
                whereClauses.Add("type<>@type");
                if (statement != null)
                {
                    statement.TryBind("@type", excludeTypes[0]);
                }
            }
            else if (excludeTypes.Length > 1)
            {
                var inClause = string.Join(",", excludeTypes.Select(i => "'" + i + "'").ToArray());
                whereClauses.Add(string.Format("type not in ({0})", inClause));
            }

            if (query.ChannelIds.Length == 1)
            {
                whereClauses.Add("ChannelId=@ChannelId");
                if (statement != null)
                {
                    statement.TryBind("@ChannelId", query.ChannelIds[0]);
                }
            }
            if (query.ChannelIds.Length > 1)
            {
                var inClause = string.Join(",", query.ChannelIds.Select(i => "'" + i + "'").ToArray());
                whereClauses.Add(string.Format("ChannelId in ({0})", inClause));
            }

            if (query.ParentId.HasValue)
            {
                whereClauses.Add("ParentId=@ParentId");
                if (statement != null)
                {
                    statement.TryBind("@ParentId", query.ParentId.Value);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.Path))
            {
                whereClauses.Add("Path=@Path");
                if (statement != null)
                {
                    statement.TryBind("@Path", query.Path);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.PresentationUniqueKey))
            {
                whereClauses.Add("PresentationUniqueKey=@PresentationUniqueKey");
                if (statement != null)
                {
                    statement.TryBind("@PresentationUniqueKey", query.PresentationUniqueKey);
                }
            }

            if (query.MinCommunityRating.HasValue)
            {
                whereClauses.Add("CommunityRating>=@MinCommunityRating");
                if (statement != null)
                {
                    statement.TryBind("@MinCommunityRating", query.MinCommunityRating.Value);
                }
            }

            if (query.MinIndexNumber.HasValue)
            {
                whereClauses.Add("IndexNumber>=@MinIndexNumber");
                if (statement != null)
                {
                    statement.TryBind("@MinIndexNumber", query.MinIndexNumber.Value);
                }
            }

            if (query.MinDateCreated.HasValue)
            {
                whereClauses.Add("DateCreated>=@MinDateCreated");
                if (statement != null)
                {
                    statement.TryBind("@MinDateCreated", query.MinDateCreated.Value);
                }
            }

            if (query.MinDateLastSaved.HasValue)
            {
                whereClauses.Add("DateLastSaved>=@MinDateLastSaved");
                if (statement != null)
                {
                    statement.TryBind("@MinDateLastSaved", query.MinDateLastSaved.Value);
                }
            }

            //if (query.MinPlayers.HasValue)
            //{
            //    whereClauses.Add("Players>=@MinPlayers");
            //    cmd.Parameters.Add(cmd, "@MinPlayers", DbType.Int32).Value = query.MinPlayers.Value;
            //}

            //if (query.MaxPlayers.HasValue)
            //{
            //    whereClauses.Add("Players<=@MaxPlayers");
            //    cmd.Parameters.Add(cmd, "@MaxPlayers", DbType.Int32).Value = query.MaxPlayers.Value;
            //}

            if (query.IndexNumber.HasValue)
            {
                whereClauses.Add("IndexNumber=@IndexNumber");
                if (statement != null)
                {
                    statement.TryBind("@IndexNumber", query.IndexNumber.Value);
                }
            }
            if (query.ParentIndexNumber.HasValue)
            {
                whereClauses.Add("ParentIndexNumber=@ParentIndexNumber");
                if (statement != null)
                {
                    statement.TryBind("@ParentIndexNumber", query.ParentIndexNumber.Value);
                }
            }
            if (query.ParentIndexNumberNotEquals.HasValue)
            {
                whereClauses.Add("(ParentIndexNumber<>@ParentIndexNumberNotEquals or ParentIndexNumber is null)");
                if (statement != null)
                {
                    statement.TryBind("@ParentIndexNumberNotEquals", query.ParentIndexNumberNotEquals.Value);
                }
            }
            if (query.MinEndDate.HasValue)
            {
                whereClauses.Add("EndDate>=@MinEndDate");
                if (statement != null)
                {
                    statement.TryBind("@MinEndDate", query.MinEndDate.Value);
                }
            }

            if (query.MaxEndDate.HasValue)
            {
                whereClauses.Add("EndDate<=@MaxEndDate");
                if (statement != null)
                {
                    statement.TryBind("@MaxEndDate", query.MaxEndDate.Value);
                }
            }

            if (query.MinStartDate.HasValue)
            {
                whereClauses.Add("StartDate>=@MinStartDate");
                if (statement != null)
                {
                    statement.TryBind("@MinStartDate", query.MinStartDate.Value);
                }
            }

            if (query.MaxStartDate.HasValue)
            {
                whereClauses.Add("StartDate<=@MaxStartDate");
                if (statement != null)
                {
                    statement.TryBind("@MaxStartDate", query.MaxStartDate.Value);
                }
            }

            if (query.MinPremiereDate.HasValue)
            {
                whereClauses.Add("PremiereDate>=@MinPremiereDate");
                if (statement != null)
                {
                    statement.TryBind("@MinPremiereDate", query.MinPremiereDate.Value);
                }
            }
            if (query.MaxPremiereDate.HasValue)
            {
                whereClauses.Add("PremiereDate<=@MaxPremiereDate");
                if (statement != null)
                {
                    statement.TryBind("@MaxPremiereDate", query.MaxPremiereDate.Value);
                }
            }

            if (query.SourceTypes.Length == 1)
            {
                whereClauses.Add("SourceType=@SourceType");
                if (statement != null)
                {
                    statement.TryBind("@SourceType", query.SourceTypes[0].ToString());
                }
            }
            else if (query.SourceTypes.Length > 1)
            {
                var inClause = string.Join(",", query.SourceTypes.Select(i => "'" + i + "'").ToArray());
                whereClauses.Add(string.Format("SourceType in ({0})", inClause));
            }

            if (query.ExcludeSourceTypes.Length == 1)
            {
                whereClauses.Add("SourceType<>@ExcludeSourceTypes");
                if (statement != null)
                {
                    statement.TryBind("@ExcludeSourceTypes", query.ExcludeSourceTypes[0].ToString());
                }
            }
            else if (query.ExcludeSourceTypes.Length > 1)
            {
                var inClause = string.Join(",", query.ExcludeSourceTypes.Select(i => "'" + i + "'").ToArray());
                whereClauses.Add(string.Format("SourceType not in ({0})", inClause));
            }

            if (query.TrailerTypes.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var type in query.TrailerTypes)
                {
                    var paramName = "@TrailerTypes" + index;

                    clauses.Add("TrailerTypes like " + paramName);
                    if (statement != null)
                    {
                        statement.TryBind(paramName, "%" + type + "%");
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.IsAiring.HasValue)
            {
                if (query.IsAiring.Value)
                {
                    whereClauses.Add("StartDate<=@MaxStartDate");
                    if (statement != null)
                    {
                        statement.TryBind("@MaxStartDate", DateTime.UtcNow);
                    }

                    whereClauses.Add("EndDate>=@MinEndDate");
                    if (statement != null)
                    {
                        statement.TryBind("@MinEndDate", DateTime.UtcNow);
                    }
                }
                else
                {
                    whereClauses.Add("(StartDate>@IsAiringDate OR EndDate < @IsAiringDate)");
                    if (statement != null)
                    {
                        statement.TryBind("@IsAiringDate", DateTime.UtcNow);
                    }
                }
            }

            if (query.PersonIds.Length > 0)
            {
                // TODO: Should this query with CleanName ? 

                var clauses = new List<string>();
                var index = 0;
                foreach (var personId in query.PersonIds)
                {
                    var paramName = "@PersonId" + index;

                    clauses.Add("(select Name from TypedBaseItems where guid=" + paramName + ") in (select Name from People where ItemId=Guid)");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, personId.ToGuidParamValue());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (!string.IsNullOrWhiteSpace(query.Person))
            {
                whereClauses.Add("Guid in (select ItemId from People where Name=@PersonName)");
                if (statement != null)
                {
                    statement.TryBind("@PersonName", query.Person);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.SlugName))
            {
                whereClauses.Add("SlugName=@SlugName");
                if (statement != null)
                {
                    statement.TryBind("@SlugName", query.SlugName);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.MinSortName))
            {
                whereClauses.Add("SortName>=@MinSortName");
                if (statement != null)
                {
                    statement.TryBind("@MinSortName", query.MinSortName);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.ExternalSeriesId))
            {
                whereClauses.Add("ExternalSeriesId=@ExternalSeriesId");
                if (statement != null)
                {
                    statement.TryBind("@ExternalSeriesId", query.ExternalSeriesId);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.ExternalId))
            {
                whereClauses.Add("ExternalId=@ExternalId");
                if (statement != null)
                {
                    statement.TryBind("@ExternalId", query.ExternalId);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                whereClauses.Add("CleanName=@Name");

                if (statement != null)
                {
                    statement.TryBind("@Name", GetCleanValue(query.Name));
                }
            }

            if (!string.IsNullOrWhiteSpace(query.NameContains))
            {
                whereClauses.Add("CleanName like @NameContains");
                if (statement != null)
                {
                    statement.TryBind("@NameContains", "%" + GetCleanValue(query.NameContains) + "%");
                }
            }
            if (!string.IsNullOrWhiteSpace(query.NameStartsWith))
            {
                whereClauses.Add("SortName like @NameStartsWith");
                if (statement != null)
                {
                    statement.TryBind("@NameStartsWith", query.NameStartsWith + "%");
                }
            }
            if (!string.IsNullOrWhiteSpace(query.NameStartsWithOrGreater))
            {
                whereClauses.Add("SortName >= @NameStartsWithOrGreater");
                // lowercase this because SortName is stored as lowercase
                if (statement != null)
                {
                    statement.TryBind("@NameStartsWithOrGreater", query.NameStartsWithOrGreater.ToLower());
                }
            }
            if (!string.IsNullOrWhiteSpace(query.NameLessThan))
            {
                whereClauses.Add("SortName < @NameLessThan");
                // lowercase this because SortName is stored as lowercase
                if (statement != null)
                {
                    statement.TryBind("@NameLessThan", query.NameLessThan.ToLower());
                }
            }

            if (query.ImageTypes.Length > 0)
            {
                foreach (var requiredImage in query.ImageTypes)
                {
                    whereClauses.Add("Images like '%" + requiredImage + "%'");
                }
            }

            if (query.IsLiked.HasValue)
            {
                if (query.IsLiked.Value)
                {
                    whereClauses.Add("rating>=@UserRating");
                    if (statement != null)
                    {
                        statement.TryBind("@UserRating", UserItemData.MinLikeValue);
                    }
                }
                else
                {
                    whereClauses.Add("(rating is null or rating<@UserRating)");
                    if (statement != null)
                    {
                        statement.TryBind("@UserRating", UserItemData.MinLikeValue);
                    }
                }
            }

            if (query.IsFavoriteOrLiked.HasValue)
            {
                if (query.IsFavoriteOrLiked.Value)
                {
                    whereClauses.Add("IsFavorite=@IsFavoriteOrLiked");
                }
                else
                {
                    whereClauses.Add("(IsFavorite is null or IsFavorite=@IsFavoriteOrLiked)");
                }
                if (statement != null)
                {
                    statement.TryBind("@IsFavoriteOrLiked", query.IsFavoriteOrLiked.Value);
                }
            }

            if (query.IsFavorite.HasValue)
            {
                if (query.IsFavorite.Value)
                {
                    whereClauses.Add("IsFavorite=@IsFavorite");
                }
                else
                {
                    whereClauses.Add("(IsFavorite is null or IsFavorite=@IsFavorite)");
                }
                if (statement != null)
                {
                    statement.TryBind("@IsFavorite", query.IsFavorite.Value);
                }
            }

            if (EnableJoinUserData(query))
            {
                if (query.IsPlayed.HasValue)
                {
                    if (query.IsPlayed.Value)
                    {
                        whereClauses.Add("(played=@IsPlayed)");
                    }
                    else
                    {
                        whereClauses.Add("(played is null or played=@IsPlayed)");
                    }
                    if (statement != null)
                    {
                        statement.TryBind("@IsPlayed", query.IsPlayed.Value);
                    }
                }
            }

            if (query.IsResumable.HasValue)
            {
                if (query.IsResumable.Value)
                {
                    whereClauses.Add("playbackPositionTicks > 0");
                }
                else
                {
                    whereClauses.Add("(playbackPositionTicks is null or playbackPositionTicks = 0)");
                }
            }

            if (query.ArtistIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var artistId in query.ArtistIds)
                {
                    var paramName = "@ArtistIds" + index;

                    clauses.Add("(select CleanName from TypedBaseItems where guid=" + paramName + ") in (select CleanValue from itemvalues where ItemId=Guid and Type<=1)");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, artistId.ToGuidParamValue());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.AlbumIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var albumId in query.AlbumIds)
                {
                    var paramName = "@AlbumIds" + index;

                    clauses.Add("Album in (select Name from typedbaseitems where guid=" + paramName + ")");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, albumId.ToGuidParamValue());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.ExcludeArtistIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var artistId in query.ExcludeArtistIds)
                {
                    var paramName = "@ExcludeArtistId" + index;

                    clauses.Add("(select CleanName from TypedBaseItems where guid=" + paramName + ") not in (select CleanValue from itemvalues where ItemId=Guid and Type<=1)");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, artistId.ToGuidParamValue());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.GenreIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var genreId in query.GenreIds)
                {
                    var paramName = "@GenreId" + index;

                    clauses.Add("(select CleanName from TypedBaseItems where guid=" + paramName + ") in (select CleanValue from itemvalues where ItemId=Guid and Type=2)");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, genreId.ToGuidParamValue());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.Genres.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in query.Genres)
                {
                    clauses.Add("@Genre" + index + " in (select CleanValue from itemvalues where ItemId=Guid and Type=2)");
                    if (statement != null)
                    {
                        statement.TryBind("@Genre" + index, GetCleanValue(item));
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.Tags.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in query.Tags)
                {
                    clauses.Add("@Tag" + index + " in (select CleanValue from itemvalues where ItemId=Guid and Type=4)");
                    if (statement != null)
                    {
                        statement.TryBind("@Tag" + index, GetCleanValue(item));
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.StudioIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var studioId in query.StudioIds)
                {
                    var paramName = "@StudioId" + index;

                    clauses.Add("(select CleanName from TypedBaseItems where guid=" + paramName + ") in (select CleanValue from itemvalues where ItemId=Guid and Type=3)");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, studioId.ToGuidParamValue());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.Keywords.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in query.Keywords)
                {
                    clauses.Add("@Keyword" + index + " in (select CleanValue from itemvalues where ItemId=Guid and Type=5)");
                    if (statement != null)
                    {
                        statement.TryBind("@Keyword" + index, GetCleanValue(item));
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.OfficialRatings.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in query.OfficialRatings)
                {
                    clauses.Add("OfficialRating=@OfficialRating" + index);
                    if (statement != null)
                    {
                        statement.TryBind("@OfficialRating" + index, item);
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.MinParentalRating.HasValue)
            {
                whereClauses.Add("InheritedParentalRatingValue<=@MinParentalRating");
                if (statement != null)
                {
                    statement.TryBind("@MinParentalRating", query.MinParentalRating.Value);
                }
            }

            if (query.MaxParentalRating.HasValue)
            {
                whereClauses.Add("InheritedParentalRatingValue<=@MaxParentalRating");
                if (statement != null)
                {
                    statement.TryBind("@MaxParentalRating", query.MaxParentalRating.Value);
                }
            }

            if (query.HasParentalRating.HasValue)
            {
                if (query.HasParentalRating.Value)
                {
                    whereClauses.Add("InheritedParentalRatingValue > 0");
                }
                else
                {
                    whereClauses.Add("InheritedParentalRatingValue = 0");
                }
            }

            if (query.HasOverview.HasValue)
            {
                if (query.HasOverview.Value)
                {
                    whereClauses.Add("(Overview not null AND Overview<>'')");
                }
                else
                {
                    whereClauses.Add("(Overview is null OR Overview='')");
                }
            }

            if (query.HasDeadParentId.HasValue)
            {
                if (query.HasDeadParentId.Value)
                {
                    whereClauses.Add("ParentId NOT NULL AND ParentId NOT IN (select guid from TypedBaseItems)");
                }
            }

            if (query.Years.Length == 1)
            {
                whereClauses.Add("ProductionYear=@Years");
                if (statement != null)
                {
                    statement.TryBind("@Years", query.Years[0].ToString());
                }
            }
            else if (query.Years.Length > 1)
            {
                var val = string.Join(",", query.Years.ToArray());

                whereClauses.Add("ProductionYear in (" + val + ")");
            }

            if (query.LocationTypes.Length == 1)
            {
                if (query.LocationTypes[0] == LocationType.Virtual && _config.Configuration.SchemaVersion >= 90)
                {
                    query.IsVirtualItem = true;
                }
                else
                {
                    whereClauses.Add("LocationType=@LocationType");
                    if (statement != null)
                    {
                        statement.TryBind("@LocationType", query.LocationTypes[0].ToString());
                    }
                }
            }
            else if (query.LocationTypes.Length > 1)
            {
                var val = string.Join(",", query.LocationTypes.Select(i => "'" + i + "'").ToArray());

                whereClauses.Add("LocationType in (" + val + ")");
            }
            if (query.IsVirtualItem.HasValue)
            {
                whereClauses.Add("IsVirtualItem=@IsVirtualItem");
                if (statement != null)
                {
                    statement.TryBind("@IsVirtualItem", query.IsVirtualItem.Value);
                }
            }
            if (query.IsSpecialSeason.HasValue)
            {
                if (query.IsSpecialSeason.Value)
                {
                    whereClauses.Add("IndexNumber = 0");
                }
                else
                {
                    whereClauses.Add("IndexNumber <> 0");
                }
            }
            if (query.IsUnaired.HasValue)
            {
                if (query.IsUnaired.Value)
                {
                    whereClauses.Add("PremiereDate >= DATETIME('now')");
                }
                else
                {
                    whereClauses.Add("PremiereDate < DATETIME('now')");
                }
            }
            if (query.IsMissing.HasValue)
            {
                if (query.IsMissing.Value)
                {
                    whereClauses.Add("(IsVirtualItem=1 AND PremiereDate < DATETIME('now'))");
                }
                else
                {
                    whereClauses.Add("(IsVirtualItem=0 OR PremiereDate >= DATETIME('now'))");
                }
            }
            if (query.IsVirtualUnaired.HasValue)
            {
                if (query.IsVirtualUnaired.Value)
                {
                    whereClauses.Add("(IsVirtualItem=1 AND PremiereDate >= DATETIME('now'))");
                }
                else
                {
                    whereClauses.Add("(IsVirtualItem=0 OR PremiereDate < DATETIME('now'))");
                }
            }
            if (query.MediaTypes.Length == 1)
            {
                whereClauses.Add("MediaType=@MediaTypes");
                if (statement != null)
                {
                    statement.TryBind("@MediaTypes", query.MediaTypes[0]);
                }
            }
            if (query.MediaTypes.Length > 1)
            {
                var val = string.Join(",", query.MediaTypes.Select(i => "'" + i + "'").ToArray());

                whereClauses.Add("MediaType in (" + val + ")");
            }
            if (query.ItemIds.Length > 0)
            {
                var includeIds = new List<string>();

                var index = 0;
                foreach (var id in query.ItemIds)
                {
                    includeIds.Add("Guid = @IncludeId" + index);
                    if (statement != null)
                    {
                        statement.TryBind("@IncludeId" + index, new Guid(id));
                    }
                    index++;
                }

                whereClauses.Add(string.Join(" OR ", includeIds.ToArray()));
            }
            if (query.ExcludeItemIds.Length > 0)
            {
                var excludeIds = new List<string>();

                var index = 0;
                foreach (var id in query.ExcludeItemIds)
                {
                    excludeIds.Add("Guid <> @ExcludeId" + index);
                    if (statement != null)
                    {
                        statement.TryBind("@ExcludeId" + index, new Guid(id));
                    }
                    index++;
                }

                whereClauses.Add(string.Join(" AND ", excludeIds.ToArray()));
            }

            if (query.ExcludeProviderIds.Count > 0)
            {
                var excludeIds = new List<string>();

                var index = 0;
                foreach (var pair in query.ExcludeProviderIds)
                {
                    if (string.Equals(pair.Key, MetadataProviders.TmdbCollection.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var paramName = "@ExcludeProviderId" + index;
                    //excludeIds.Add("(COALESCE((select value from ProviderIds where ItemId=Guid and Name = '" + pair.Key + "'), '') <> " + paramName + ")");
                    excludeIds.Add("(ProviderIds is null or ProviderIds not like " + paramName + ")");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, "%" + pair.Key + "=" + pair.Value + "%");
                    }
                    index++;

                    break;
                }

                whereClauses.Add(string.Join(" AND ", excludeIds.ToArray()));
            }

            if (query.HasImdbId.HasValue)
            {
                whereClauses.Add("ProviderIds like '%imdb=%'");
            }

            if (query.HasTmdbId.HasValue)
            {
                whereClauses.Add("ProviderIds like '%tmdb=%'");
            }

            if (query.HasTvdbId.HasValue)
            {
                whereClauses.Add("ProviderIds like '%tvdb=%'");
            }
            if (query.HasThemeSong.HasValue)
            {
                if (query.HasThemeSong.Value)
                {
                    whereClauses.Add("ThemeSongIds not null");
                }
                else
                {
                    whereClauses.Add("ThemeSongIds is null");
                }
            }
            if (query.HasThemeVideo.HasValue)
            {
                if (query.HasThemeVideo.Value)
                {
                    whereClauses.Add("ThemeVideoIds not null");
                }
                else
                {
                    whereClauses.Add("ThemeVideoIds is null");
                }
            }

            //var enableItemsByName = query.IncludeItemsByName ?? query.IncludeItemTypes.Length > 0;
            var enableItemsByName = query.IncludeItemsByName ?? false;

            if (query.TopParentIds.Length == 1)
            {
                if (enableItemsByName)
                {
                    whereClauses.Add("(TopParentId=@TopParentId or IsItemByName=@IsItemByName)");
                    if (statement != null)
                    {
                        statement.TryBind("@IsItemByName", true);
                    }
                }
                else
                {
                    whereClauses.Add("(TopParentId=@TopParentId)");
                }
                if (statement != null)
                {
                    statement.TryBind("@TopParentId", query.TopParentIds[0]);
                }
            }
            if (query.TopParentIds.Length > 1)
            {
                var val = string.Join(",", query.TopParentIds.Select(i => "'" + i + "'").ToArray());

                if (enableItemsByName)
                {
                    whereClauses.Add("(IsItemByName=@IsItemByName or TopParentId in (" + val + "))");
                    if (statement != null)
                    {
                        statement.TryBind("@IsItemByName", true);
                    }
                }
                else
                {
                    whereClauses.Add("(TopParentId in (" + val + "))");
                }
            }

            if (query.AncestorIds.Length == 1)
            {
                whereClauses.Add("Guid in (select itemId from AncestorIds where AncestorId=@AncestorId)");

                if (statement != null)
                {
                    statement.TryBind("@AncestorId", new Guid(query.AncestorIds[0]));
                }
            }
            if (query.AncestorIds.Length > 1)
            {
                var inClause = string.Join(",", query.AncestorIds.Select(i => "'" + new Guid(i).ToString("N") + "'").ToArray());
                whereClauses.Add(string.Format("Guid in (select itemId from AncestorIds where AncestorIdText in ({0}))", inClause));
            }
            if (!string.IsNullOrWhiteSpace(query.AncestorWithPresentationUniqueKey))
            {
                var inClause = "select guid from TypedBaseItems where PresentationUniqueKey=@AncestorWithPresentationUniqueKey";
                whereClauses.Add(string.Format("Guid in (select itemId from AncestorIds where AncestorId in ({0}))", inClause));
                if (statement != null)
                {
                    statement.TryBind("@AncestorWithPresentationUniqueKey", query.AncestorWithPresentationUniqueKey);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.SeriesPresentationUniqueKey))
            {
                whereClauses.Add("SeriesPresentationUniqueKey=@SeriesPresentationUniqueKey");

                if (statement != null)
                {
                    statement.TryBind("@SeriesPresentationUniqueKey", query.SeriesPresentationUniqueKey);
                }
            }

            if (query.BlockUnratedItems.Length == 1)
            {
                whereClauses.Add("(InheritedParentalRatingValue > 0 or UnratedType <> @UnratedType)");
                if (statement != null)
                {
                    statement.TryBind("@UnratedType", query.BlockUnratedItems[0].ToString());
                }
            }
            if (query.BlockUnratedItems.Length > 1)
            {
                var inClause = string.Join(",", query.BlockUnratedItems.Select(i => "'" + i.ToString() + "'").ToArray());
                whereClauses.Add(string.Format("(InheritedParentalRatingValue > 0 or UnratedType not in ({0}))", inClause));
            }

            var excludeTagIndex = 0;
            foreach (var excludeTag in query.ExcludeTags)
            {
                whereClauses.Add("(Tags is null OR Tags not like @excludeTag" + excludeTagIndex + ")");
                if (statement != null)
                {
                    statement.TryBind("@excludeTag" + excludeTagIndex, "%" + excludeTag + "%");
                }
                excludeTagIndex++;
            }

            excludeTagIndex = 0;
            foreach (var excludeTag in query.ExcludeInheritedTags)
            {
                whereClauses.Add("(InheritedTags is null OR InheritedTags not like @excludeInheritedTag" + excludeTagIndex + ")");
                if (statement != null)
                {
                    statement.TryBind("@excludeInheritedTag" + excludeTagIndex, "%" + excludeTag + "%");
                }
                excludeTagIndex++;
            }

            return whereClauses;
        }

        private string GetCleanValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return value.RemoveDiacritics().ToLower();
        }

        private bool EnableGroupByPresentationUniqueKey(InternalItemsQuery query)
        {
            if (!query.GroupByPresentationUniqueKey)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(query.PresentationUniqueKey))
            {
                return false;
            }

            if (query.User == null)
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            var types = new[] {
                typeof(Episode).Name,
                typeof(Video).Name ,
                typeof(Movie).Name ,
                typeof(MusicVideo).Name ,
                typeof(Series).Name ,
                typeof(Season).Name };

            if (types.Any(i => query.IncludeItemTypes.Contains(i, StringComparer.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private static readonly Type[] KnownTypes =
        {
            typeof(LiveTvProgram),
            typeof(LiveTvChannel),
            typeof(LiveTvVideoRecording),
            typeof(LiveTvAudioRecording),
            typeof(Series),
            typeof(Audio),
            typeof(MusicAlbum),
            typeof(MusicArtist),
            typeof(MusicGenre),
            typeof(MusicVideo),
            typeof(Movie),
            typeof(Playlist),
            typeof(AudioPodcast),
            typeof(AudioBook),
            typeof(Trailer),
            typeof(BoxSet),
            typeof(Episode),
            typeof(Season),
            typeof(Series),
            typeof(Book),
            typeof(CollectionFolder),
            typeof(Folder),
            typeof(Game),
            typeof(GameGenre),
            typeof(GameSystem),
            typeof(Genre),
            typeof(Person),
            typeof(Photo),
            typeof(PhotoAlbum),
            typeof(Studio),
            typeof(UserRootFolder),
            typeof(UserView),
            typeof(Video),
            typeof(Year),
            typeof(Channel),
            typeof(AggregateFolder)
        };

        public async Task UpdateInheritedValues(CancellationToken cancellationToken)
        {
            await UpdateInheritedTags(cancellationToken).ConfigureAwait(false);
        }

        private async Task UpdateInheritedTags(CancellationToken cancellationToken)
        {
            var newValues = new List<Tuple<Guid, string>>();

            var commandText = "select Guid,InheritedTags,(select group_concat(Tags, '|') from TypedBaseItems where (guid=outer.guid) OR (guid in (Select AncestorId from AncestorIds where ItemId=Outer.guid))) as NewInheritedTags from typedbaseitems as Outer where NewInheritedTags <> InheritedTags";

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    foreach (var row in connection.Query(commandText))
                    {
                        var id = row.GetGuid(0);
                        string value = row.IsDBNull(2) ? null : row.GetString(2);

                        newValues.Add(new Tuple<Guid, string>(id, value));
                    }

                    Logger.Debug("UpdateInheritedTags - {0} rows", newValues.Count);
                    if (newValues.Count == 0)
                    {
                        return;
                    }

                    // write lock here
                    using (var statement = PrepareStatement(connection, "Update TypedBaseItems set InheritedTags=@InheritedTags where Guid=@Guid"))
                    {
                        foreach (var item in newValues)
                        {
                            var paramList = new List<object>();

                            paramList.Add(item.Item1);
                            paramList.Add(item.Item2);

                            statement.Execute(paramList.ToArray());
                        }
                    }
                }
            }
        }

        private static Dictionary<string, string[]> GetTypeMapDictionary()
        {
            var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            foreach (var t in KnownTypes)
            {
                dict[t.Name] = new[] { t.FullName };
            }

            dict["Recording"] = new[] { typeof(LiveTvAudioRecording).FullName, typeof(LiveTvVideoRecording).FullName };
            dict["Program"] = new[] { typeof(LiveTvProgram).FullName };
            dict["TvChannel"] = new[] { typeof(LiveTvChannel).FullName };

            return dict;
        }

        // Not crazy about having this all the way down here, but at least it's in one place
        readonly Dictionary<string, string[]> _types = GetTypeMapDictionary();

        private IEnumerable<string> MapIncludeItemTypes(string value)
        {
            string[] result;
            if (_types.TryGetValue(value, out result))
            {
                return result;
            }

            return new[] { value };
        }

        public async Task DeleteItem(Guid id, CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            CheckDisposed();

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        // Delete people
                        ExecuteWithSingleParam(db, "delete from People where ItemId=@Id", id.ToGuidParamValue());

                        // Delete chapters
                        ExecuteWithSingleParam(db, "delete from " + ChaptersTableName + " where ItemId=@Id", id.ToGuidParamValue());

                        // Delete media streams
                        ExecuteWithSingleParam(db, "delete from mediastreams where ItemId=@Id", id.ToGuidParamValue());

                        // Delete ancestors
                        ExecuteWithSingleParam(db, "delete from AncestorIds where ItemId=@Id", id.ToGuidParamValue());

                        // Delete item values
                        ExecuteWithSingleParam(db, "delete from ItemValues where ItemId=@Id", id.ToGuidParamValue());

                        // Delete the item
                        ExecuteWithSingleParam(db, "delete from TypedBaseItems where guid=@Id", id.ToGuidParamValue());
                    }, TransactionMode);
                }
            }
        }

        private void ExecuteWithSingleParam(IDatabaseConnection db, string query, byte[] value)
        {
            using (var statement = PrepareStatement(db, query))
            {
                statement.TryBind("@Id", value);

                statement.MoveNext();
            }
        }

        public List<string> GetPeopleNames(InternalPeopleQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            var commandText = "select Distinct Name from People";

            var whereClauses = GetPeopleWhereClauses(query, null);

            if (whereClauses.Count > 0)
            {
                commandText += "  where " + string.Join(" AND ", whereClauses.ToArray());
            }

            commandText += " order by ListOrder";

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var list = new List<string>();
                    using (var statement = PrepareStatementSafe(connection, commandText))
                    {
                        // Run this again to bind the params
                        GetPeopleWhereClauses(query, statement);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            list.Add(row.GetString(0));
                        }
                    }
                    return list;
                }
            }
        }

        public List<PersonInfo> GetPeople(InternalPeopleQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            var commandText = "select ItemId, Name, Role, PersonType, SortOrder from People";

            var whereClauses = GetPeopleWhereClauses(query, null);

            if (whereClauses.Count > 0)
            {
                commandText += "  where " + string.Join(" AND ", whereClauses.ToArray());
            }

            commandText += " order by ListOrder";

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var list = new List<PersonInfo>();

                    using (var statement = PrepareStatementSafe(connection, commandText))
                    {
                        // Run this again to bind the params
                        GetPeopleWhereClauses(query, statement);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            list.Add(GetPerson(row));
                        }
                    }

                    return list;
                }
            }
        }

        private List<string> GetPeopleWhereClauses(InternalPeopleQuery query, IStatement statement)
        {
            var whereClauses = new List<string>();

            if (query.ItemId != Guid.Empty)
            {
                whereClauses.Add("ItemId=@ItemId");
                if (statement != null)
                {
                    statement.TryBind("@ItemId", query.ItemId.ToGuidParamValue());
                }
            }
            if (query.AppearsInItemId != Guid.Empty)
            {
                whereClauses.Add("Name in (Select Name from People where ItemId=@AppearsInItemId)");
                if (statement != null)
                {
                    statement.TryBind("@AppearsInItemId", query.AppearsInItemId.ToGuidParamValue());
                }
            }
            if (query.PersonTypes.Count == 1)
            {
                whereClauses.Add("PersonType=@PersonType");
                if (statement != null)
                {
                    statement.TryBind("@PersonType", query.PersonTypes[0]);
                }
            }
            if (query.PersonTypes.Count > 1)
            {
                var val = string.Join(",", query.PersonTypes.Select(i => "'" + i + "'").ToArray());

                whereClauses.Add("PersonType in (" + val + ")");
            }
            if (query.ExcludePersonTypes.Count == 1)
            {
                whereClauses.Add("PersonType<>@PersonType");
                if (statement != null)
                {
                    statement.TryBind("@PersonType", query.ExcludePersonTypes[0]);
                }
            }
            if (query.ExcludePersonTypes.Count > 1)
            {
                var val = string.Join(",", query.ExcludePersonTypes.Select(i => "'" + i + "'").ToArray());

                whereClauses.Add("PersonType not in (" + val + ")");
            }
            if (query.MaxListOrder.HasValue)
            {
                whereClauses.Add("ListOrder<=@MaxListOrder");
                if (statement != null)
                {
                    statement.TryBind("@MaxListOrder", query.MaxListOrder.Value);
                }
            }
            if (!string.IsNullOrWhiteSpace(query.NameContains))
            {
                whereClauses.Add("Name like @NameContains");
                if (statement != null)
                {
                    statement.TryBind("@NameContains", "%" + query.NameContains + "%");
                }
            }
            if (query.SourceTypes.Length == 1)
            {
                whereClauses.Add("(select sourcetype from typedbaseitems where guid=ItemId) = @SourceTypes");
                if (statement != null)
                {
                    statement.TryBind("@SourceTypes", query.SourceTypes[0].ToString());
                }
            }

            return whereClauses;
        }

        private void UpdateAncestors(Guid itemId, List<Guid> ancestorIds, IDatabaseConnection db, IStatement deleteAncestorsStatement, IStatement updateAncestorsStatement)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentNullException("itemId");
            }

            if (ancestorIds == null)
            {
                throw new ArgumentNullException("ancestorIds");
            }

            CheckDisposed();

            // First delete 
            deleteAncestorsStatement.Reset();
            deleteAncestorsStatement.TryBind("@ItemId", itemId.ToGuidParamValue());
            deleteAncestorsStatement.MoveNext();

            foreach (var ancestorId in ancestorIds)
            {
                updateAncestorsStatement.Reset();
                updateAncestorsStatement.TryBind("@ItemId", itemId.ToGuidParamValue());
                updateAncestorsStatement.TryBind("@AncestorId", ancestorId.ToGuidParamValue());
                updateAncestorsStatement.TryBind("@AncestorIdText", ancestorId.ToString("N"));
                updateAncestorsStatement.MoveNext();
            }
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetAllArtists(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 0, 1 }, typeof(MusicArtist).FullName);
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetArtists(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 0 }, typeof(MusicArtist).FullName);
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetAlbumArtists(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 1 }, typeof(MusicArtist).FullName);
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetStudios(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 3 }, typeof(Studio).FullName);
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetGenres(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 2 }, typeof(Genre).FullName);
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetGameGenres(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 2 }, typeof(GameGenre).FullName);
        }

        public QueryResult<Tuple<BaseItem, ItemCounts>> GetMusicGenres(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 2 }, typeof(MusicGenre).FullName);
        }

        public List<string> GetStudioNames()
        {
            return GetItemValueNames(new[] { 3 }, new List<string>(), new List<string>());
        }

        public List<string> GetAllArtistNames()
        {
            return GetItemValueNames(new[] { 0, 1 }, new List<string>(), new List<string>());
        }

        public List<string> GetMusicGenreNames()
        {
            return GetItemValueNames(new[] { 2 }, new List<string> { "Audio", "MusicVideo", "MusicAlbum", "MusicArtist" }, new List<string>());
        }

        public List<string> GetGameGenreNames()
        {
            return GetItemValueNames(new[] { 2 }, new List<string> { "Game" }, new List<string>());
        }

        public List<string> GetGenreNames()
        {
            return GetItemValueNames(new[] { 2 }, new List<string>(), new List<string> { "Audio", "MusicVideo", "MusicAlbum", "MusicArtist", "Game", "GameSystem" });
        }

        private List<string> GetItemValueNames(int[] itemValueTypes, List<string> withItemTypes, List<string> excludeItemTypes)
        {
            CheckDisposed();

            withItemTypes = withItemTypes.SelectMany(MapIncludeItemTypes).ToList();
            excludeItemTypes = excludeItemTypes.SelectMany(MapIncludeItemTypes).ToList();

            var now = DateTime.UtcNow;

            var typeClause = itemValueTypes.Length == 1 ?
                ("Type=" + itemValueTypes[0].ToString(CultureInfo.InvariantCulture)) :
                ("Type in (" + string.Join(",", itemValueTypes.Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray()) + ")");

            var commandText = "Select Value From ItemValues where " + typeClause;

            if (withItemTypes.Count > 0)
            {
                var typeString = string.Join(",", withItemTypes.Select(i => "'" + i + "'").ToArray());
                commandText += " AND ItemId In (select guid from typedbaseitems where type in (" + typeString + "))";
            }
            if (excludeItemTypes.Count > 0)
            {
                var typeString = string.Join(",", excludeItemTypes.Select(i => "'" + i + "'").ToArray());
                commandText += " AND ItemId not In (select guid from typedbaseitems where type in (" + typeString + "))";
            }

            commandText += " Group By CleanValue";

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var list = new List<string>();

                    using (var statement = PrepareStatementSafe(connection, commandText))
                    {
                        foreach (var row in statement.ExecuteQuery())
                        {
                            if (!row.IsDBNull(0))
                            {
                                list.Add(row.GetString(0));
                            }
                        }
                    }

                    LogQueryTime("GetItemValueNames", commandText, now);

                    return list;
                }
            }
        }

        private QueryResult<Tuple<BaseItem, ItemCounts>> GetItemValues(InternalItemsQuery query, int[] itemValueTypes, string returnType)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            if (!query.Limit.HasValue)
            {
                query.EnableTotalRecordCount = false;
            }

            CheckDisposed();
            //Logger.Info("GetItemValues: " + _environmentInfo.StackTrace);

            var now = DateTime.UtcNow;

            var typeClause = itemValueTypes.Length == 1 ?
                ("Type=" + itemValueTypes[0].ToString(CultureInfo.InvariantCulture)) :
                ("Type in (" + string.Join(",", itemValueTypes.Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray()) + ")");

            InternalItemsQuery typeSubQuery = null;

            var itemCountColumns = new List<Tuple<string, string>>();

            var typesToCount = query.IncludeItemTypes.ToList();

            if (typesToCount.Count > 0)
            {
                var itemCountColumnQuery = "select group_concat(type, '|')" + GetFromText("B");

                typeSubQuery = new InternalItemsQuery(query.User)
                {
                    ExcludeItemTypes = query.ExcludeItemTypes,
                    IncludeItemTypes = query.IncludeItemTypes,
                    MediaTypes = query.MediaTypes,
                    AncestorIds = query.AncestorIds,
                    ExcludeItemIds = query.ExcludeItemIds,
                    ItemIds = query.ItemIds,
                    TopParentIds = query.TopParentIds,
                    ParentId = query.ParentId,
                    IsPlayed = query.IsPlayed
                };
                var whereClauses = GetWhereClauses(typeSubQuery, null, "itemTypes");

                whereClauses.Add("guid in (select ItemId from ItemValues where ItemValues.CleanValue=A.CleanName AND " + typeClause + ")");

                var typeWhereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                itemCountColumnQuery += typeWhereText;

                //itemCountColumnQuery += ")";

                itemCountColumns.Add(new Tuple<string, string>("itemTypes", "(" + itemCountColumnQuery + ") as itemTypes"));
            }

            var columns = _retriveItemColumns.ToList();
            columns.AddRange(itemCountColumns.Select(i => i.Item2).ToArray());

            var commandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, columns.ToArray())) + GetFromText();
            commandText += GetJoinUserDataText(query);

            var innerQuery = new InternalItemsQuery(query.User)
            {
                ExcludeItemTypes = query.ExcludeItemTypes,
                IncludeItemTypes = query.IncludeItemTypes,
                MediaTypes = query.MediaTypes,
                AncestorIds = query.AncestorIds,
                ExcludeItemIds = query.ExcludeItemIds,
                ItemIds = query.ItemIds,
                TopParentIds = query.TopParentIds,
                ParentId = query.ParentId,
                IsPlayed = query.IsPlayed
            };

            var innerWhereClauses = GetWhereClauses(innerQuery, null);

            var innerWhereText = innerWhereClauses.Count == 0 ?
                string.Empty :
                " where " + string.Join(" AND ", innerWhereClauses.ToArray());

            var whereText = " where Type=@SelectType";

            if (typesToCount.Count == 0)
            {
                whereText += " And CleanName In (Select CleanValue from ItemValues where " + typeClause + " AND ItemId in (select guid from TypedBaseItems" + innerWhereText + "))";
            }
            else
            {
                //whereText += " And itemTypes not null";
                whereText += " And CleanName In (Select CleanValue from ItemValues where " + typeClause + " AND ItemId in (select guid from TypedBaseItems" + innerWhereText + "))";
            }

            var outerQuery = new InternalItemsQuery(query.User)
            {
                IsFavorite = query.IsFavorite,
                IsFavoriteOrLiked = query.IsFavoriteOrLiked,
                IsLiked = query.IsLiked,
                IsLocked = query.IsLocked,
                NameLessThan = query.NameLessThan,
                NameStartsWith = query.NameStartsWith,
                NameStartsWithOrGreater = query.NameStartsWithOrGreater,
                AlbumArtistStartsWithOrGreater = query.AlbumArtistStartsWithOrGreater,
                Tags = query.Tags,
                OfficialRatings = query.OfficialRatings,
                GenreIds = query.GenreIds,
                Genres = query.Genres,
                Years = query.Years
            };

            var outerWhereClauses = GetWhereClauses(outerQuery, null);

            whereText += outerWhereClauses.Count == 0 ?
                string.Empty :
                " AND " + string.Join(" AND ", outerWhereClauses.ToArray());
            //cmd.CommandText += GetGroupBy(query);

            commandText += whereText;
            commandText += " group by PresentationUniqueKey";

            commandText += " order by SortName";

            if (query.Limit.HasValue || query.StartIndex.HasValue)
            {
                var offset = query.StartIndex ?? 0;

                if (query.Limit.HasValue || offset > 0)
                {
                    commandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                }

                if (offset > 0)
                {
                    commandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                }
            }

            var isReturningZeroItems = query.Limit.HasValue && query.Limit <= 0;

            var statementTexts = new List<string>();
            if (!isReturningZeroItems)
            {
                statementTexts.Add(commandText);
            }
            if (query.EnableTotalRecordCount)
            {
                var countText = "select count (distinct PresentationUniqueKey)" + GetFromText();

                countText += GetJoinUserDataText(query);
                countText += whereText;
                statementTexts.Add(countText);
            }

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    return connection.RunInTransaction(db =>
                    {
                        var list = new List<Tuple<BaseItem, ItemCounts>>();
                        var result = new QueryResult<Tuple<BaseItem, ItemCounts>>();

                        var statements = PrepareAllSafe(db, statementTexts)
                            .ToList();

                        if (!isReturningZeroItems)
                        {
                            using (var statement = statements[0])
                            {
                                statement.TryBind("@SelectType", returnType);
                                if (EnableJoinUserData(query))
                                {
                                    statement.TryBind("@UserId", query.User.Id);
                                }

                                if (typeSubQuery != null)
                                {
                                    GetWhereClauses(typeSubQuery, null, "itemTypes");
                                }
                                BindSimilarParams(query, statement);
                                GetWhereClauses(innerQuery, statement);
                                GetWhereClauses(outerQuery, statement);

                                foreach (var row in statement.ExecuteQuery())
                                {
                                    var item = GetItem(row);
                                    if (item != null)
                                    {
                                        var countStartColumn = columns.Count - 1;

                                        list.Add(new Tuple<BaseItem, ItemCounts>(item, GetItemCounts(row, countStartColumn, typesToCount)));
                                    }
                                }

                                LogQueryTime("GetItemValues", commandText, now);
                            }
                        }

                        if (query.EnableTotalRecordCount)
                        {
                            commandText = "select count (distinct PresentationUniqueKey)" + GetFromText();

                            commandText += GetJoinUserDataText(query);
                            commandText += whereText;

                            using (var statement = statements[statements.Count - 1])
                            {
                                statement.TryBind("@SelectType", returnType);
                                if (EnableJoinUserData(query))
                                {
                                    statement.TryBind("@UserId", query.User.Id);
                                }

                                if (typeSubQuery != null)
                                {
                                    GetWhereClauses(typeSubQuery, null, "itemTypes");
                                }
                                BindSimilarParams(query, statement);
                                GetWhereClauses(innerQuery, statement);
                                GetWhereClauses(outerQuery, statement);

                                result.TotalRecordCount = statement.ExecuteQuery().SelectScalarInt().First();

                                LogQueryTime("GetItemValues", commandText, now);
                            }
                        }

                        if (result.TotalRecordCount == 0)
                        {
                            result.TotalRecordCount = list.Count;
                        }
                        result.Items = list.ToArray();

                        return result;

                    }, ReadTransactionMode);
                }
            }
        }

        private ItemCounts GetItemCounts(IReadOnlyList<IResultSetValue> reader, int countStartColumn, List<string> typesToCount)
        {
            var counts = new ItemCounts();

            if (typesToCount.Count == 0)
            {
                return counts;
            }

            var typeString = reader.IsDBNull(countStartColumn) ? null : reader.GetString(countStartColumn);

            if (string.IsNullOrWhiteSpace(typeString))
            {
                return counts;
            }

            var allTypes = typeString.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .ToLookup(i => i).ToList();

            foreach (var type in allTypes)
            {
                var value = type.ToList().Count;
                var typeName = type.Key;

                if (string.Equals(typeName, typeof(Series).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.SeriesCount = value;
                }
                else if (string.Equals(typeName, typeof(Episode).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.EpisodeCount = value;
                }
                else if (string.Equals(typeName, typeof(Movie).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.MovieCount = value;
                }
                else if (string.Equals(typeName, typeof(MusicAlbum).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.AlbumCount = value;
                }
                else if (string.Equals(typeName, typeof(MusicArtist).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.ArtistCount = value;
                }
                else if (string.Equals(typeName, typeof(Audio).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.SongCount = value;
                }
                else if (string.Equals(typeName, typeof(Game).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.GameCount = value;
                }
                else if (string.Equals(typeName, typeof(Trailer).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.TrailerCount = value;
                }
                counts.ItemCount += value;
            }

            return counts;
        }

        private List<Tuple<int, string>> GetItemValuesToSave(BaseItem item)
        {
            var list = new List<Tuple<int, string>>();

            var hasArtist = item as IHasArtist;
            if (hasArtist != null)
            {
                list.AddRange(hasArtist.Artists.Select(i => new Tuple<int, string>(0, i)));
            }

            var hasAlbumArtist = item as IHasAlbumArtist;
            if (hasAlbumArtist != null)
            {
                list.AddRange(hasAlbumArtist.AlbumArtists.Select(i => new Tuple<int, string>(1, i)));
            }

            list.AddRange(item.Genres.Select(i => new Tuple<int, string>(2, i)));
            list.AddRange(item.Studios.Select(i => new Tuple<int, string>(3, i)));
            list.AddRange(item.Tags.Select(i => new Tuple<int, string>(4, i)));
            list.AddRange(item.Keywords.Select(i => new Tuple<int, string>(5, i)));

            return list;
        }

        private void UpdateItemValues(Guid itemId, List<Tuple<int, string>> values, IDatabaseConnection db)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentNullException("itemId");
            }

            if (values == null)
            {
                throw new ArgumentNullException("keys");
            }

            CheckDisposed();

            // First delete 
            db.Execute("delete from ItemValues where ItemId=@Id", itemId.ToGuidParamValue());

            using (var statement = PrepareStatement(db, "insert into ItemValues (ItemId, Type, Value, CleanValue) values (@ItemId, @Type, @Value, @CleanValue)"))
            {
                foreach (var pair in values)
                {
                    var itemValue = pair.Item2;

                    // Don't save if invalid
                    if (string.IsNullOrWhiteSpace(itemValue))
                    {
                        continue;
                    }

                    statement.Reset();

                    statement.TryBind("@ItemId", itemId.ToGuidParamValue());
                    statement.TryBind("@Type", pair.Item1);
                    statement.TryBind("@Value", itemValue);

                    if (pair.Item2 == null)
                    {
                        statement.TryBindNull("@CleanValue");
                    }
                    else
                    {
                        statement.TryBind("@CleanValue", GetCleanValue(pair.Item2));
                    }

                    statement.MoveNext();
                }
            }
        }

        public async Task UpdatePeople(Guid itemId, List<PersonInfo> people)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentNullException("itemId");
            }

            if (people == null)
            {
                throw new ArgumentNullException("people");
            }

            CheckDisposed();

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    // First delete 
                    // "delete from People where ItemId=?"
                    connection.Execute("delete from People where ItemId=?", itemId.ToGuidParamValue());

                    var listIndex = 0;

                    using (var statement = PrepareStatement(connection,
                                "insert into People (ItemId, Name, Role, PersonType, SortOrder, ListOrder) values (@ItemId, @Name, @Role, @PersonType, @SortOrder, @ListOrder)"))
                    {
                        foreach (var person in people)
                        {
                            if (listIndex > 0)
                            {
                                statement.Reset();
                            }

                            statement.TryBind("@ItemId", itemId.ToGuidParamValue());
                            statement.TryBind("@Name", person.Name);
                            statement.TryBind("@Role", person.Role);
                            statement.TryBind("@PersonType", person.Type);
                            statement.TryBind("@SortOrder", person.SortOrder);
                            statement.TryBind("@ListOrder", listIndex);

                            statement.MoveNext();
                            listIndex++;
                        }
                    }
                }
            }
        }

        private PersonInfo GetPerson(IReadOnlyList<IResultSetValue> reader)
        {
            var item = new PersonInfo();

            item.ItemId = reader.GetGuid(0);
            item.Name = reader.GetString(1);

            if (!reader.IsDBNull(2))
            {
                item.Role = reader.GetString(2);
            }

            if (!reader.IsDBNull(3))
            {
                item.Type = reader.GetString(3);
            }

            if (!reader.IsDBNull(4))
            {
                item.SortOrder = reader.GetInt32(4);
            }

            return item;
        }

        public IEnumerable<MediaStream> GetMediaStreams(MediaStreamQuery query)
        {
            CheckDisposed();

            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            var cmdText = "select " + string.Join(",", _mediaStreamSaveColumns) + " from mediastreams where";

            cmdText += " ItemId=@ItemId";

            if (query.Type.HasValue)
            {
                cmdText += " AND StreamType=@StreamType";
            }

            if (query.Index.HasValue)
            {
                cmdText += " AND StreamIndex=@StreamIndex";
            }

            cmdText += " order by StreamIndex ASC";

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var list = new List<MediaStream>();

                    using (var statement = PrepareStatementSafe(connection, cmdText))
                    {
                        statement.TryBind("@ItemId", query.ItemId.ToGuidParamValue());

                        if (query.Type.HasValue)
                        {
                            statement.TryBind("@StreamType", query.Type.Value.ToString());
                        }

                        if (query.Index.HasValue)
                        {
                            statement.TryBind("@StreamIndex", query.Index.Value);
                        }

                        foreach (var row in statement.ExecuteQuery())
                        {
                            list.Add(GetMediaStream(row));
                        }
                    }

                    return list;
                }
            }
        }

        public async Task SaveMediaStreams(Guid id, List<MediaStream> streams, CancellationToken cancellationToken)
        {
            CheckDisposed();

            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            if (streams == null)
            {
                throw new ArgumentNullException("streams");
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    // First delete chapters
                    connection.Execute("delete from mediastreams where ItemId=@ItemId", id.ToGuidParamValue());

                    using (var statement = PrepareStatement(connection, string.Format("replace into mediastreams ({0}) values ({1})",
                                string.Join(",", _mediaStreamSaveColumns),
                                string.Join(",", _mediaStreamSaveColumns.Select(i => "@" + i).ToArray()))))
                    {
                        foreach (var stream in streams)
                        {
                            var paramList = new List<object>();

                            paramList.Add(id.ToGuidParamValue());
                            paramList.Add(stream.Index);
                            paramList.Add(stream.Type.ToString());
                            paramList.Add(stream.Codec);
                            paramList.Add(stream.Language);
                            paramList.Add(stream.ChannelLayout);
                            paramList.Add(stream.Profile);
                            paramList.Add(stream.AspectRatio);
                            paramList.Add(stream.Path);

                            paramList.Add(stream.IsInterlaced);
                            paramList.Add(stream.BitRate);
                            paramList.Add(stream.Channels);
                            paramList.Add(stream.SampleRate);

                            paramList.Add(stream.IsDefault);
                            paramList.Add(stream.IsForced);
                            paramList.Add(stream.IsExternal);

                            paramList.Add(stream.Width);
                            paramList.Add(stream.Height);
                            paramList.Add(stream.AverageFrameRate);
                            paramList.Add(stream.RealFrameRate);
                            paramList.Add(stream.Level);
                            paramList.Add(stream.PixelFormat);
                            paramList.Add(stream.BitDepth);
                            paramList.Add(stream.IsExternal);
                            paramList.Add(stream.RefFrames);

                            paramList.Add(stream.CodecTag);
                            paramList.Add(stream.Comment);
                            paramList.Add(stream.NalLengthSize);
                            paramList.Add(stream.IsAVC);
                            paramList.Add(stream.Title);

                            paramList.Add(stream.TimeBase);
                            paramList.Add(stream.CodecTimeBase);

                            statement.Execute(paramList.ToArray());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the chapter.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>ChapterInfo.</returns>
        private MediaStream GetMediaStream(IReadOnlyList<IResultSetValue> reader)
        {
            var item = new MediaStream
            {
                Index = reader[1].ToInt()
            };

            item.Type = (MediaStreamType)Enum.Parse(typeof(MediaStreamType), reader[2].ToString(), true);

            if (reader[3].SQLiteType != SQLiteType.Null)
            {
                item.Codec = reader[3].ToString();
            }

            if (reader[4].SQLiteType != SQLiteType.Null)
            {
                item.Language = reader[4].ToString();
            }

            if (reader[5].SQLiteType != SQLiteType.Null)
            {
                item.ChannelLayout = reader[5].ToString();
            }

            if (reader[6].SQLiteType != SQLiteType.Null)
            {
                item.Profile = reader[6].ToString();
            }

            if (reader[7].SQLiteType != SQLiteType.Null)
            {
                item.AspectRatio = reader[7].ToString();
            }

            if (reader[8].SQLiteType != SQLiteType.Null)
            {
                item.Path = reader[8].ToString();
            }

            item.IsInterlaced = reader.GetBoolean(9);

            if (reader[10].SQLiteType != SQLiteType.Null)
            {
                item.BitRate = reader.GetInt32(10);
            }

            if (reader[11].SQLiteType != SQLiteType.Null)
            {
                item.Channels = reader.GetInt32(11);
            }

            if (reader[12].SQLiteType != SQLiteType.Null)
            {
                item.SampleRate = reader.GetInt32(12);
            }

            item.IsDefault = reader.GetBoolean(13);
            item.IsForced = reader.GetBoolean(14);
            item.IsExternal = reader.GetBoolean(15);

            if (reader[16].SQLiteType != SQLiteType.Null)
            {
                item.Width = reader.GetInt32(16);
            }

            if (reader[17].SQLiteType != SQLiteType.Null)
            {
                item.Height = reader.GetInt32(17);
            }

            if (reader[18].SQLiteType != SQLiteType.Null)
            {
                item.AverageFrameRate = reader.GetFloat(18);
            }

            if (reader[19].SQLiteType != SQLiteType.Null)
            {
                item.RealFrameRate = reader.GetFloat(19);
            }

            if (reader[20].SQLiteType != SQLiteType.Null)
            {
                item.Level = reader.GetFloat(20);
            }

            if (reader[21].SQLiteType != SQLiteType.Null)
            {
                item.PixelFormat = reader[21].ToString();
            }

            if (reader[22].SQLiteType != SQLiteType.Null)
            {
                item.BitDepth = reader.GetInt32(22);
            }

            if (reader[23].SQLiteType != SQLiteType.Null)
            {
                item.IsAnamorphic = reader.GetBoolean(23);
            }

            if (reader[24].SQLiteType != SQLiteType.Null)
            {
                item.RefFrames = reader.GetInt32(24);
            }

            if (reader[25].SQLiteType != SQLiteType.Null)
            {
                item.CodecTag = reader.GetString(25);
            }

            if (reader[26].SQLiteType != SQLiteType.Null)
            {
                item.Comment = reader.GetString(26);
            }

            if (reader[27].SQLiteType != SQLiteType.Null)
            {
                item.NalLengthSize = reader.GetString(27);
            }

            if (reader[28].SQLiteType != SQLiteType.Null)
            {
                item.IsAVC = reader[28].ToBool();
            }

            if (reader[29].SQLiteType != SQLiteType.Null)
            {
                item.Title = reader[29].ToString();
            }

            if (reader[30].SQLiteType != SQLiteType.Null)
            {
                item.TimeBase = reader[30].ToString();
            }

            if (reader[31].SQLiteType != SQLiteType.Null)
            {
                item.CodecTimeBase = reader[31].ToString();
            }

            return item;
        }

    }
}