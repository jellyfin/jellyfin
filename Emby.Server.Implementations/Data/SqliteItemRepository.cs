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
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Reflection;
using SQLitePCL.pretty;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Threading;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;

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

        private readonly IFileSystem _fileSystem;
        private readonly IEnvironmentInfo _environmentInfo;
        private IServerApplicationHost _appHost;

        public IImageProcessor ImageProcessor { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteItemRepository"/> class.
        /// </summary>
        public SqliteItemRepository(IServerConfigurationManager config, IServerApplicationHost appHost, IJsonSerializer jsonSerializer, ILogger logger, IAssemblyInfo assemblyInfo, IFileSystem fileSystem, IEnvironmentInfo environmentInfo, ITimerFactory timerFactory)
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

            _appHost = appHost;
            _config = config;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
            _environmentInfo = environmentInfo;
            _typeMapper = new TypeMapper(assemblyInfo);

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

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        public void Initialize(SqliteUserDataRepository userDataRepo, IUserManager userManager)
        {
            using (var connection = CreateConnection())
            {
                RunDefaultInitialization(connection);

                var createMediaStreamsTableCommand
                    = "create table if not exists mediastreams (ItemId GUID, StreamIndex INT, StreamType TEXT, Codec TEXT, Language TEXT, ChannelLayout TEXT, Profile TEXT, AspectRatio TEXT, Path TEXT, IsInterlaced BIT, BitRate INT NULL, Channels INT NULL, SampleRate INT NULL, IsDefault BIT, IsForced BIT, IsExternal BIT, Height INT NULL, Width INT NULL, AverageFrameRate FLOAT NULL, RealFrameRate FLOAT NULL, Level FLOAT NULL, PixelFormat TEXT, BitDepth INT NULL, IsAnamorphic BIT NULL, RefFrames INT NULL, CodecTag TEXT NULL, Comment TEXT NULL, NalLengthSize TEXT NULL, IsAvc BIT NULL, Title TEXT NULL, TimeBase TEXT NULL, CodecTimeBase TEXT NULL, ColorPrimaries TEXT NULL, ColorSpace TEXT NULL, ColorTransfer TEXT NULL, PRIMARY KEY (ItemId, StreamIndex))";

                string[] queries = {
                    "PRAGMA locking_mode=EXCLUSIVE",

                    "create table if not exists TypedBaseItems (guid GUID primary key NOT NULL, type TEXT NOT NULL, data BLOB NULL, ParentId GUID NULL, Path TEXT NULL)",

                    "create table if not exists AncestorIds (ItemId GUID NOT NULL, AncestorId GUID NOT NULL, AncestorIdText TEXT NOT NULL, PRIMARY KEY (ItemId, AncestorId))",
                    "create index if not exists idx_AncestorIds1 on AncestorIds(AncestorId)",
                    "create index if not exists idx_AncestorIds5 on AncestorIds(AncestorIdText,ItemId)",

                    "create table if not exists ItemValues (ItemId GUID NOT NULL, Type INT NOT NULL, Value TEXT NOT NULL, CleanValue TEXT NOT NULL)",

                    "create table if not exists People (ItemId GUID, Name TEXT NOT NULL, Role TEXT, PersonType TEXT, SortOrder int, ListOrder int)",

                    "drop index if exists idxPeopleItemId",
                    "create index if not exists idxPeopleItemId1 on People(ItemId,ListOrder)",
                    "create index if not exists idxPeopleName on People(Name)",

                    "create table if not exists "+ChaptersTableName+" (ItemId GUID, ChapterIndex INT NOT NULL, StartPositionTicks BIGINT NOT NULL, Name TEXT, ImagePath TEXT, PRIMARY KEY (ItemId, ChapterIndex))",

                    createMediaStreamsTableCommand,

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
                    AddColumn(db, "TypedBaseItems", "DateCreated", "DATETIME", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "DateModified", "DATETIME", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsSeries", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "EpisodeTitle", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "IsRepeat", "BIT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "PreferredMetadataLanguage", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "PreferredMetadataCountryCode", "Text", existingColumnNames);
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
                    AddColumn(db, "TypedBaseItems", "ExtraIds", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "TotalBitrate", "INT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ExtraType", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Artists", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "AlbumArtists", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ExternalId", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "SeriesPresentationUniqueKey", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "ShowId", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "OwnerId", "Text", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Width", "INT", existingColumnNames);
                    AddColumn(db, "TypedBaseItems", "Height", "INT", existingColumnNames);

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

                    AddColumn(db, "MediaStreams", "ColorPrimaries", "TEXT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "ColorSpace", "TEXT", existingColumnNames);
                    AddColumn(db, "MediaStreams", "ColorTransfer", "TEXT", existingColumnNames);

                }, TransactionMode);

                string[] postQueries =

                {
                    // obsolete
                    "drop index if exists idx_TypedBaseItems",
                    "drop index if exists idx_mediastreams",
                    "drop index if exists idx_mediastreams1",
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

            userDataRepo.Initialize(WriteLock, _connection, userManager);
        }

        private readonly string[] _retriveItemColumns =
        {
            "type",
            "data",
            "StartDate",
            "EndDate",
            "ChannelId",
            "IsMovie",
            "IsSeries",
            "EpisodeTitle",
            "IsRepeat",
            "CommunityRating",
            "CustomRating",
            "IndexNumber",
            "IsLocked",
            "PreferredMetadataLanguage",
            "PreferredMetadataCountryCode",
            "Width",
            "Height",
            "DateLastRefreshed",
            "Name",
            "Path",
            "PremiereDate",
            "Overview",
            "ParentIndexNumber",
            "ProductionYear",
            "OfficialRating",
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
            "ExtraIds",
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
            "CodecTimeBase",
            "ColorPrimaries",
            "ColorSpace",
            "ColorTransfer"
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
                "IsMovie",
                "IsSeries",
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
                "DateCreated",
                "DateModified",
                "PreferredMetadataLanguage",
                "PreferredMetadataCountryCode",
                "Width",
                "Height",
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
                "ExtraIds",
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

        public void SaveImages(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            CheckDisposed();

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        using (var saveImagesStatement = PrepareStatement(db, "Update TypedBaseItems set Images=@Images where guid=@Id"))
                        {
                            saveImagesStatement.TryBind("@Id", item.Id.ToGuidBlob());
                            saveImagesStatement.TryBind("@Images", SerializeImages(item));

                            saveImagesStatement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
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
                "delete from AncestorIds where ItemId=@ItemId"

            }).ToList();

            using (var saveItemStatement = statements[0])
            {
                using (var deleteAncestorsStatement = statements[1])
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
                        //logger.LogDebug(_saveItemCommand.CommandText);

                        var inheritedTags = tuple.Item5;

                        if (item.SupportsAncestors)
                        {
                            UpdateAncestors(item.Id, tuple.Item2, db, deleteAncestorsStatement);
                        }

                        UpdateItemValues(item.Id, GetItemValuesToSave(item, inheritedTags), db);

                        requiresReset = true;
                    }
                }
            }
        }

        private string GetPathToSave(string path)
        {
            if (path == null)
            {
                return null;
            }

            return _appHost.ReverseVirtualPath(path);
        }

        private string RestorePath(string path)
        {
            return _appHost.ExpandVirtualPath(path);
        }

        private void SaveItem(BaseItem item, BaseItem topParent, string userDataKey, IStatement saveItemStatement)
        {
            saveItemStatement.TryBind("@guid", item.Id);
            saveItemStatement.TryBind("@type", item.GetType().FullName);

            if (TypeRequiresDeserialization(item.GetType()))
            {
                saveItemStatement.TryBind("@data", _jsonSerializer.SerializeToBytes(item));
            }
            else
            {
                saveItemStatement.TryBindNull("@data");
            }

            saveItemStatement.TryBind("@Path", GetPathToSave(item.Path));

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

            saveItemStatement.TryBind("@ChannelId", item.ChannelId.Equals(Guid.Empty) ? null : item.ChannelId.ToString("N"));

            var hasProgramAttributes = item as IHasProgramAttributes;
            if (hasProgramAttributes != null)
            {
                saveItemStatement.TryBind("@IsMovie", hasProgramAttributes.IsMovie);
                saveItemStatement.TryBind("@IsSeries", hasProgramAttributes.IsSeries);
                saveItemStatement.TryBind("@EpisodeTitle", hasProgramAttributes.EpisodeTitle);
                saveItemStatement.TryBind("@IsRepeat", hasProgramAttributes.IsRepeat);
            }
            else
            {
                saveItemStatement.TryBindNull("@IsMovie");
                saveItemStatement.TryBindNull("@IsSeries");
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
            if (parentId.Equals(Guid.Empty))
            {
                saveItemStatement.TryBindNull("@ParentId");
            }
            else
            {
                saveItemStatement.TryBind("@ParentId", parentId);
            }

            if (item.Genres.Length > 0)
            {
                saveItemStatement.TryBind("@Genres", string.Join("|", item.Genres));
            }
            else
            {
                saveItemStatement.TryBindNull("@Genres");
            }

            saveItemStatement.TryBind("@InheritedParentalRatingValue", item.InheritedParentalRatingValue);

            saveItemStatement.TryBind("@SortName", item.SortName);

            saveItemStatement.TryBind("@ForcedSortName", item.ForcedSortName);

            saveItemStatement.TryBind("@RunTimeTicks", item.RunTimeTicks);

            saveItemStatement.TryBind("@DateCreated", item.DateCreated);
            saveItemStatement.TryBind("@DateModified", item.DateModified);

            saveItemStatement.TryBind("@PreferredMetadataLanguage", item.PreferredMetadataLanguage);
            saveItemStatement.TryBind("@PreferredMetadataCountryCode", item.PreferredMetadataCountryCode);

            if (item.Width > 0)
            {
                saveItemStatement.TryBind("@Width", item.Width);
            }
            else
            {
                saveItemStatement.TryBindNull("@Width");
            }
            if (item.Height > 0)
            {
                saveItemStatement.TryBind("@Height", item.Height);
            }
            else
            {
                saveItemStatement.TryBindNull("@Height");
            }

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

            var livetvChannel = item as LiveTvChannel;
            if (livetvChannel != null)
            {
                saveItemStatement.TryBind("@ExternalServiceId", livetvChannel.ServiceName);
            }
            else
            {
                saveItemStatement.TryBindNull("@ExternalServiceId");
            }

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
                //logger.LogDebug("Item {0} has top parent {1}", item.Id, topParent.Id);
                saveItemStatement.TryBind("@TopParentId", topParent.Id.ToString("N"));
            }
            else
            {
                //logger.LogDebug("Item {0} has null top parent", item.Id);
                saveItemStatement.TryBindNull("@TopParentId");
            }

            var trailer = item as Trailer;
            if (trailer != null && trailer.TrailerTypes.Length > 0)
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

            var hasSeriesName = item as IHasSeries;
            if (hasSeriesName != null)
            {
                saveItemStatement.TryBind("@SeriesName", hasSeriesName.SeriesName);
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

                var nullableSeasonId = episode.SeasonId.Equals(Guid.Empty) ? (Guid?)null : episode.SeasonId;

                saveItemStatement.TryBind("@SeasonId", nullableSeasonId);
            }
            else
            {
                saveItemStatement.TryBindNull("@SeasonName");
                saveItemStatement.TryBindNull("@SeasonId");
            }

            var hasSeries = item as IHasSeries;
            if (hasSeries != null)
            {
                var nullableSeriesId = hasSeries.SeriesId.Equals(Guid.Empty) ? (Guid?)null : hasSeries.SeriesId;

                saveItemStatement.TryBind("@SeriesId", nullableSeriesId);
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

            if (item.ExtraIds.Length > 0)
            {
                saveItemStatement.TryBind("@ExtraIds", string.Join("|", item.ExtraIds.ToArray()));
            }
            else
            {
                saveItemStatement.TryBindNull("@ExtraIds");
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
            if (!ownerId.Equals(Guid.Empty))
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

            item.ImageInfos = list.ToArray();
        }

        public string ToValueString(ItemImageInfo image)
        {
            var delimeter = "*";

            var path = image.Path;

            if (path == null)
            {
                path = string.Empty;
            }

            return GetPathToSave(path) +
                   delimeter +
                   image.DateModified.Ticks.ToString(CultureInfo.InvariantCulture) +
                   delimeter +
                   image.Type +
                   delimeter +
                   image.Width.ToString(CultureInfo.InvariantCulture) +
                   delimeter +
                   image.Height.ToString(CultureInfo.InvariantCulture);
        }

        public ItemImageInfo ItemImageInfoFromValueString(string value)
        {
            var parts = value.Split(new[] { '*' }, StringSplitOptions.None);

            if (parts.Length < 3)
            {
                return null;
            }

            var image = new ItemImageInfo();

            image.Path = RestorePath(parts[0]);

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

            if (parts.Length >= 5)
            {
                int width;
                int height;
                if (int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out width))
                {
                    if (int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out height))
                    {
                        image.Width = width;
                        image.Height = height;
                    }
                }
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
            if (id.Equals(Guid.Empty))
            {
                throw new ArgumentNullException("id");
            }

            CheckDisposed();
            //logger.LogInformation("Retrieving item {0}", id.ToString("N"));
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
            if (type == typeof(LiveTvProgram))
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
            return GetItem(reader, query, HasProgramAttributes(query), HasEpisodeAttributes(query), HasServiceName(query), HasStartDate(query), HasTrailerTypes(query), HasArtistFields(query), HasSeriesFields(query));
        }

        private BaseItem GetItem(IReadOnlyList<IResultSetValue> reader, InternalItemsQuery query, bool enableProgramAttributes, bool hasEpisodeAttributes, bool hasServiceName, bool queryHasStartDate, bool hasTrailerTypes, bool hasArtistFields, bool hasSeriesFields)
        {
            var typeString = reader.GetString(0);

            var type = _typeMapper.GetType(typeString);

            if (type == null)
            {
                //logger.LogDebug("Unknown type {0}", typeString);

                return null;
            }

            BaseItem item = null;

            if (TypeRequiresDeserialization(type))
            {
                using (var stream = new MemoryStream(reader[1].ToBlob()))
                {
                    stream.Position = 0;

                    try
                    {
                        item = _jsonSerializer.DeserializeFromStream(stream, type) as BaseItem;
                    }
                    catch (SerializationException ex)
                    {
                        Logger.LogError(ex, "Error deserializing item");
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
                item.EndDate = reader[index].TryReadDateTime();
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.ChannelId = new Guid(reader.GetString(index));
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
                        hasProgramAttributes.IsSeries = reader.GetBoolean(index);
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
                    index += 4;
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

            if (HasField(query, ItemFields.Width))
            {
                if (!reader.IsDBNull(index))
                {
                    item.Width = reader.GetInt32(index);
                }
                index++;
            }

            if (HasField(query, ItemFields.Height))
            {
                if (!reader.IsDBNull(index))
                {
                    item.Height = reader.GetInt32(index);
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
                item.Path = RestorePath(reader.GetString(index));
            }
            index++;

            if (!reader.IsDBNull(index))
            {
                item.PremiereDate = reader[index].TryReadDateTime();
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
                    item.Genres = reader.GetString(index).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
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
            if (hasServiceName)
            {
                var livetvChannel = item as LiveTvChannel;
                if (livetvChannel != null)
                {
                    if (!reader.IsDBNull(index))
                    {
                        livetvChannel.ServiceName = reader.GetString(index);
                    }
                }
                index++;
            }

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

                            }).Where(i => i.HasValue).Select(i => i.Value).ToArray();
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
                    folder.DateLastMediaAdded = reader[index].TryReadDateTime();
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

            var hasSeriesName = item as IHasSeries;
            if (hasSeriesName != null)
            {
                if (!reader.IsDBNull(index))
                {
                    hasSeriesName.SeriesName = reader.GetString(index);
                }
            }
            index++;

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

            var hasSeries = item as IHasSeries;
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

            if (HasField(query, ItemFields.ExtraIds))
            {
                if (!reader.IsDBNull(index))
                {
                    item.ExtraIds = SplitToGuids(reader.GetString(index));
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
        /// Gets chapters for an item
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>IEnumerable{ChapterInfo}.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public List<ChapterInfo> GetChapters(BaseItem item)
        {
            CheckDisposed();

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var list = new List<ChapterInfo>();

                    using (var statement = PrepareStatementSafe(connection, "select StartPositionTicks,Name,ImagePath,ImageDateModified from " + ChaptersTableName + " where ItemId = @ItemId order by ChapterIndex asc"))
                    {
                        statement.TryBind("@ItemId", item.Id);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            list.Add(GetChapter(row, item));
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
        public ChapterInfo GetChapter(BaseItem item, int index)
        {
            CheckDisposed();

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = PrepareStatementSafe(connection, "select StartPositionTicks,Name,ImagePath,ImageDateModified from " + ChaptersTableName + " where ItemId = @ItemId and ChapterIndex=@ChapterIndex"))
                    {
                        statement.TryBind("@ItemId", item.Id);
                        statement.TryBind("@ChapterIndex", index);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            return GetChapter(row, item);
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
        private ChapterInfo GetChapter(IReadOnlyList<IResultSetValue> reader, BaseItem item)
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

                if (!string.IsNullOrEmpty(chapter.ImagePath))
                {
                    chapter.ImageTag = ImageProcessor.GetImageCacheTag(item, chapter);
                }
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

            if (id.Equals(Guid.Empty))
            {
                throw new ArgumentNullException("id");
            }

            if (chapters == null)
            {
                throw new ArgumentNullException("chapters");
            }

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        var idBlob = id.ToGuidBlob();

                        // First delete chapters
                        db.Execute("delete from " + ChaptersTableName + " where ItemId=@ItemId", idBlob);

                        InsertChapters(idBlob, chapters, db);

                    }, TransactionMode);
                }
            }
        }

        private void InsertChapters(byte[] idBlob, List<ChapterInfo> chapters, IDatabaseConnection db)
        {
            var startIndex = 0;
            var limit = 100;
            var chapterIndex = 0;

            while (startIndex < chapters.Count)
            {
                var insertText = new StringBuilder("insert into " + ChaptersTableName + " (ItemId, ChapterIndex, StartPositionTicks, Name, ImagePath, ImageDateModified) values ");

                var endIndex = Math.Min(chapters.Count, startIndex + limit);
                var isSubsequentRow = false;

                for (var i = startIndex; i < endIndex; i++)
                {
                    if (isSubsequentRow)
                    {
                        insertText.Append(",");
                    }

                    insertText.AppendFormat("(@ItemId, @ChapterIndex{0}, @StartPositionTicks{0}, @Name{0}, @ImagePath{0}, @ImageDateModified{0})", i.ToString(CultureInfo.InvariantCulture));
                    isSubsequentRow = true;
                }

                using (var statement = PrepareStatementSafe(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", idBlob);

                    for (var i = startIndex; i < endIndex; i++)
                    {
                        var index = i.ToString(CultureInfo.InvariantCulture);

                        var chapter = chapters[i];

                        statement.TryBind("@ChapterIndex" + index, chapterIndex);
                        statement.TryBind("@StartPositionTicks" + index, chapter.StartPositionTicks);
                        statement.TryBind("@Name" + index, chapter.Name);
                        statement.TryBind("@ImagePath" + index, chapter.ImagePath);
                        statement.TryBind("@ImageDateModified" + index, chapter.ImageDateModified);

                        chapterIndex++;
                    }

                    statement.Reset();
                    statement.MoveNext();
                }

                startIndex += limit;
            }
        }

        private bool EnableJoinUserData(InternalItemsQuery query)
        {
            if (query.User == null)
            {
                return false;
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

        private string[] GetColumnNamesFromField(ItemFields field)
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
            if (field == ItemFields.IsHD)
            {
                return Array.Empty<string>();
            }

            return new[] { field.ToString() };
        }

        private bool HasField(InternalItemsQuery query, ItemFields name)
        {
            switch (name)
            {
                case ItemFields.Tags:
                    return query.DtoOptions.ContainsField(name) || HasProgramAttributes(query);
                case ItemFields.CustomRating:
                case ItemFields.ProductionLocations:
                case ItemFields.Settings:
                case ItemFields.OriginalTitle:
                case ItemFields.Taglines:
                case ItemFields.SortName:
                case ItemFields.Studios:
                case ItemFields.ExtraIds:
                case ItemFields.DateCreated:
                case ItemFields.Overview:
                case ItemFields.Genres:
                case ItemFields.DateLastMediaAdded:
                case ItemFields.PresentationUniqueKey:
                case ItemFields.InheritedParentalRatingValue:
                case ItemFields.ExternalSeriesId:
                case ItemFields.SeriesPresentationUniqueKey:
                case ItemFields.DateLastRefreshed:
                case ItemFields.DateLastSaved:
                    return query.DtoOptions.ContainsField(name);
                case ItemFields.ServiceName:
                    return HasServiceName(query);
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
                "TvChannel",
                "LiveTvProgram",
                "LiveTvTvChannel"
            };

            return types.Any(i => query.IncludeItemTypes.Contains(i, StringComparer.OrdinalIgnoreCase));
        }

        private bool HasServiceName(InternalItemsQuery query)
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
                "TvChannel",
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
                "AudioPodcast"
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
                list.Remove("IsMovie");
                list.Remove("IsSeries");
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
                list.Add("UserDatas.UserId");
                list.Add("UserDatas.lastPlayedDate");
                list.Add("UserDatas.playbackPositionTicks");
                list.Add("UserDatas.playcount");
                list.Add("UserDatas.isFavorite");
                list.Add("UserDatas.played");
                list.Add("UserDatas.rating");
            }

            if (query.SimilarTo != null)
            {
                var item = query.SimilarTo;

                var builder = new StringBuilder();
                builder.Append("(");

                if (string.IsNullOrEmpty(item.OfficialRating))
                {
                    builder.Append("((OfficialRating is null) * 10)");
                }
                else
                {
                    builder.Append("((OfficialRating=@ItemOfficialRating) * 10)");
                }

                if (item.ProductionYear.HasValue)
                {
                    //builder.Append("+ ((ProductionYear=@ItemProductionYear) * 10)");
                    builder.Append("+(Select Case When Abs(COALESCE(ProductionYear, 0) - @ItemProductionYear) < 10 Then 10 Else 0 End )");
                    builder.Append("+(Select Case When Abs(COALESCE(ProductionYear, 0) - @ItemProductionYear) < 5 Then 5 Else 0 End )");
                }

                //// genres, tags
                builder.Append("+ ((Select count(CleanValue) from ItemValues where ItemId=Guid and CleanValue in (select CleanValue from itemvalues where ItemId=@SimilarItemId)) * 10)");

                //builder.Append("+ ((Select count(CleanValue) from ItemValues where ItemId=Guid and Type=3 and CleanValue in (select CleanValue from itemvalues where ItemId=@SimilarItemId and type=3)) * 3)");

                //builder.Append("+ ((Select count(Name) from People where ItemId=Guid and Name in (select Name from People where ItemId=@SimilarItemId)) * 3)");

                ////builder.Append("(select group_concat((Select Name from People where ItemId=Guid and Name in (Select Name from People where ItemId=@SimilarItemId)), '|'))");

                builder.Append(") as SimilarityScore");

                list.Add(builder.ToString());

                var excludeIds = query.ExcludeItemIds.ToList();
                excludeIds.Add(item.Id);
                excludeIds.AddRange(item.ExtraIds);

                query.ExcludeItemIds = excludeIds.ToArray();
                query.ExcludeProviderIds = item.ProviderIds;
            }

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                var builder = new StringBuilder();
                builder.Append("(");

                builder.Append("((CleanName like @SearchTermStartsWith or (OriginalTitle not null and OriginalTitle like @SearchTermStartsWith)) * 10)");

                if (query.SearchTerm.Length > 1)
                {
                    builder.Append("+ ((CleanName like @SearchTermContains or (OriginalTitle not null and OriginalTitle like @SearchTermContains)) * 10)");
                }

                builder.Append(") as SearchScore");

                list.Add(builder.ToString());
            }

            return list.ToArray();
        }

        private void BindSearchParams(InternalItemsQuery query, IStatement statement)
        {
            var searchTerm = query.SearchTerm;

            if (string.IsNullOrEmpty(searchTerm))
            {
                return;
            }

            searchTerm = FixUnicodeChars(searchTerm);
            searchTerm = GetCleanValue(searchTerm);

            var commandText = statement.SQL;
            if (commandText.IndexOf("@SearchTermStartsWith", StringComparison.OrdinalIgnoreCase) != -1)
            {
                statement.TryBind("@SearchTermStartsWith", searchTerm + "%");
            }
            if (commandText.IndexOf("@SearchTermContains", StringComparison.OrdinalIgnoreCase) != -1)
            {
                statement.TryBind("@SearchTermContains", "%" + searchTerm + "%");
            }
        }

        private void BindSimilarParams(InternalItemsQuery query, IStatement statement)
        {
            var item = query.SimilarTo;

            if (item == null)
            {
                return;
            }

            var commandText = statement.SQL;

            if (commandText.IndexOf("@ItemOfficialRating", StringComparison.OrdinalIgnoreCase) != -1)
            {
                statement.TryBind("@ItemOfficialRating", item.OfficialRating);
            }

            if (commandText.IndexOf("@ItemProductionYear", StringComparison.OrdinalIgnoreCase) != -1)
            {
                statement.TryBind("@ItemProductionYear", item.ProductionYear ?? 0);
            }

            if (commandText.IndexOf("@SimilarItemId", StringComparison.OrdinalIgnoreCase) != -1)
            {
                statement.TryBind("@SimilarItemId", item.Id);
            }
        }

        private string GetJoinUserDataText(InternalItemsQuery query)
        {
            if (!EnableJoinUserData(query))
            {
                return string.Empty;
            }

            return " left join UserDatas on UserDataKey=UserDatas.Key And (UserId=@UserId)";
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

            //logger.LogInformation("GetItemList: " + _environmentInfo.StackTrace);

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
                            statement.TryBind("@UserId", query.User.InternalId);
                        }

                        BindSimilarParams(query, statement);
                        BindSearchParams(query, statement);

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

            //logger.LogInformation("GetItemList: " + _environmentInfo.StackTrace);

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
                            statement.TryBind("@UserId", query.User.InternalId);
                        }

                        BindSimilarParams(query, statement);
                        BindSearchParams(query, statement);

                        // Running this again will bind the params
                        GetWhereClauses(query, statement);

                        var hasEpisodeAttributes = HasEpisodeAttributes(query);
                        var hasServiceName = HasServiceName(query);
                        var hasProgramAttributes = HasProgramAttributes(query);
                        var hasStartDate = HasStartDate(query);
                        var hasTrailerTypes = HasTrailerTypes(query);
                        var hasArtistFields = HasArtistFields(query);
                        var hasSeriesFields = HasSeriesFields(query);

                        foreach (var row in statement.ExecuteQuery())
                        {
                            var item = GetItem(row, query, hasProgramAttributes, hasEpisodeAttributes, hasServiceName, hasStartDate, hasTrailerTypes, hasArtistFields, hasSeriesFields);
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

        private string FixUnicodeChars(string buffer)
        {
            if (buffer.IndexOf('\u2013') > -1) buffer = buffer.Replace('\u2013', '-'); // en dash
            if (buffer.IndexOf('\u2014') > -1) buffer = buffer.Replace('\u2014', '-'); // em dash
            if (buffer.IndexOf('\u2015') > -1) buffer = buffer.Replace('\u2015', '-'); // horizontal bar
            if (buffer.IndexOf('\u2017') > -1) buffer = buffer.Replace('\u2017', '_'); // double low line
            if (buffer.IndexOf('\u2018') > -1) buffer = buffer.Replace('\u2018', '\''); // left single quotation mark
            if (buffer.IndexOf('\u2019') > -1) buffer = buffer.Replace('\u2019', '\''); // right single quotation mark
            if (buffer.IndexOf('\u201a') > -1) buffer = buffer.Replace('\u201a', ','); // single low-9 quotation mark
            if (buffer.IndexOf('\u201b') > -1) buffer = buffer.Replace('\u201b', '\''); // single high-reversed-9 quotation mark
            if (buffer.IndexOf('\u201c') > -1) buffer = buffer.Replace('\u201c', '\"'); // left double quotation mark
            if (buffer.IndexOf('\u201d') > -1) buffer = buffer.Replace('\u201d', '\"'); // right double quotation mark
            if (buffer.IndexOf('\u201e') > -1) buffer = buffer.Replace('\u201e', '\"'); // double low-9 quotation mark
            if (buffer.IndexOf('\u2026') > -1) buffer = buffer.Replace("\u2026", "..."); // horizontal ellipsis
            if (buffer.IndexOf('\u2032') > -1) buffer = buffer.Replace('\u2032', '\''); // prime
            if (buffer.IndexOf('\u2033') > -1) buffer = buffer.Replace('\u2033', '\"'); // double prime
            if (buffer.IndexOf('\u0060') > -1) buffer = buffer.Replace('\u0060', '\''); // grave accent
            if (buffer.IndexOf('\u00B4') > -1) buffer = buffer.Replace('\u00B4', '\''); // acute accent

            return buffer;
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
                Logger.LogDebug("{2} query time (slow): {0}ms. Query: {1}",
                    Convert.ToInt32(elapsed),
                    commandText,
                    methodName);
            }
            else
            {
                //logger.LogDebug("{2} query time: {0}ms. Query: {1}",
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
            //logger.LogInformation("GetItems: " + _environmentInfo.StackTrace);

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
                    commandText += " select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "count (distinct PresentationUniqueKey)" })) + GetFromText();
                }
                else if (query.GroupBySeriesPresentationUniqueKey)
                {
                    commandText += " select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "count (distinct SeriesPresentationUniqueKey)" })) + GetFromText();
                }
                else
                {
                    commandText += " select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "count (guid)" })) + GetFromText();
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
                                    statement.TryBind("@UserId", query.User.InternalId);
                                }

                                BindSimilarParams(query, statement);
                                BindSearchParams(query, statement);

                                // Running this again will bind the params
                                GetWhereClauses(query, statement);

                                var hasEpisodeAttributes = HasEpisodeAttributes(query);
                                var hasServiceName = HasServiceName(query);
                                var hasProgramAttributes = HasProgramAttributes(query);
                                var hasStartDate = HasStartDate(query);
                                var hasTrailerTypes = HasTrailerTypes(query);
                                var hasArtistFields = HasArtistFields(query);
                                var hasSeriesFields = HasSeriesFields(query);

                                foreach (var row in statement.ExecuteQuery())
                                {
                                    var item = GetItem(row, query, hasProgramAttributes, hasEpisodeAttributes, hasServiceName, hasStartDate, hasTrailerTypes, hasArtistFields, hasSeriesFields);
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
                                    statement.TryBind("@UserId", query.User.InternalId);
                                }

                                BindSimilarParams(query, statement);
                                BindSearchParams(query, statement);

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
            var enableOrderInversion = false;

            if (query.SimilarTo != null)
            {
                if (orderBy.Count == 0)
                {
                    orderBy.Add(new ValueTuple<string, SortOrder>("SimilarityScore", SortOrder.Descending));
                    orderBy.Add(new ValueTuple<string, SortOrder>(ItemSortBy.Random, SortOrder.Ascending));
                    //orderBy.Add(new Tuple<string, SortOrder>(ItemSortBy.Random, SortOrder.Ascending));
                }
            }

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                orderBy = new List<(string, SortOrder)>();
                orderBy.Add(new ValueTuple<string, SortOrder>("SearchScore", SortOrder.Descending));
                orderBy.Add(new ValueTuple<string, SortOrder>(ItemSortBy.SortName, SortOrder.Ascending));
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

        private ValueTuple<string, bool> MapOrderByField(string name, InternalItemsQuery query)
        {
            if (string.Equals(name, ItemSortBy.AirTime, StringComparison.OrdinalIgnoreCase))
            {
                // TODO
                return new ValueTuple<string, bool>("SortName", false);
            }
            if (string.Equals(name, ItemSortBy.Runtime, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("RuntimeTicks", false);
            }
            if (string.Equals(name, ItemSortBy.Random, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("RANDOM()", false);
            }
            if (string.Equals(name, ItemSortBy.DatePlayed, StringComparison.OrdinalIgnoreCase))
            {
                if (query.GroupBySeriesPresentationUniqueKey)
                {
                    return new ValueTuple<string, bool>("MAX(LastPlayedDate)", false);
                }

                return new ValueTuple<string, bool>("LastPlayedDate", false);
            }
            if (string.Equals(name, ItemSortBy.PlayCount, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("PlayCount", false);
            }
            if (string.Equals(name, ItemSortBy.IsFavoriteOrLiked, StringComparison.OrdinalIgnoreCase))
            {
                // (Select Case When Abs(COALESCE(ProductionYear, 0) - @ItemProductionYear) < 10 Then 2 Else 0 End )
                return new ValueTuple<string, bool>("(Select Case When IsFavorite is null Then 0 Else IsFavorite End )", true);
            }
            if (string.Equals(name, ItemSortBy.IsFolder, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("IsFolder", true);
            }
            if (string.Equals(name, ItemSortBy.IsPlayed, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("played", true);
            }
            if (string.Equals(name, ItemSortBy.IsUnplayed, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("played", false);
            }
            if (string.Equals(name, ItemSortBy.DateLastContentAdded, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("DateLastMediaAdded", false);
            }
            if (string.Equals(name, ItemSortBy.Artist, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("(select CleanValue from itemvalues where ItemId=Guid and Type=0 LIMIT 1)", false);
            }
            if (string.Equals(name, ItemSortBy.AlbumArtist, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("(select CleanValue from itemvalues where ItemId=Guid and Type=1 LIMIT 1)", false);
            }
            if (string.Equals(name, ItemSortBy.OfficialRating, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("InheritedParentalRatingValue", false);
            }
            if (string.Equals(name, ItemSortBy.Studio, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("(select CleanValue from itemvalues where ItemId=Guid and Type=3 LIMIT 1)", false);
            }
            if (string.Equals(name, ItemSortBy.SeriesDatePlayed, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("(Select MAX(LastPlayedDate) from TypedBaseItems B" + GetJoinUserDataText(query) + " where Played=1 and B.SeriesPresentationUniqueKey=A.PresentationUniqueKey)", false);
            }
            if (string.Equals(name, ItemSortBy.SeriesSortName, StringComparison.OrdinalIgnoreCase))
            {
                return new ValueTuple<string, bool>("SeriesName", false);
            }

            return new ValueTuple<string, bool>(name, false);
        }

        public List<Guid> GetItemIdsList(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();
            //logger.LogInformation("GetItemIdsList: " + _environmentInfo.StackTrace);

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
                            statement.TryBind("@UserId", query.User.InternalId);
                        }

                        BindSimilarParams(query, statement);
                        BindSearchParams(query, statement);

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
                            statement.TryBind("@UserId", query.User.InternalId);
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
            //logger.LogInformation("GetItemIds: " + _environmentInfo.StackTrace);

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
                    commandText += " select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "count (distinct PresentationUniqueKey)" })) + GetFromText();
                }
                else if (query.GroupBySeriesPresentationUniqueKey)
                {
                    commandText += " select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "count (distinct SeriesPresentationUniqueKey)" })) + GetFromText();
                }
                else
                {
                    commandText += " select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "count (guid)" })) + GetFromText();
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
                                    statement.TryBind("@UserId", query.User.InternalId);
                                }

                                BindSimilarParams(query, statement);
                                BindSearchParams(query, statement);

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
                                    statement.TryBind("@UserId", query.User.InternalId);
                                }

                                BindSimilarParams(query, statement);
                                BindSearchParams(query, statement);

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

            var minWidth = query.MinWidth;
            var maxWidth = query.MaxWidth;

            if (query.IsHD.HasValue)
            {
                var threshold = 1200;
                if (query.IsHD.Value)
                {
                    minWidth = threshold;
                }
                else
                {
                    maxWidth = threshold - 1;
                }
            }

            if (query.Is4K.HasValue)
            {
                var threshold = 3800;
                if (query.Is4K.Value)
                {
                    minWidth = threshold;
                }
                else
                {
                    maxWidth = threshold - 1;
                }
            }

            if (minWidth.HasValue)
            {
                whereClauses.Add("Width>=@MinWidth");
                if (statement != null)
                {
                    statement.TryBind("@MinWidth", minWidth);
                }
            }
            if (query.MinHeight.HasValue)
            {
                whereClauses.Add("Height>=@MinHeight");
                if (statement != null)
                {
                    statement.TryBind("@MinHeight", query.MinHeight);
                }
            }
            if (maxWidth.HasValue)
            {
                whereClauses.Add("Width<=@MaxWidth");
                if (statement != null)
                {
                    statement.TryBind("@MaxWidth", maxWidth);
                }
            }
            if (query.MaxHeight.HasValue)
            {
                whereClauses.Add("Height<=@MaxHeight");
                if (statement != null)
                {
                    statement.TryBind("@MaxHeight", query.MaxHeight);
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

            var tags = query.Tags.ToList();
            var excludeTags = query.ExcludeTags.ToList();

            //if (!(query.IsMovie ?? true) || !(query.IsSeries ?? true))
            //{
            //    if (query.IsMovie.HasValue)
            //    {
            //        var alternateTypes = new List<string>();
            //        if (query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(typeof(Movie).Name))
            //        {
            //            alternateTypes.Add(typeof(Movie).FullName);
            //        }
            //        if (query.IncludeItemTypes.Length == 0 || query.IncludeItemTypes.Contains(typeof(Trailer).Name))
            //        {
            //            alternateTypes.Add(typeof(Trailer).FullName);
            //        }

            //        if (alternateTypes.Count == 0)
            //        {
            //            whereClauses.Add("IsMovie=@IsMovie");
            //            if (statement != null)
            //            {
            //                statement.TryBind("@IsMovie", query.IsMovie);
            //            }
            //        }
            //        else
            //        {
            //            whereClauses.Add("(IsMovie is null OR IsMovie=@IsMovie)");
            //            if (statement != null)
            //            {
            //                statement.TryBind("@IsMovie", query.IsMovie);
            //            }
            //        }
            //    }
            //}
            //else
            //{

            //}

            if (query.IsMovie ?? false)
            {
                var programAttribtues = new List<string>();

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

                whereClauses.Add("(" + string.Join(" OR ", programAttribtues.ToArray()) + ")");
            }
            else if (query.IsMovie.HasValue)
            {
                whereClauses.Add("IsMovie=@IsMovie");
                if (statement != null)
                {
                    statement.TryBind("@IsMovie", query.IsMovie);
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

            if (query.IsSports.HasValue)
            {
                if (query.IsSports.Value)
                {
                    tags.Add("Sports");
                }
                else
                {
                    excludeTags.Add("Sports");
                }
            }

            if (query.IsNews.HasValue)
            {
                if (query.IsNews.Value)
                {
                    tags.Add("News");
                }
                else
                {
                    excludeTags.Add("News");
                }
            }

            if (query.IsKids.HasValue)
            {
                if (query.IsKids.Value)
                {
                    tags.Add("Kids");
                }
                else
                {
                    excludeTags.Add("Kids");
                }
            }

            if (query.SimilarTo != null && query.MinSimilarityScore > 0)
            {
                whereClauses.Add("SimilarityScore > " + (query.MinSimilarityScore - 1).ToString(CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(query.SearchTerm))
            {
                whereClauses.Add("SearchScore > 0");
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

            // Only specify excluded types if no included types are specified
            if (includeTypes.Length == 0)
            {
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
            }

            if (query.ChannelIds.Length == 1)
            {
                whereClauses.Add("ChannelId=@ChannelId");
                if (statement != null)
                {
                    statement.TryBind("@ChannelId", query.ChannelIds[0].ToString("N"));
                }
            }
            else if (query.ChannelIds.Length > 1)
            {
                var inClause = string.Join(",", query.ChannelIds.Select(i => "'" + i.ToString("N") + "'").ToArray());
                whereClauses.Add(string.Format("ChannelId in ({0})", inClause));
            }

            if (!query.ParentId.Equals(Guid.Empty))
            {
                whereClauses.Add("ParentId=@ParentId");
                if (statement != null)
                {
                    statement.TryBind("@ParentId", query.ParentId);
                }
            }

            if (!string.IsNullOrWhiteSpace(query.Path))
            {
                //whereClauses.Add("(Path=@Path COLLATE NOCASE)");
                whereClauses.Add("Path=@Path");
                if (statement != null)
                {
                    statement.TryBind("@Path", GetPathToSave(query.Path));
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

            var minEndDate = query.MinEndDate;
            var maxEndDate = query.MaxEndDate;

            if (query.HasAired.HasValue)
            {
                if (query.HasAired.Value)
                {
                    maxEndDate = DateTime.UtcNow;
                }
                else
                {
                    minEndDate = DateTime.UtcNow;
                }
            }

            if (minEndDate.HasValue)
            {
                whereClauses.Add("EndDate>=@MinEndDate");
                if (statement != null)
                {
                    statement.TryBind("@MinEndDate", minEndDate.Value);
                }
            }

            if (maxEndDate.HasValue)
            {
                whereClauses.Add("EndDate<=@MaxEndDate");
                if (statement != null)
                {
                    statement.TryBind("@MaxEndDate", maxEndDate.Value);
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

                    clauses.Add("(guid in (select itemid from People where Name = (select Name from TypedBaseItems where guid=" + paramName + ")))");

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

            // These are the same, for now
            var nameContains = query.NameContains;
            if (!string.IsNullOrWhiteSpace(nameContains))
            {
                whereClauses.Add("(CleanName like @NameContains or OriginalTitle like @NameContains)");
                if (statement != null)
                {
                    nameContains = FixUnicodeChars(nameContains);

                    statement.TryBind("@NameContains", "%" + GetCleanValue(nameContains) + "%");
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
                    // We should probably figure this out for all folders, but for right now, this is the only place where we need it
                    if (query.IncludeItemTypes.Length == 1 && string.Equals(query.IncludeItemTypes[0], typeof(Series).Name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (query.IsPlayed.Value)
                        {
                            whereClauses.Add("PresentationUniqueKey not in (select S.SeriesPresentationUniqueKey from TypedBaseitems S left join UserDatas UD on S.UserDataKey=UD.Key And UD.UserId=@UserId where Coalesce(UD.Played, 0)=0 and S.IsFolder=0 and S.IsVirtualItem=0 and S.SeriesPresentationUniqueKey not null)");
                        }
                        else
                        {
                            whereClauses.Add("PresentationUniqueKey in (select S.SeriesPresentationUniqueKey from TypedBaseitems S left join UserDatas UD on S.UserDataKey=UD.Key And UD.UserId=@UserId where Coalesce(UD.Played, 0)=0 and S.IsFolder=0 and S.IsVirtualItem=0 and S.SeriesPresentationUniqueKey not null)");
                        }
                    }
                    else
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

                    clauses.Add("(guid in (select itemid from itemvalues where CleanValue = (select CleanName from TypedBaseItems where guid=" + paramName + ") and Type<=1))");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, artistId.ToGuidBlob());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.AlbumArtistIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var artistId in query.AlbumArtistIds)
                {
                    var paramName = "@ArtistIds" + index;

                    clauses.Add("(guid in (select itemid from itemvalues where CleanValue = (select CleanName from TypedBaseItems where guid=" + paramName + ") and Type=1))");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, artistId.ToGuidBlob());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.ContributingArtistIds.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var artistId in query.ContributingArtistIds)
                {
                    var paramName = "@ArtistIds" + index;

                    clauses.Add("((select CleanName from TypedBaseItems where guid=" + paramName + ") in (select CleanValue from itemvalues where ItemId=Guid and Type=0) AND (select CleanName from TypedBaseItems where guid=" + paramName + ") not in (select CleanValue from itemvalues where ItemId=Guid and Type=1))");
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

                    clauses.Add("(guid not in (select itemid from itemvalues where CleanValue = (select CleanName from TypedBaseItems where guid=" + paramName + ") and Type<=1))");
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

                    clauses.Add("(guid in (select itemid from itemvalues where CleanValue = (select CleanName from TypedBaseItems where guid=" + paramName + ") and Type=2))");
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

            if (tags.Count > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in tags)
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

            if (excludeTags.Count > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in excludeTags)
                {
                    clauses.Add("@ExcludeTag" + index + " not in (select CleanValue from itemvalues where ItemId=Guid and Type=4)");
                    if (statement != null)
                    {
                        statement.TryBind("@ExcludeTag" + index, GetCleanValue(item));
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

                    clauses.Add("(guid in (select itemid from itemvalues where CleanValue = (select CleanName from TypedBaseItems where guid=" + paramName + ") and Type=3))");

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
                whereClauses.Add("InheritedParentalRatingValue>=@MinParentalRating");
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

            if (query.HasOfficialRating.HasValue)
            {
                if (query.HasOfficialRating.Value)
                {
                    whereClauses.Add("(OfficialRating not null AND OfficialRating<>'')");
                }
                else
                {
                    whereClauses.Add("(OfficialRating is null OR OfficialRating='')");
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

            if (query.HasOwnerId.HasValue)
            {
                if (query.HasOwnerId.Value)
                {
                    whereClauses.Add("OwnerId not null");
                }
                else
                {
                    whereClauses.Add("OwnerId is null");
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

            if (query.HasSubtitles.HasValue)
            {
                if (query.HasSubtitles.Value)
                {
                    whereClauses.Add("((select type from MediaStreams where MediaStreams.ItemId=A.Guid and MediaStreams.StreamType='Subtitle' limit 1) not null)");
                }
                else
                {
                    whereClauses.Add("((select type from MediaStreams where MediaStreams.ItemId=A.Guid and MediaStreams.StreamType='Subtitle' limit 1) is null)");
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

            if (query.IsDeadArtist.HasValue && query.IsDeadArtist.Value)
            {
                whereClauses.Add("CleanName not in (Select CleanValue From ItemValues where Type in (0,1))");
            }

            if (query.IsDeadStudio.HasValue && query.IsDeadStudio.Value)
            {
                whereClauses.Add("CleanName not in (Select CleanValue From ItemValues where Type = 3)");
            }

            if (query.IsDeadPerson.HasValue && query.IsDeadPerson.Value)
            {
                whereClauses.Add("Name not in (Select Name From People)");
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
                        statement.TryBind("@IncludeId" + index, id);
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
                        statement.TryBind("@ExcludeId" + index, id);
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

                if (excludeIds.Count > 0)
                {
                    whereClauses.Add(string.Join(" AND ", excludeIds.ToArray()));
                }
            }

            if (query.HasAnyProviderId.Count > 0)
            {
                var hasProviderIds = new List<string>();

                var index = 0;
                foreach (var pair in query.HasAnyProviderId)
                {
                    if (string.Equals(pair.Key, MetadataProviders.TmdbCollection.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var paramName = "@HasAnyProviderId" + index;
                    //hasProviderIds.Add("(COALESCE((select value from ProviderIds where ItemId=Guid and Name = '" + pair.Key + "'), '') <> " + paramName + ")");
                    hasProviderIds.Add("ProviderIds like " + paramName + "");
                    if (statement != null)
                    {
                        statement.TryBind(paramName, "%" + pair.Key + "=" + pair.Value + "%");
                    }
                    index++;

                    break;
                }

                if (hasProviderIds.Count > 0)
                {
                    whereClauses.Add("(" + string.Join(" OR ", hasProviderIds.ToArray()) + ")");
                }
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

            var includedItemByNameTypes = GetItemByNameTypesInQuery(query).SelectMany(MapIncludeItemTypes).ToList();
            var enableItemsByName = (query.IncludeItemsByName ?? false) && includedItemByNameTypes.Count > 0;

            var queryTopParentIds = query.TopParentIds;

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
                    statement.TryBind("@TopParentId", queryTopParentIds[0].ToString("N"));
                }
            }
            else if (queryTopParentIds.Length > 1)
            {
                var val = string.Join(",", queryTopParentIds.Select(i => "'" + i.ToString("N") + "'").ToArray());

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
                    statement.TryBind("@AncestorId", query.AncestorIds[0]);
                }
            }
            if (query.AncestorIds.Length > 1)
            {
                var inClause = string.Join(",", query.AncestorIds.Select(i => "'" + i.ToString("N") + "'").ToArray());
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

            if (query.SeriesStatuses.Length > 0)
            {
                var statuses = new List<string>();

                foreach (var seriesStatus in query.SeriesStatuses)
                {
                    statuses.Add("data like  '%" + seriesStatus + "%'");
                }

                whereClauses.Add("(" + string.Join(" OR ", statuses.ToArray()) + ")");
            }

            if (query.BoxSetLibraryFolders.Length > 0)
            {
                var folderIdQueries = new List<string>();

                foreach (var folderId in query.BoxSetLibraryFolders)
                {
                    folderIdQueries.Add("data like '%" + folderId.ToString("N") + "%'");
                }

                whereClauses.Add("(" + string.Join(" OR ", folderIdQueries.ToArray()) + ")");
            }

            if (query.VideoTypes.Length > 0)
            {
                var videoTypes = new List<string>();

                foreach (var videoType in query.VideoTypes)
                {
                    videoTypes.Add("data like '%\"VideoType\":\"" + videoType.ToString() + "\"%'");
                }

                whereClauses.Add("(" + string.Join(" OR ", videoTypes.ToArray()) + ")");
            }

            if (query.Is3D.HasValue)
            {
                if (query.Is3D.Value)
                {
                    whereClauses.Add("data like '%Video3DFormat%'");
                }
                else
                {
                    whereClauses.Add("data not like '%Video3DFormat%'");
                }
            }

            if (query.IsPlaceHolder.HasValue)
            {
                if (query.IsPlaceHolder.Value)
                {
                    whereClauses.Add("data like '%\"IsPlaceHolder\":true%'");
                }
                else
                {
                    whereClauses.Add("(data is null or data not like '%\"IsPlaceHolder\":true%')");
                }
            }

            if (query.HasSpecialFeature.HasValue)
            {
                if (query.HasSpecialFeature.Value)
                {
                    whereClauses.Add("ExtraIds not null");
                }
                else
                {
                    whereClauses.Add("ExtraIds is null");
                }
            }

            if (query.HasTrailer.HasValue)
            {
                if (query.HasTrailer.Value)
                {
                    whereClauses.Add("ExtraIds not null");
                }
                else
                {
                    whereClauses.Add("ExtraIds is null");
                }
            }

            if (query.HasThemeSong.HasValue)
            {
                if (query.HasThemeSong.Value)
                {
                    whereClauses.Add("ExtraIds not null");
                }
                else
                {
                    whereClauses.Add("ExtraIds is null");
                }
            }

            if (query.HasThemeVideo.HasValue)
            {
                if (query.HasThemeVideo.Value)
                {
                    whereClauses.Add("ExtraIds not null");
                }
                else
                {
                    whereClauses.Add("ExtraIds is null");
                }
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
            typeof(Series),
            typeof(Audio),
            typeof(MusicAlbum),
            typeof(MusicArtist),
            typeof(MusicGenre),
            typeof(MusicVideo),
            typeof(Movie),
            typeof(Playlist),
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
            if (id.Equals(Guid.Empty))
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
                        var idBlob = id.ToGuidBlob();

                        // Delete people
                        ExecuteWithSingleParam(db, "delete from People where ItemId=@Id", idBlob);

                        // Delete chapters
                        ExecuteWithSingleParam(db, "delete from " + ChaptersTableName + " where ItemId=@Id", idBlob);

                        // Delete media streams
                        ExecuteWithSingleParam(db, "delete from mediastreams where ItemId=@Id", idBlob);

                        // Delete ancestors
                        ExecuteWithSingleParam(db, "delete from AncestorIds where ItemId=@Id", idBlob);

                        // Delete item values
                        ExecuteWithSingleParam(db, "delete from ItemValues where ItemId=@Id", idBlob);

                        // Delete the item
                        ExecuteWithSingleParam(db, "delete from TypedBaseItems where guid=@Id", idBlob);
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

            if (!query.ItemId.Equals(Guid.Empty))
            {
                whereClauses.Add("ItemId=@ItemId");
                if (statement != null)
                {
                    statement.TryBind("@ItemId", query.ItemId.ToGuidBlob());
                }
            }
            if (!query.AppearsInItemId.Equals(Guid.Empty))
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

        private void UpdateAncestors(Guid itemId, List<Guid> ancestorIds, IDatabaseConnection db, IStatement deleteAncestorsStatement)
        {
            if (itemId.Equals(Guid.Empty))
            {
                throw new ArgumentNullException("itemId");
            }

            if (ancestorIds == null)
            {
                throw new ArgumentNullException("ancestorIds");
            }

            CheckDisposed();

            var itemIdBlob = itemId.ToGuidBlob();

            // First delete 
            deleteAncestorsStatement.Reset();
            deleteAncestorsStatement.TryBind("@ItemId", itemIdBlob);
            deleteAncestorsStatement.MoveNext();

            if (ancestorIds.Count == 0)
            {
                return;
            }

            var insertText = new StringBuilder("insert into AncestorIds (ItemId, AncestorId, AncestorIdText) values ");

            for (var i = 0; i < ancestorIds.Count; i++)
            {
                if (i > 0)
                {
                    insertText.Append(",");
                }

                insertText.AppendFormat("(@ItemId, @AncestorId{0}, @AncestorIdText{0})", i.ToString(CultureInfo.InvariantCulture));
            }

            using (var statement = PrepareStatementSafe(db, insertText.ToString()))
            {
                statement.TryBind("@ItemId", itemIdBlob);

                for (var i = 0; i < ancestorIds.Count; i++)
                {
                    var index = i.ToString(CultureInfo.InvariantCulture);

                    var ancestorId = ancestorIds[i];

                    statement.TryBind("@AncestorId" + index, ancestorId.ToGuidBlob());
                    statement.TryBind("@AncestorIdText" + index, ancestorId.ToString("N"));
                }

                statement.Reset();
                statement.MoveNext();
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
            //logger.LogInformation("GetItemValues: " + _environmentInfo.StackTrace);

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

            // do this first before calling GetFinalColumnsToSelect, otherwise ExcludeItemIds will be set by SimilarTo
            var innerQuery = new InternalItemsQuery(query.User)
            {
                ExcludeItemTypes = query.ExcludeItemTypes,
                IncludeItemTypes = query.IncludeItemTypes,
                MediaTypes = query.MediaTypes,
                AncestorIds = query.AncestorIds,
                ItemIds = query.ItemIds,
                TopParentIds = query.TopParentIds,
                ParentId = query.ParentId,
                IsPlayed = query.IsPlayed,
                IsAiring = query.IsAiring,
                IsMovie = query.IsMovie,
                IsSports = query.IsSports,
                IsKids = query.IsKids,
                IsNews = query.IsNews,
                IsSeries = query.IsSeries
            };

            columns = GetFinalColumnsToSelect(query, columns.ToArray()).ToList();

            var commandText = "select " + string.Join(",", columns.ToArray()) + GetFromText();
            commandText += GetJoinUserDataText(query);

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
                Years = query.Years,
                NameContains = query.NameContains,
                SearchTerm = query.SearchTerm,
                SimilarTo = query.SimilarTo,
                ExcludeItemIds = query.ExcludeItemIds
            };

            var outerWhereClauses = GetWhereClauses(outerQuery, null);

            whereText += outerWhereClauses.Count == 0 ?
                string.Empty :
                " AND " + string.Join(" AND ", outerWhereClauses.ToArray());
            //cmd.CommandText += GetGroupBy(query);

            commandText += whereText;
            commandText += " group by PresentationUniqueKey";

            if (query.SimilarTo != null || !string.IsNullOrEmpty(query.SearchTerm))
            {
                commandText += GetOrderByText(query);
            }
            else
            {
                commandText += " order by SortName";
            }

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
                var countText = "select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "count (distinct PresentationUniqueKey)" })) + GetFromText();

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

                        //logger.LogInformation("GetItemValues {0}", string.Join(";", statementTexts.ToArray()));
                        var statements = PrepareAllSafe(db, statementTexts);

                        if (!isReturningZeroItems)
                        {
                            using (var statement = statements[0])
                            {
                                statement.TryBind("@SelectType", returnType);
                                if (EnableJoinUserData(query))
                                {
                                    statement.TryBind("@UserId", query.User.InternalId);
                                }

                                if (typeSubQuery != null)
                                {
                                    GetWhereClauses(typeSubQuery, null);
                                }
                                BindSimilarParams(query, statement);
                                BindSearchParams(query, statement);
                                GetWhereClauses(innerQuery, statement);
                                GetWhereClauses(outerQuery, statement);

                                var hasEpisodeAttributes = HasEpisodeAttributes(query);
                                var hasProgramAttributes = HasProgramAttributes(query);
                                var hasServiceName = HasServiceName(query);
                                var hasStartDate = HasStartDate(query);
                                var hasTrailerTypes = HasTrailerTypes(query);
                                var hasArtistFields = HasArtistFields(query);
                                var hasSeriesFields = HasSeriesFields(query);

                                foreach (var row in statement.ExecuteQuery())
                                {
                                    var item = GetItem(row, query, hasProgramAttributes, hasEpisodeAttributes, hasServiceName, hasStartDate, hasTrailerTypes, hasArtistFields, hasSeriesFields);
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
                            commandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "count (distinct PresentationUniqueKey)" })) + GetFromText();

                            commandText += GetJoinUserDataText(query);
                            commandText += whereText;

                            using (var statement = statements[statements.Count - 1])
                            {
                                statement.TryBind("@SelectType", returnType);
                                if (EnableJoinUserData(query))
                                {
                                    statement.TryBind("@UserId", query.User.InternalId);
                                }

                                if (typeSubQuery != null)
                                {
                                    GetWhereClauses(typeSubQuery, null);
                                }
                                BindSimilarParams(query, statement);
                                BindSearchParams(query, statement);
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
            if (itemId.Equals(Guid.Empty))
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

            InsertItemValues(guidBlob, values, db);
        }

        private void InsertItemValues(byte[] idBlob, List<Tuple<int, string>> values, IDatabaseConnection db)
        {
            var startIndex = 0;
            var limit = 100;

            while (startIndex < values.Count)
            {
                var insertText = new StringBuilder("insert into ItemValues (ItemId, Type, Value, CleanValue) values ");

                var endIndex = Math.Min(values.Count, startIndex + limit);
                var isSubsequentRow = false;

                for (var i = startIndex; i < endIndex; i++)
                {
                    if (isSubsequentRow)
                    {
                        insertText.Append(",");
                    }

                    insertText.AppendFormat("(@ItemId, @Type{0}, @Value{0}, @CleanValue{0})", i.ToString(CultureInfo.InvariantCulture));
                    isSubsequentRow = true;
                }

                using (var statement = PrepareStatementSafe(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", idBlob);

                    for (var i = startIndex; i < endIndex; i++)
                    {
                        var index = i.ToString(CultureInfo.InvariantCulture);

                        var currentValueInfo = values[i];

                        var itemValue = currentValueInfo.Item2;

                        // Don't save if invalid
                        if (string.IsNullOrWhiteSpace(itemValue))
                        {
                            continue;
                        }

                        statement.TryBind("@Type" + index, currentValueInfo.Item1);
                        statement.TryBind("@Value" + index, itemValue);
                        statement.TryBind("@CleanValue" + index, GetCleanValue(itemValue));
                    }

                    statement.Reset();
                    statement.MoveNext();
                }

                startIndex += limit;
            }
        }

        public void UpdatePeople(Guid itemId, List<PersonInfo> people)
        {
            if (itemId.Equals(Guid.Empty))
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
                    connection.RunInTransaction(db =>
                    {
                        var itemIdBlob = itemId.ToGuidBlob();

                        // First delete chapters
                        db.Execute("delete from People where ItemId=@ItemId", itemIdBlob);

                        InsertPeople(itemIdBlob, people, db);

                    }, TransactionMode);

                }
            }
        }

        private void InsertPeople(byte[] idBlob, List<PersonInfo> people, IDatabaseConnection db)
        {
            var startIndex = 0;
            var limit = 100;
            var listIndex = 0;

            while (startIndex < people.Count)
            {
                var insertText = new StringBuilder("insert into People (ItemId, Name, Role, PersonType, SortOrder, ListOrder) values ");

                var endIndex = Math.Min(people.Count, startIndex + limit);
                var isSubsequentRow = false;

                for (var i = startIndex; i < endIndex; i++)
                {
                    if (isSubsequentRow)
                    {
                        insertText.Append(",");
                    }

                    insertText.AppendFormat("(@ItemId, @Name{0}, @Role{0}, @PersonType{0}, @SortOrder{0}, @ListOrder{0})", i.ToString(CultureInfo.InvariantCulture));
                    isSubsequentRow = true;
                }

                using (var statement = PrepareStatementSafe(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", idBlob);

                    for (var i = startIndex; i < endIndex; i++)
                    {
                        var index = i.ToString(CultureInfo.InvariantCulture);

                        var person = people[i];

                        statement.TryBind("@Name" + index, person.Name);
                        statement.TryBind("@Role" + index, person.Role);
                        statement.TryBind("@PersonType" + index, person.Type);
                        statement.TryBind("@SortOrder" + index, person.SortOrder);
                        statement.TryBind("@ListOrder" + index, listIndex);

                        listIndex++;
                    }

                    statement.Reset();
                    statement.MoveNext();
                }

                startIndex += limit;
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

            if (id.Equals(Guid.Empty))
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
                    connection.RunInTransaction(db =>
                    {
                        var itemIdBlob = id.ToGuidBlob();

                        // First delete chapters
                        db.Execute("delete from mediastreams where ItemId=@ItemId", itemIdBlob);

                        InsertMediaStreams(itemIdBlob, streams, db);

                    }, TransactionMode);
                }
            }
        }

        private void InsertMediaStreams(byte[] idBlob, List<MediaStream> streams, IDatabaseConnection db)
        {
            var startIndex = 0;
            var limit = 10;

            while (startIndex < streams.Count)
            {
                var insertText = new StringBuilder(string.Format("insert into mediastreams ({0}) values ", string.Join(",", _mediaStreamSaveColumns)));

                var endIndex = Math.Min(streams.Count, startIndex + limit);
                var isSubsequentRow = false;

                for (var i = startIndex; i < endIndex; i++)
                {
                    if (isSubsequentRow)
                    {
                        insertText.Append(",");
                    }

                    var index = i.ToString(CultureInfo.InvariantCulture);

                    var mediaStreamSaveColumns = string.Join(",", _mediaStreamSaveColumns.Skip(1).Select(m => "@" + m + index).ToArray());

                    insertText.AppendFormat("(@ItemId, {0})", mediaStreamSaveColumns);
                    isSubsequentRow = true;
                }

                using (var statement = PrepareStatementSafe(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", idBlob);

                    for (var i = startIndex; i < endIndex; i++)
                    {
                        var index = i.ToString(CultureInfo.InvariantCulture);

                        var stream = streams[i];

                        statement.TryBind("@StreamIndex" + index, stream.Index);
                        statement.TryBind("@StreamType" + index, stream.Type.ToString());
                        statement.TryBind("@Codec" + index, stream.Codec);
                        statement.TryBind("@Language" + index, stream.Language);
                        statement.TryBind("@ChannelLayout" + index, stream.ChannelLayout);
                        statement.TryBind("@Profile" + index, stream.Profile);
                        statement.TryBind("@AspectRatio" + index, stream.AspectRatio);
                        statement.TryBind("@Path" + index, GetPathToSave(stream.Path));

                        statement.TryBind("@IsInterlaced" + index, stream.IsInterlaced);
                        statement.TryBind("@BitRate" + index, stream.BitRate);
                        statement.TryBind("@Channels" + index, stream.Channels);
                        statement.TryBind("@SampleRate" + index, stream.SampleRate);

                        statement.TryBind("@IsDefault" + index, stream.IsDefault);
                        statement.TryBind("@IsForced" + index, stream.IsForced);
                        statement.TryBind("@IsExternal" + index, stream.IsExternal);

                        // Yes these are backwards due to a mistake
                        statement.TryBind("@Width" + index, stream.Height);
                        statement.TryBind("@Height" + index, stream.Width);

                        statement.TryBind("@AverageFrameRate" + index, stream.AverageFrameRate);
                        statement.TryBind("@RealFrameRate" + index, stream.RealFrameRate);
                        statement.TryBind("@Level" + index, stream.Level);

                        statement.TryBind("@PixelFormat" + index, stream.PixelFormat);
                        statement.TryBind("@BitDepth" + index, stream.BitDepth);
                        statement.TryBind("@IsExternal" + index, stream.IsExternal);
                        statement.TryBind("@RefFrames" + index, stream.RefFrames);

                        statement.TryBind("@CodecTag" + index, stream.CodecTag);
                        statement.TryBind("@Comment" + index, stream.Comment);
                        statement.TryBind("@NalLengthSize" + index, stream.NalLengthSize);
                        statement.TryBind("@IsAvc" + index, stream.IsAVC);
                        statement.TryBind("@Title" + index, stream.Title);

                        statement.TryBind("@TimeBase" + index, stream.TimeBase);
                        statement.TryBind("@CodecTimeBase" + index, stream.CodecTimeBase);

                        statement.TryBind("@ColorPrimaries" + index, stream.ColorPrimaries);
                        statement.TryBind("@ColorSpace" + index, stream.ColorSpace);
                        statement.TryBind("@ColorTransfer" + index, stream.ColorTransfer);
                    }

                    statement.Reset();
                    statement.MoveNext();
                }

                startIndex += limit;
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
                item.Path = RestorePath(reader[8].ToString());
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

            if (reader[32].SQLiteType != SQLiteType.Null)
            {
                item.ColorPrimaries = reader[32].ToString();
            }

            if (reader[33].SQLiteType != SQLiteType.Null)
            {
                item.ColorSpace = reader[33].ToString();
            }

            if (reader[34].SQLiteType != SQLiteType.Null)
            {
                item.ColorTransfer = reader[34].ToString();
            }

            return item;
        }

    }
}
