using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Devices;
using Emby.Server.Implementations.Playlists;
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
using MediaBrowser.Model.Reflection;
using SQLitePCL.pretty;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Threading;
using MediaBrowser.Model.Extensions;

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
            if (_shrinkMemoryTimer != null)
            {
                _shrinkMemoryTimer.Dispose();
                _shrinkMemoryTimer = null;
            }

            base.CloseConnection();
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        public void Initialize(SqliteUserDataRepository userDataRepo)
        {
            using (var connection = CreateConnection())
            {
                RunDefaultInitialization(connection);

                var createMediaStreamsTableCommand
                    = "create table if not exists mediastreams (ItemId GUID, StreamIndex INT, StreamType TEXT, Codec TEXT, Language TEXT, ChannelLayout TEXT, Profile TEXT, AspectRatio TEXT, Path TEXT, IsInterlaced BIT, BitRate INT NULL, Channels INT NULL, SampleRate INT NULL, IsDefault BIT, IsForced BIT, IsExternal BIT, Height INT NULL, Width INT NULL, AverageFrameRate FLOAT NULL, RealFrameRate FLOAT NULL, Level FLOAT NULL, PixelFormat TEXT, BitDepth INT NULL, IsAnamorphic BIT NULL, RefFrames INT NULL, CodecTag TEXT NULL, Comment TEXT NULL, NalLengthSize TEXT NULL, IsAvc BIT NULL, Title TEXT NULL, TimeBase TEXT NULL, CodecTimeBase TEXT NULL, PRIMARY KEY (ItemId, StreamIndex))";

                string[] queries = {
                    "PRAGMA locking_mode=EXCLUSIVE",

                    "create table if not exists TypedBaseItems (guid GUID primary key NOT NULL, type TEXT NOT NULL, data BLOB NULL, ParentId GUID NULL, Path TEXT NULL)",

                    "create table if not exists AncestorIds (ItemId GUID, AncestorId GUID, AncestorIdText TEXT, PRIMARY KEY (ItemId, AncestorId))",
                    "create index if not exists idx_AncestorIds1 on AncestorIds(AncestorId)",
                    "create index if not exists idx_AncestorIds5 on AncestorIds(AncestorIdText,ItemId)",

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
                    AddColumn(db, "TypedBaseItems", "ForcedSortName", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "RunTimeTicks", "BIGINT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "HomePageUrl", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "DateCreated", "DATETIME", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "DateModified", "DATETIME", existingColumnNames);
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
                    AddColumn(db, "TypedBaseItems", "TrailerTypes", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "CriticRating", "Float", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "CleanName", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "PresentationUniqueKey", "Text", existingColumnNames);
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
                    AddColumn(db, "TypedBaseItems", "ExternalSeriesId", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Tagline", "Text", existingColumnNames);
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
                    AddColumn(db, "TypedBaseItems", "ShowId", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "OwnerId", "Text", existingColumnNames);

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
                    "drop index if exists idx_AncestorIds3",
                    "drop index if exists idx_AncestorIds4",
                    "drop index if exists idx_AncestorIds2",

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
                    "create index if not exists idx_ItemValues7 on ItemValues(Type,CleanValue,ItemId)",

                    // Used to update inherited tags
                    "create index if not exists idx_ItemValues8 on ItemValues(Type, ItemId, Value)",
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
            "HomePageUrl",
            "ForcedSortName",
            "RunTimeTicks",
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
            "TrailerTypes",
            "OriginalTitle",
            "PrimaryVersionId",
            "DateLastMediaAdded",
            "Album",
            "CriticRating",
            "IsVirtualItem",
            "SeriesName",
            "SeasonName",
            "SeasonId",
            "SeriesId",
            "PresentationUniqueKey",
            "InheritedParentalRatingValue",
            "ExternalSeriesId",
            "Tagline",
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
            "SeriesPresentationUniqueKey",
            "ShowId",
            "OwnerId"
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
                "ForcedSortName",
                "RunTimeTicks",
                "HomePageUrl",
                "DateCreated",
                "DateModified",
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
                "TrailerTypes",
                "CriticRating",
                "CleanName",
                "PresentationUniqueKey",
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
                "ExternalSeriesId",
                "Tagline",
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
                "SeriesPresentationUniqueKey",
                "ShowId",
                "OwnerId"
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
        /// <exception cref="System.ArgumentNullException">item</exception>
        public void SaveItem(BaseItem item, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            SaveItems(new List<BaseItem> { item }, cancellationToken);
        }

        /// <summary>
        /// Saves the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="System.ArgumentNullException">
        /// items
        /// or
        /// cancellationToken
        /// </exception>
        public void SaveItems(List<BaseItem> items, CancellationToken cancellationToken)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            cancellationToken.ThrowIfCancellationRequested();

            CheckDisposed();

            var tuples = new List<Tuple<BaseItem, List<Guid>, BaseItem, string, List<string>>>();
            foreach (var item in items)
            {
                var ancestorIds = item.SupportsAncestors ?
                    item.GetAncestorIds().Distinct().ToList() :
                    null;

                var topParent = item.GetTopParent();

                var userdataKey = item.GetUserDataKeys().FirstOrDefault();
                var inheritedTags = item.GetInheritedTags();

                tuples.Add(new Tuple<BaseItem, List<Guid>, BaseItem, string, List<string>>(item, ancestorIds, topParent, userdataKey, inheritedTags));
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

        private void SaveItemsInTranscation(IDatabaseConnection db, List<Tuple<BaseItem, List<Guid>, BaseItem, string, List<string>>> tuples)
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

                            var inheritedTags = tuple.Item5;

                            if (item.SupportsAncestors)
                            {
                                UpdateAncestors(item.Id, tuple.Item2, db, deleteAncestorsStatement, updateAncestorsStatement);
                            }

                            UpdateItemValues(item.Id, GetItemValuesToSave(item, inheritedTags), db);

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

            var parentId = item.ParentId;
            if (parentId == Guid.Empty)
            {
                saveItemStatement.TryBindNull("@ParentId");
            }
            else
            {
                saveItemStatement.TryBind("@ParentId", parentId);
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

            saveItemStatement.TryBind("@ForcedSortName", item.ForcedSortName);

            saveItemStatement.TryBind("@RunTimeTicks", item.RunTimeTicks);

            saveItemStatement.TryBind("@HomePageUrl", item.HomePageUrl);
            saveItemStatement.TryBind("@DateCreated", item.DateCreated);
            saveItemStatement.TryBind("@DateModified", item.DateModified);

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

            if (item.LockedFields.Length > 0)
            {
                saveItemStatement.TryBind("@LockedFields", string.Join("|", item.LockedFields.Select(i => i.ToString()).ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@LockedFields");
            }

            if (item.Studios.Length > 0)
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

            if (item.Tags.Length > 0)
            {
                saveItemStatement.TryBind("@Tags", string.Join("|", item.Tags));
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

            if (string.IsNullOrWhiteSpace(item.Name))
            {
                saveItemStatement.TryBindNull("@CleanName");
            }
            else
            {
                saveItemStatement.TryBind("@CleanName", GetCleanValue(item.Name));
            }

            saveItemStatement.TryBind("@PresentationUniqueKey", item.PresentationUniqueKey);
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
                saveItemStatement.TryBind("@SeriesPresentationUniqueKey", hasSeries.SeriesPresentationUniqueKey);
            }
            else
            {
                saveItemStatement.TryBindNull("@SeriesId");
                saveItemStatement.TryBindNull("@SeriesPresentationUniqueKey");
            }

            saveItemStatement.TryBind("@ExternalSeriesId", item.ExternalSeriesId);
            saveItemStatement.TryBind("@Tagline", item.Tagline);

            saveItemStatement.TryBind("@ProviderIds", SerializeProviderIds(item));
            saveItemStatement.TryBind("@Images", SerializeImages(item));

            if (item.ProductionLocations.Length > 0)
            {
                saveItemStatement.TryBind("@ProductionLocations", string.Join("|", item.ProductionLocations));
            }
            else
            {
                saveItemStatement.TryBindNull("@ProductionLocations");
            }

            if (item.ThemeSongIds.Length > 0)
            {
                saveItemStatement.TryBind("@ThemeSongIds", string.Join("|", item.ThemeSongIds.ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@ThemeSongIds");
            }

            if (item.ThemeVideoIds.Length > 0)
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
                if (hasArtists.Artists.Length > 0)
                {
                    artists = string.Join("|", hasArtists.Artists);
                }
            }
            saveItemStatement.TryBind("@Artists", artists);

            string albumArtists = null;
            var hasAlbumArtists = item as IHasAlbumArtist;
            if (hasAlbumArtists != null)
            {
                if (hasAlbumArtists.AlbumArtists.Length > 0)
                {
                    albumArtists = string.Join("|", hasAlbumArtists.AlbumArtists);
                }
            }
            saveItemStatement.TryBind("@AlbumArtists", albumArtists);
            saveItemStatement.TryBind("@ExternalId", item.ExternalId);

            var program = item as LiveTvProgram;
            if (program != null)
            {
                saveItemStatement.TryBind("@ShowId", program.ShowId);
            }
            else
            {
                saveItemStatement.TryBindNull("@ShowId");
            }

            var ownerId = item.OwnerId;
            if (ownerId != Guid.Empty)
            {
                saveItemStatement.TryBind("@OwnerId", ownerId);
            }
            else
            {
                saveItemStatement.TryBindNull("@OwnerId");
            }

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
            var images = item.ImageInfos;

            if (images.Length == 0)
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

            if (item.ImageInfos.Length > 0)
            {
                return;
            }

            var parts = value.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<ItemImageInfo>();
            foreach (var part in parts)
            {
                var image = ItemImageInfoFromValueString(part);

                if (image != null)
                {
                    list.Add(image);
                }
            }

            item.ImageInfos = list.ToArray(list.Count);
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
                   image.Type;
        }

        public ItemImageInfo ItemImageInfoFromValueString(string value)
        {
            var parts = value.Split(new[] { '*' }, StringSplitOptions.None);

            if (parts.Length < 3)
            {
                return null;
            }

            var image = new ItemImageInfo();

            image.Path = parts[0];

            long ticks;
            if (long.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out ticks))
            {
                image.DateModified = new DateTime(ticks, DateTimeKind.Utc);
            }

            ImageType type;
            if (Enum.TryParse(parts[2], true, out type))
            {
                image.Type = type;
            }

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
                            return GetItem(row, new InternalItemsQuery());
                        }
                    }

                    return null;
                }
            }
        }

        private bool TypeRequiresDeserialization(Type type)
        {
            if (_config.Configuration.SkipDeserializationForBasicTypes)
            {
                if (type == typeof(Channel))
                {
                    return false;
                }
                if (type == typeof(UserRootFolder))
                {
                    return false;
                }
            }

            if (type == typeof(Season))
            {
                return false;
            }
            if (type == typeof(MusicArtist))
            {
                return false;
            }

            if (type == typeof(Person))
            {
                return false;
            }
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

            if (type == typeof(PhotoAlbum))
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
            if (type == typeof(RecordingGroup))
            {
                return false;
            }
            if (type == typeof(LiveTvProgram))
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

            if (type == typeof(Audio))
            {
                return false;
            }
            if (type == typeof(MusicAlbum))
            {
                return false;
            }

            return true;
        }

        private BaseItem GetItem(IReadOnlyList<IResultSetValue> reader, InternalItemsQuery query)
        {
            return GetItem(reader, query, HasProgramAttributes(query), HasEpisodeAttributes(query), HasStartDate(query), HasTrailerTypes(query), HasArtistFields(query), HasSeriesFields(query));
        }

        private BaseItem GetItem(IReadOnlyList<IResultSetValue> reader, InternalItemsQuery query, bool enableProgramAttributes, bool hasEpisodeAttributes, bool queryHasStartDate, bool hasTrailerTypes, bool hasArtistFields, bool hasSeriesFields)
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

            var index = 2;

            if (queryHasStartDate)
            {
                if (!reader.IsDBNull(index))
                {
                    var hasStartDate = item as IHasStartDate;
                    if (hasStartDate != null)
                    {
                        hasStartDate.StartDate = reader[index].ReadDateTime();
                    }
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.EndDate = reader[index].ReadDateTime();
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.ChannelId = reader.GetString(index);
            }
            index++;

            if (enableProgramAttributes)
            {
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
            }

            if (!reader.IsDBNull(index))
            {
                item.CommunityRating = reader.GetFloat(index);
            }
            index++;

            if (HasField(query, ItemFields.CustomRating))
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

            if (HasField(query, ItemFields.Settings))
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

            if (HasField(query, ItemFields.ExternalEtag))
            {
                if (!reader.IsDBNull(index))
                {
                    item.ExternalEtag = reader.GetString(index);
                }
                index++;
            }

            if (HasField(query, ItemFields.DateLastRefreshed))
            {
                if (!reader.IsDBNull(index))
                {
                    item.DateLastRefreshed = reader[index].ReadDateTime();
                }
                index++;
            }

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

            if (HasField(query, ItemFields.Overview))
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

            if (HasField(query, ItemFields.HomePageUrl))
            {
                if (!reader.IsDBNull(index))
                {
                    item.HomePageUrl = reader.GetString(index);
                }
                index++;
            }

            if (HasField(query, ItemFields.SortName))
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

            if (HasField(query, ItemFields.DateCreated))
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

            if (HasField(query, ItemFields.Genres))
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
                ProgramAudio audio;
                if (Enum.TryParse(reader.GetString(index), true, out audio))
                {
                    item.Audio = audio;
                }
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

            if (HasField(query, ItemFields.DateLastSaved))
            {
                if (!reader.IsDBNull(index))
                {
                    item.DateLastSaved = reader[index].ReadDateTime();
                }
                index++;
            }

            if (HasField(query, ItemFields.Settings))
            {
                if (!reader.IsDBNull(index))
                {
                    item.LockedFields = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).Select(
                        i =>
                        {
                            MetadataFields parsedValue;

                            if (Enum.TryParse(i, true, out parsedValue))
                            {
                                return parsedValue;
                            }
                            return (MetadataFields?)null;
                        }).Where(i => i.HasValue).Select(i => i.Value).ToArray();
                }
                index++;
            }

            if (HasField(query, ItemFields.Studios))
            {
                if (!reader.IsDBNull(index))
                {
                    item.Studios = reader.GetString(index).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                }
                index++;
            }

            if (HasField(query, ItemFields.Tags))
            {
                if (!reader.IsDBNull(index))
                {
                    item.Tags = reader.GetString(index).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                }
                index++;
            }

            if (hasTrailerTypes)
            {
                var trailer = item as Trailer;
                if (trailer != null)
                {
                    if (!reader.IsDBNull(index))
                    {
                        trailer.TrailerTypes = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).Select(
                            i =>
                            {
                                TrailerType parsedValue;

                                if (Enum.TryParse(i, true, out parsedValue))
                                {
                                    return parsedValue;
                                }
                                return (TrailerType?)null;

                            }).Where(i => i.HasValue).Select(i => i.Value).ToList();
                    }
                }
                index++;
            }

            if (HasField(query, ItemFields.OriginalTitle))
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

            if (HasField(query, ItemFields.DateLastMediaAdded))
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

            if (!reader.IsDBNull(index))
            {
                item.IsVirtualItem = reader.GetBoolean(index);
            }
            index++;

            var hasSeries = item as IHasSeries;
            if (hasSeriesFields)
            {
                if (hasSeries != null)
                {
                    if (!reader.IsDBNull(index))
                    {
                        hasSeries.SeriesName = reader.GetString(index);
                    }
                }
                index++;
            }

            if (hasEpisodeAttributes)
            {
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
            }

            if (hasSeriesFields)
            {
                if (hasSeries != null)
                {
                    if (!reader.IsDBNull(index))
                    {
                        hasSeries.SeriesId = reader.GetGuid(index);
                    }
                }
                index++;
            }

            if (HasField(query, ItemFields.PresentationUniqueKey))
            {
                if (!reader.IsDBNull(index))
                {
                    item.PresentationUniqueKey = reader.GetString(index);
                }
                index++;
            }

            if (HasField(query, ItemFields.InheritedParentalRatingValue))
            {
                if (!reader.IsDBNull(index))
                {
                    item.InheritedParentalRatingValue = reader.GetInt32(index);
                }
                index++;
            }

            if (HasField(query, ItemFields.ExternalSeriesId))
            {
                if (!reader.IsDBNull(index))
                {
                    item.ExternalSeriesId = reader.GetString(index);
                }
                index++;
            }

            if (HasField(query, ItemFields.Taglines))
            {
                if (!reader.IsDBNull(index))
                {
                    item.Tagline = reader.GetString(index);
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

            if (HasField(query, ItemFields.ProductionLocations))
            {
                if (!reader.IsDBNull(index))
                {
                    item.ProductionLocations = reader.GetString(index).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).ToArray();
                }
                index++;
            }

            if (HasField(query, ItemFields.ThemeSongIds))
            {
                if (!reader.IsDBNull(index))
                {
                    item.ThemeSongIds = SplitToGuids(reader.GetString(index));
                }
                index++;
            }

            if (HasField(query, ItemFields.ThemeVideoIds))
            {
                if (!reader.IsDBNull(index))
                {
                    item.ThemeVideoIds = SplitToGuids(reader.GetString(index));
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
                ExtraType extraType;
                if (Enum.TryParse(reader.GetString(index), true, out extraType))
                {
                    item.ExtraType = extraType;
                }
            }
            index++;

            if (hasArtistFields)
            {
                var hasArtists = item as IHasArtist;
                if (hasArtists != null && !reader.IsDBNull(index))
                {
                    hasArtists.Artists = reader.GetString(index).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                }
                index++;

                var hasAlbumArtists = item as IHasAlbumArtist;
                if (hasAlbumArtists != null && !reader.IsDBNull(index))
                {
                    hasAlbumArtists.AlbumArtists = reader.GetString(index).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.ExternalId = reader.GetString(index);
            }
            index++;

            if (HasField(query, ItemFields.SeriesPresentationUniqueKey))
            {
                if (hasSeries != null)
                {
                    if (!reader.IsDBNull(index))
                    {
                        hasSeries.SeriesPresentationUniqueKey = reader.GetString(index);
                    }
                }
                index++;
            }

            if (enableProgramAttributes)
            {
                var program = item as LiveTvProgram;
                if (program != null)
                {
                    if (!reader.IsDBNull(index))
                    {
                        program.ShowId = reader.GetString(index);
                    }
                    index++;
                }
                else
                {
                    index++;
                }
            }

            if (!reader.IsDBNull(index))
            {
                item.OwnerId = reader.GetGuid(index);
            }
            index++;

            return item;
        }

        private Guid[] SplitToGuids(string value)
        {
            var ids = value.Split('|');

            var result = new Guid[ids.Length];

            for (var i = 0; i < result.Length; i++)
            {
                result[i] = new Guid(ids[i]);
            }

            return result;
        }

        /// <summary>
        /// Gets the critic reviews.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        public List<ItemReview> GetCriticReviews(Guid itemId)
        {
            return new List<ItemReview>();
        }

        /// <summary>
        /// Saves the critic reviews.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="criticReviews">The critic reviews.</param>
        public void SaveCriticReviews(Guid itemId, IEnumerable<ItemReview> criticReviews)
        {
        }

        /// <summary>
        /// Gets chapters for an item
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>IEnumerable{ChapterInfo}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public List<ChapterInfo> GetChapters(Guid id)
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
        public void SaveChapters(Guid id, List<ChapterInfo> chapters)
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

            var index = 0;

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        // First delete chapters
                        db.Execute("delete from " + ChaptersTableName + " where ItemId=@ItemId", id.ToGuidBlob());

                        using (var saveChapterStatement = PrepareStatement(db, "replace into " + ChaptersTableName + " (ItemId, ChapterIndex, StartPositionTicks, Name, ImagePath, ImageDateModified) values (@ItemId, @ChapterIndex, @StartPositionTicks, @Name, @ImagePath, @ImageDateModified)"))
                        {
                            foreach (var chapter in chapters)
                            {
                                if (index > 0)
                                {
                                    saveChapterStatement.Reset();
                                }

                                saveChapterStatement.TryBind("@ItemId", id.ToGuidBlob());
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

            var sortingFields = query.OrderBy.Select(i => i.Item1).ToList();

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

        private readonly List<ItemFields> allFields = Enum.GetNames(typeof(ItemFields))
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
            if (field == ItemFields.Tags)
            {
                return new[] { "Tags" };
            }

            return new[] { field.ToString() };
        }

        private bool HasField(InternalItemsQuery query, ItemFields name)
        {
            var fields = query.DtoOptions.Fields;

            switch (name)
            {
                case ItemFields.HomePageUrl:
                case ItemFields.CustomRating:
                case ItemFields.ProductionLocations:
                case ItemFields.Settings:
                case ItemFields.OriginalTitle:
                case ItemFields.Taglines:
                case ItemFields.SortName:
                case ItemFields.Studios:
                case ItemFields.Tags:
                case ItemFields.ThemeSongIds:
                case ItemFields.ThemeVideoIds:
                case ItemFields.DateCreated:
                case ItemFields.Overview:
                case ItemFields.Genres:
                case ItemFields.DateLastMediaAdded:
                case ItemFields.ExternalEtag:
                case ItemFields.PresentationUniqueKey:
                case ItemFields.InheritedParentalRatingValue:
                case ItemFields.ExternalSeriesId:
                case ItemFields.SeriesPresentationUniqueKey:
                case ItemFields.DateLastRefreshed:
                case ItemFields.DateLastSaved:
                    return fields.Contains(name);
                case ItemFields.ServiceName:
                    return true;
                default:
                    return true;
            }
        }

        private bool HasProgramAttributes(InternalItemsQuery query)
        {
            var excludeParentTypes = new string[]
            {
                "Series",
                "Season",
                "MusicAlbum",
                "MusicArtist",
                "PhotoAlbum"
            };

            if (excludeParentTypes.Contains(query.ParentType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            var types = new string[]
            {
                "Program",
                "Recording",
                "TvChannel",
                "LiveTvAudioRecording",
                "LiveTvVideoRecording",
                "LiveTvProgram",
                "LiveTvTvChannel"
            };

            return types.Any(i => query.IncludeItemTypes.Contains(i, StringComparer.OrdinalIgnoreCase));
        }

        private bool HasStartDate(InternalItemsQuery query)
        {
            var excludeParentTypes = new string[]
            {
                "Series",
                "Season",
                "MusicAlbum",
                "MusicArtist",
                "PhotoAlbum"
            };

            if (excludeParentTypes.Contains(query.ParentType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            var types = new string[]
            {
                "Program",
                "Recording",
                "LiveTvAudioRecording",
                "LiveTvVideoRecording",
                "LiveTvProgram"
            };

            return types.Any(i => query.IncludeItemTypes.Contains(i, StringComparer.OrdinalIgnoreCase));
        }

        private bool HasEpisodeAttributes(InternalItemsQuery query)
        {
            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            var types = new string[]
            {
                "Episode"
            };

            return types.Any(i => query.IncludeItemTypes.Contains(i, StringComparer.OrdinalIgnoreCase));
        }

        private bool HasTrailerTypes(InternalItemsQuery query)
        {
            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            var types = new string[]
            {
                "Trailer"
            };

            return types.Any(i => query.IncludeItemTypes.Contains(i, StringComparer.OrdinalIgnoreCase));
        }

        private bool HasArtistFields(InternalItemsQuery query)
        {
            var excludeParentTypes = new string[]
            {
                "Series",
                "Season",
                "PhotoAlbum"
            };

            if (excludeParentTypes.Contains(query.ParentType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            var types = new string[]
            {
                "Audio",
                "MusicAlbum",
                "MusicVideo",
                "AudioBook",
                "AudioPodcast",
                "LiveTvAudioRecording",
                "Recording"
            };

            return types.Any(i => query.IncludeItemTypes.Contains(i, StringComparer.OrdinalIgnoreCase));
        }

        private bool HasSeriesFields(InternalItemsQuery query)
        {
            var excludeParentTypes = new string[]
            {
                "PhotoAlbum"
            };

            if (excludeParentTypes.Contains(query.ParentType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            var types = new string[]
            {
                "Book",
                "AudioBook",
                "Episode",
                "Season"
            };

            return types.Any(i => query.IncludeItemTypes.Contains(i, StringComparer.OrdinalIgnoreCase));
        }

        private string[] GetFinalColumnsToSelect(InternalItemsQuery query, string[] startColumns)
        {
            var list = startColumns.ToList();

            foreach (var field in allFields)
            {
                if (!HasField(query, field))
                {
                    foreach (var fieldToRemove in GetColumnNamesFromField(field).ToList())
                    {
                        list.Remove(fieldToRemove);
                    }
                }
            }

            if (!HasProgramAttributes(query))
            {
                list.Remove("IsKids");
                list.Remove("IsMovie");
                list.Remove("IsSports");
                list.Remove("IsSeries");
                list.Remove("IsLive");
                list.Remove("IsNews");
                list.Remove("IsPremiere");
                list.Remove("EpisodeTitle");
                list.Remove("IsRepeat");
                list.Remove("ShowId");
            }

            if (!HasEpisodeAttributes(query))
            {
                list.Remove("SeasonName");
                list.Remove("SeasonId");
            }

            if (!HasStartDate(query))
            {
                list.Remove("StartDate");
            }

            if (!HasTrailerTypes(query))
            {
                list.Remove("TrailerTypes");
            }

            if (!HasArtistFields(query))
            {
                list.Remove("AlbumArtists");
                list.Remove("Artists");
            }

            if (!HasSeriesFields(query))
            {
                list.Remove("SeriesId");
                list.Remove("SeriesName");
            }

            if (!HasEpisodeAttributes(query))
            {
                list.Remove("SeasonName");
                list.Remove("SeasonId");
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

                query.ExcludeItemIds = excludeIds.ToArray(excludeIds.Count);
                query.ExcludeProviderIds = item.ProviderIds;
            }

            return list.ToArray(list.Count);
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

            if (query.GroupBySeriesPresentationUniqueKey)
            {
                groups.Add("SeriesPresentationUniqueKey");
            }

            if (groups.Count > 0)
            {
                return " Group by " + string.Join(",", groups.ToArray(groups.Count));
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

            var commandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "count(distinct PresentationUniqueKey)" })) + GetFromText();
            commandText += GetJoinUserDataText(query);

            var whereClauses = GetWhereClauses(query, null);

            var whereText = whereClauses.Count == 0 ?
                string.Empty :
                " where " + string.Join(" AND ", whereClauses.ToArray(whereClauses.Count));

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
                " where " + string.Join(" AND ", whereClauses.ToArray(whereClauses.Count));

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

                        var hasEpisodeAttributes = HasEpisodeAttributes(query);
                        var hasProgramAttributes = HasProgramAttributes(query);
                        var hasStartDate = HasStartDate(query);
                        var hasTrailerTypes = HasTrailerTypes(query);
                        var hasArtistFields = HasArtistFields(query);
                        var hasSeriesFields = HasSeriesFields(query);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            var item = GetItem(row, query, hasProgramAttributes, hasEpisodeAttributes, hasStartDate, hasTrailerTypes, hasArtistFields, hasSeriesFields);
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
            slowThreshold = 10;
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
                    Items = returnList.ToArray(returnList.Count),
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
                " where " + string.Join(" AND ", whereClauses.ToArray(whereClauses.Count));

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
                else if (query.GroupBySeriesPresentationUniqueKey)
                {
                    commandText += " select count (distinct SeriesPresentationUniqueKey)" + GetFromText();
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
                        var statements = PrepareAllSafe(db, statementTexts);

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

                                var hasEpisodeAttributes = HasEpisodeAttributes(query);
                                var hasProgramAttributes = HasProgramAttributes(query);
                                var hasStartDate = HasStartDate(query);
                                var hasTrailerTypes = HasTrailerTypes(query);
                                var hasArtistFields = HasArtistFields(query);
                                var hasSeriesFields = HasSeriesFields(query);

                                foreach (var row in statement.ExecuteQuery())
                                {
                                    var item = GetItem(row, query, hasProgramAttributes, hasEpisodeAttributes, hasStartDate, hasTrailerTypes, hasArtistFields, hasSeriesFields);
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

                        result.Items = list.ToArray(list.Count);
                        return result;

                    }, ReadTransactionMode);
                }
            }
        }

        private string GetOrderByText(InternalItemsQuery query)
        {
            var orderBy = query.OrderBy.ToList();
            var enableOrderInversion = false;

            if (query.SimilarTo != null)
            {
                if (orderBy.Count == 0)
                {
                    orderBy.Add(new Tuple<string, SortOrder>(ItemSortBy.Random, SortOrder.Ascending));
                    orderBy.Add(new Tuple<string, SortOrder>("SimilarityScore", SortOrder.Descending));
                    //orderBy.Add(new Tuple<string, SortOrder>(ItemSortBy.Random, SortOrder.Ascending));
                }
            }

            query.OrderBy = orderBy.ToArray();

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
                if (query.GroupBySeriesPresentationUniqueKey)
                {
                    return new Tuple<string, bool>("MAX(LastPlayedDate)", false);
                }

                return new Tuple<string, bool>("LastPlayedDate", false);
            }
            if (string.Equals(name, ItemSortBy.PlayCount, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("PlayCount", false);
            }
            if (string.Equals(name, ItemSortBy.IsFavoriteOrLiked, StringComparison.OrdinalIgnoreCase))
            {
                // (Select Case When Abs(COALESCE(ProductionYear, 0) - @ItemProductionYear) < 10 Then 2 Else 0 End )
                return new Tuple<string, bool>("(Select Case When IsFavorite is null Then 0 Else IsFavorite End )", true);
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
            if (string.Equals(name, ItemSortBy.SeriesSortName, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("SeriesName", false);
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
                " where " + string.Join(" AND ", whereClauses.ToArray(whereClauses.Count));

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
                            list.Add(row[0].ReadGuidFromBlob());
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
                " where " + string.Join(" AND ", whereClauses.ToArray(whereClauses.Count));

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
                    Items = returnList.ToArray(returnList.Count),
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
                " where " + string.Join(" AND ", whereClauses.ToArray(whereClauses.Count));

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
                else if (query.GroupBySeriesPresentationUniqueKey)
                {
                    commandText += " select count (distinct SeriesPresentationUniqueKey)" + GetFromText();
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

                        var statements = PrepareAllSafe(db, statementTexts);

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
                                    list.Add(row[0].ReadGuidFromBlob());
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

                        result.Items = list.ToArray(list.Count);
                        return result;

                    }, ReadTransactionMode);
                }
            }
        }

        private bool IsAlphaNumeric(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
                return false;

            for (int i = 0; i < str.Length; i++)
            {
                if (!(char.IsLetter(str[i])) && (!(char.IsNumber(str[i]))))
                    return false;
            }

            return true;
        }

        private bool IsValidType(string value)
        {
            return IsAlphaNumeric(value);
        }

        private bool IsValidMediaType(string value)
        {
            return IsAlphaNumeric(value);
        }

        private bool IsValidId(string value)
        {
            return IsAlphaNumeric(value);
        }

        private bool IsValidPersonType(string value)
        {
            return IsAlphaNumeric(value);
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
                    whereClauses.Add("(" + string.Join(" OR ", programAttribtues.ToArray(programAttribtues.Count)) + ")");
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
                whereClauses.Add("type=@type");
                if (statement != null)
                {
                    statement.TryBind("@type", includeTypes[0]);
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
            else if (query.ChannelIds.Length > 1)
            {
                var inClause = string.Join(",", query.ChannelIds.Where(IsValidId).Select(i => "'" + i + "'").ToArray());
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
                whereClauses.Add("(DateLastSaved not null and DateLastSaved>=@MinDateLastSavedForUser)");
                if (statement != null)
                {
                    statement.TryBind("@MinDateLastSaved", query.MinDateLastSaved.Value);
                }
            }

            if (query.MinDateLastSavedForUser.HasValue)
            {
                whereClauses.Add("(DateLastSaved not null and DateLastSaved>=@MinDateLastSavedForUser)");
                if (statement != null)
                {
                    statement.TryBind("@MinDateLastSavedForUser", query.MinDateLastSavedForUser.Value);
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
                        statement.TryBind(paramName, personId.ToGuidBlob());
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
                        statement.TryBind(paramName, artistId.ToGuidBlob());
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
                        statement.TryBind(paramName, albumId.ToGuidBlob());
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
                        statement.TryBind(paramName, artistId.ToGuidBlob());
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
                        statement.TryBind(paramName, genreId.ToGuidBlob());
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
                        statement.TryBind(paramName, studioId.ToGuidBlob());
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

            if (!string.IsNullOrWhiteSpace(query.HasNoAudioTrackWithLanguage))
            {
                whereClauses.Add("((select language from MediaStreams where MediaStreams.ItemId=A.Guid and MediaStreams.StreamType='Audio' and MediaStreams.Language=@HasNoAudioTrackWithLanguage limit 1) is null)");
                if (statement != null)
                {
                    statement.TryBind("@HasNoAudioTrackWithLanguage", query.HasNoAudioTrackWithLanguage);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.HasNoInternalSubtitleTrackWithLanguage))
            {
                whereClauses.Add("((select language from MediaStreams where MediaStreams.ItemId=A.Guid and MediaStreams.StreamType='Subtitle' and MediaStreams.IsExternal=0 and MediaStreams.Language=@HasNoInternalSubtitleTrackWithLanguage limit 1) is null)");
                if (statement != null)
                {
                    statement.TryBind("@HasNoInternalSubtitleTrackWithLanguage", query.HasNoInternalSubtitleTrackWithLanguage);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.HasNoExternalSubtitleTrackWithLanguage))
            {
                whereClauses.Add("((select language from MediaStreams where MediaStreams.ItemId=A.Guid and MediaStreams.StreamType='Subtitle' and MediaStreams.IsExternal=1 and MediaStreams.Language=@HasNoExternalSubtitleTrackWithLanguage limit 1) is null)");
                if (statement != null)
                {
                    statement.TryBind("@HasNoExternalSubtitleTrackWithLanguage", query.HasNoExternalSubtitleTrackWithLanguage);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.HasNoSubtitleTrackWithLanguage))
            {
                whereClauses.Add("((select language from MediaStreams where MediaStreams.ItemId=A.Guid and MediaStreams.StreamType='Subtitle' and MediaStreams.Language=@HasNoSubtitleTrackWithLanguage limit 1) is null)");
                if (statement != null)
                {
                    statement.TryBind("@HasNoSubtitleTrackWithLanguage", query.HasNoSubtitleTrackWithLanguage);
                }
            }

            if (query.HasChapterImages.HasValue)
            {
                if (query.HasChapterImages.Value)
                {
                    whereClauses.Add("((select imagepath from Chapters2 where Chapters2.ItemId=A.Guid and imagepath not null limit 1) not null)");
                }
                else
                {
                    whereClauses.Add("((select imagepath from Chapters2 where Chapters2.ItemId=A.Guid and imagepath not null limit 1) is null)");
                }
            }

            if (query.HasDeadParentId.HasValue && query.HasDeadParentId.Value)
            {
                whereClauses.Add("ParentId NOT NULL AND ParentId NOT IN (select guid from TypedBaseItems)");
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

            var isVirtualItem = query.IsVirtualItem ?? query.IsMissing;
            if (isVirtualItem.HasValue)
            {
                whereClauses.Add("IsVirtualItem=@IsVirtualItem");
                if (statement != null)
                {
                    statement.TryBind("@IsVirtualItem", isVirtualItem.Value);
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
            var queryMediaTypes = query.MediaTypes.Where(IsValidMediaType).ToArray();
            if (queryMediaTypes.Length == 1)
            {
                whereClauses.Add("MediaType=@MediaTypes");
                if (statement != null)
                {
                    statement.TryBind("@MediaTypes", queryMediaTypes[0]);
                }
            }
            else if (queryMediaTypes.Length > 1)
            {
                var val = string.Join(",", queryMediaTypes.Select(i => "'" + i + "'").ToArray());

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

                whereClauses.Add("(" + string.Join(" OR ", includeIds.ToArray()) + ")");
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

            var includedItemByNameTypes = GetItemByNameTypesInQuery(query).SelectMany(MapIncludeItemTypes).ToList();
            var enableItemsByName = (query.IncludeItemsByName ?? false) && includedItemByNameTypes.Count > 0;

            var queryTopParentIds = query.TopParentIds.Where(IsValidId).ToArray();

            if (queryTopParentIds.Length == 1)
            {
                if (enableItemsByName && includedItemByNameTypes.Count == 1)
                {
                    whereClauses.Add("(TopParentId=@TopParentId or Type=@IncludedItemByNameType)");
                    if (statement != null)
                    {
                        statement.TryBind("@IncludedItemByNameType", includedItemByNameTypes[0]);
                    }
                }
                else if (enableItemsByName && includedItemByNameTypes.Count > 1)
                {
                    var itemByNameTypeVal = string.Join(",", includedItemByNameTypes.Select(i => "'" + i + "'").ToArray());
                    whereClauses.Add("(TopParentId=@TopParentId or Type in (" + itemByNameTypeVal + "))");
                }
                else
                {
                    whereClauses.Add("(TopParentId=@TopParentId)");
                }
                if (statement != null)
                {
                    statement.TryBind("@TopParentId", queryTopParentIds[0]);
                }
            }
            else if (queryTopParentIds.Length > 1)
            {
                var val = string.Join(",", queryTopParentIds.Select(i => "'" + i + "'").ToArray());

                if (enableItemsByName && includedItemByNameTypes.Count == 1)
                {
                    whereClauses.Add("(Type=@IncludedItemByNameType or TopParentId in (" + val + "))");
                    if (statement != null)
                    {
                        statement.TryBind("@IncludedItemByNameType", includedItemByNameTypes[0]);
                    }
                }
                else if (enableItemsByName && includedItemByNameTypes.Count > 1)
                {
                    var itemByNameTypeVal = string.Join(",", includedItemByNameTypes.Select(i => "'" + i + "'").ToArray());
                    whereClauses.Add("(Type in (" + itemByNameTypeVal + ") or TopParentId in (" + val + "))");
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

            if (query.ExcludeInheritedTags.Length > 0)
            {
                var tagValues = query.ExcludeInheritedTags.Select(i => "'" + GetCleanValue(i) + "'").ToArray();
                var tagValuesList = string.Join(",", tagValues);

                whereClauses.Add("((select CleanValue from itemvalues where ItemId=Guid and Type=6 and cleanvalue in (" + tagValuesList + ")) is null)");
            }

            return whereClauses;
        }

        private List<string> GetItemByNameTypesInQuery(InternalItemsQuery query)
        {
            var list = new List<string>();

            if (IsTypeInQuery(typeof(Person).Name, query))
            {
                list.Add(typeof(Person).Name);
            }
            if (IsTypeInQuery(typeof(Genre).Name, query))
            {
                list.Add(typeof(Genre).Name);
            }
            if (IsTypeInQuery(typeof(MusicGenre).Name, query))
            {
                list.Add(typeof(MusicGenre).Name);
            }
            if (IsTypeInQuery(typeof(GameGenre).Name, query))
            {
                list.Add(typeof(GameGenre).Name);
            }
            if (IsTypeInQuery(typeof(MusicArtist).Name, query))
            {
                list.Add(typeof(MusicArtist).Name);
            }
            if (IsTypeInQuery(typeof(Studio).Name, query))
            {
                list.Add(typeof(Studio).Name);
            }

            return list;
        }

        private bool IsTypeInQuery(string type, InternalItemsQuery query)
        {
            if (query.ExcludeItemTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            return query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(type, StringComparer.OrdinalIgnoreCase);
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

            if (query.GroupBySeriesPresentationUniqueKey)
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

        public void UpdateInheritedValues(CancellationToken cancellationToken)
        {
            UpdateInheritedTags(cancellationToken);
        }

        private void UpdateInheritedTags(CancellationToken cancellationToken)
        {
            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        connection.ExecuteAll(string.Join(";", new string[]
                        {
                            "delete from itemvalues where type = 6",

                            "insert into itemvalues (ItemId, Type, Value, CleanValue)  select ItemId, 6, Value, CleanValue from ItemValues where Type=4",

                            @"insert into itemvalues (ItemId, Type, Value, CleanValue) select AncestorIds.itemid, 6, ItemValues.Value, ItemValues.CleanValue
FROM AncestorIds
LEFT JOIN ItemValues ON (AncestorIds.AncestorId = ItemValues.ItemId)
where AncestorIdText not null and ItemValues.Value not null and ItemValues.Type = 4 "

                        }));

                    }, TransactionMode);
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

            return new[] { value }.Where(IsValidType);
        }

        public void DeleteItem(Guid id, CancellationToken cancellationToken)
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
                        ExecuteWithSingleParam(db, "delete from People where ItemId=@Id", id.ToGuidBlob());

                        // Delete chapters
                        ExecuteWithSingleParam(db, "delete from " + ChaptersTableName + " where ItemId=@Id", id.ToGuidBlob());

                        // Delete media streams
                        ExecuteWithSingleParam(db, "delete from mediastreams where ItemId=@Id", id.ToGuidBlob());

                        // Delete ancestors
                        ExecuteWithSingleParam(db, "delete from AncestorIds where ItemId=@Id", id.ToGuidBlob());

                        // Delete item values
                        ExecuteWithSingleParam(db, "delete from ItemValues where ItemId=@Id", id.ToGuidBlob());

                        // Delete the item
                        ExecuteWithSingleParam(db, "delete from TypedBaseItems where guid=@Id", id.ToGuidBlob());
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
                    statement.TryBind("@ItemId", query.ItemId.ToGuidBlob());
                }
            }
            if (query.AppearsInItemId != Guid.Empty)
            {
                whereClauses.Add("Name in (Select Name from People where ItemId=@AppearsInItemId)");
                if (statement != null)
                {
                    statement.TryBind("@AppearsInItemId", query.AppearsInItemId.ToGuidBlob());
                }
            }
            var queryPersonTypes = query.PersonTypes.Where(IsValidPersonType).ToList();

            if (queryPersonTypes.Count == 1)
            {
                whereClauses.Add("PersonType=@PersonType");
                if (statement != null)
                {
                    statement.TryBind("@PersonType", queryPersonTypes[0]);
                }
            }
            else if (queryPersonTypes.Count > 1)
            {
                var val = string.Join(",", queryPersonTypes.Select(i => "'" + i + "'").ToArray());

                whereClauses.Add("PersonType in (" + val + ")");
            }
            var queryExcludePersonTypes = query.ExcludePersonTypes.Where(IsValidPersonType).ToList();

            if (queryExcludePersonTypes.Count == 1)
            {
                whereClauses.Add("PersonType<>@PersonType");
                if (statement != null)
                {
                    statement.TryBind("@PersonType", queryExcludePersonTypes[0]);
                }
            }
            else if (queryExcludePersonTypes.Count > 1)
            {
                var val = string.Join(",", queryExcludePersonTypes.Select(i => "'" + i + "'").ToArray());

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
            deleteAncestorsStatement.TryBind("@ItemId", itemId.ToGuidBlob());
            deleteAncestorsStatement.MoveNext();

            foreach (var ancestorId in ancestorIds)
            {
                updateAncestorsStatement.Reset();
                updateAncestorsStatement.TryBind("@ItemId", itemId.ToGuidBlob());
                updateAncestorsStatement.TryBind("@AncestorId", ancestorId.ToGuidBlob());
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

            var typesToCount = query.IncludeItemTypes;

            if (typesToCount.Length > 0)
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
                var whereClauses = GetWhereClauses(typeSubQuery, null);

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

            columns = GetFinalColumnsToSelect(query, columns.ToArray()).ToList();

            var commandText = "select " + string.Join(",", columns.ToArray()) + GetFromText();
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

            if (typesToCount.Length == 0)
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

                        var statements = PrepareAllSafe(db, statementTexts);

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
                                    GetWhereClauses(typeSubQuery, null);
                                }
                                BindSimilarParams(query, statement);
                                GetWhereClauses(innerQuery, statement);
                                GetWhereClauses(outerQuery, statement);

                                var hasEpisodeAttributes = HasEpisodeAttributes(query);
                                var hasProgramAttributes = HasProgramAttributes(query);
                                var hasStartDate = HasStartDate(query);
                                var hasTrailerTypes = HasTrailerTypes(query);
                                var hasArtistFields = HasArtistFields(query);
                                var hasSeriesFields = HasSeriesFields(query);

                                foreach (var row in statement.ExecuteQuery())
                                {
                                    var item = GetItem(row, query, hasProgramAttributes, hasEpisodeAttributes, hasStartDate, hasTrailerTypes, hasArtistFields, hasSeriesFields);
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
                                    GetWhereClauses(typeSubQuery, null);
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
                        result.Items = list.ToArray(list.Count);

                        return result;

                    }, ReadTransactionMode);
                }
            }
        }

        private ItemCounts GetItemCounts(IReadOnlyList<IResultSetValue> reader, int countStartColumn, string[] typesToCount)
        {
            var counts = new ItemCounts();

            if (typesToCount.Length == 0)
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

        private List<Tuple<int, string>> GetItemValuesToSave(BaseItem item, List<string> inheritedTags)
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

            // keywords was 5

            list.AddRange(inheritedTags.Select(i => new Tuple<int, string>(6, i)));

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

            var guidBlob = itemId.ToGuidBlob();

            // First delete 
            db.Execute("delete from ItemValues where ItemId=@Id", guidBlob);

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

                    statement.TryBind("@ItemId", guidBlob);
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

        public void UpdatePeople(Guid itemId, List<PersonInfo> people)
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
                    connection.Execute("delete from People where ItemId=?", itemId.ToGuidBlob());

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

                            statement.TryBind("@ItemId", itemId.ToGuidBlob());
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

        public List<MediaStream> GetMediaStreams(MediaStreamQuery query)
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
                        statement.TryBind("@ItemId", query.ItemId.ToGuidBlob());

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

        public void SaveMediaStreams(Guid id, List<MediaStream> streams, CancellationToken cancellationToken)
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
                    connection.Execute("delete from mediastreams where ItemId=@ItemId", id.ToGuidBlob());

                    using (var statement = PrepareStatement(connection, string.Format("replace into mediastreams ({0}) values ({1})",
                        string.Join(",", _mediaStreamSaveColumns),
                        string.Join(",", _mediaStreamSaveColumns.Select(i => "@" + i).ToArray()))))
                    {
                        foreach (var stream in streams)
                        {
                            var paramList = new List<object>();

                            paramList.Add(id.ToGuidBlob());
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