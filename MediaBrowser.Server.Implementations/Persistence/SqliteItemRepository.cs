using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Server.Implementations.Persistence
{
    /// <summary>
    /// Class SQLiteItemRepository
    /// </summary>
    public class SqliteItemRepository : BaseSqliteRepository, IItemRepository
    {
        private IDbConnection _connection;

        private readonly TypeMapper _typeMapper = new TypeMapper();

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

        /// <summary>
        /// The _save item command
        /// </summary>
        private IDbCommand _saveItemCommand;

        private readonly string _criticReviewsPath;

        private IDbCommand _deleteItemCommand;

        private IDbCommand _deletePeopleCommand;
        private IDbCommand _savePersonCommand;

        private IDbCommand _deleteChaptersCommand;
        private IDbCommand _saveChapterCommand;

        private IDbCommand _deleteStreamsCommand;
        private IDbCommand _saveStreamCommand;

        private IDbCommand _deleteAncestorsCommand;
        private IDbCommand _saveAncestorCommand;

        private IDbCommand _deleteUserDataKeysCommand;
        private IDbCommand _saveUserDataKeysCommand;

        private IDbCommand _deleteItemValuesCommand;
        private IDbCommand _saveItemValuesCommand;

        private IDbCommand _deleteProviderIdsCommand;
        private IDbCommand _saveProviderIdsCommand;

        private IDbCommand _deleteImagesCommand;
        private IDbCommand _saveImagesCommand;

        private IDbCommand _updateInheritedTagsCommand;

        public const int LatestSchemaVersion = 109;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteItemRepository"/> class.
        /// </summary>
        public SqliteItemRepository(IServerConfigurationManager config, IJsonSerializer jsonSerializer, ILogManager logManager, IDbConnector connector)
            : base(logManager, connector)
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

            _criticReviewsPath = Path.Combine(_config.ApplicationPaths.DataPath, "critic-reviews");
            DbFilePath = Path.Combine(_config.ApplicationPaths.DataPath, "library.db");
        }

        private const string ChaptersTableName = "Chapters2";

        protected override async Task<IDbConnection> CreateConnection(bool isReadOnly = false)
        {
            var cacheSize = _config.Configuration.SqliteCacheSize;
            if (cacheSize <= 0)
            {
                cacheSize = Math.Min(Environment.ProcessorCount * 50000, 200000);
            }

            var connection = await DbConnector.Connect(DbFilePath, false, false, 0 - cacheSize).ConfigureAwait(false);

            connection.RunQueries(new[]
            {
                "pragma temp_store = memory",
                "pragma default_temp_store = memory",
                "PRAGMA locking_mode=EXCLUSIVE"

            }, Logger);

            return connection;
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize(SqliteUserDataRepository userDataRepo)
        {
            _connection = await CreateConnection(false).ConfigureAwait(false);

            var createMediaStreamsTableCommand
               = "create table if not exists mediastreams (ItemId GUID, StreamIndex INT, StreamType TEXT, Codec TEXT, Language TEXT, ChannelLayout TEXT, Profile TEXT, AspectRatio TEXT, Path TEXT, IsInterlaced BIT, BitRate INT NULL, Channels INT NULL, SampleRate INT NULL, IsDefault BIT, IsForced BIT, IsExternal BIT, Height INT NULL, Width INT NULL, AverageFrameRate FLOAT NULL, RealFrameRate FLOAT NULL, Level FLOAT NULL, PixelFormat TEXT, BitDepth INT NULL, IsAnamorphic BIT NULL, RefFrames INT NULL, CodecTag TEXT NULL, Comment TEXT NULL, NalLengthSize TEXT NULL, IsAvc BIT NULL, Title TEXT NULL, TimeBase TEXT NULL, CodecTimeBase TEXT NULL, PRIMARY KEY (ItemId, StreamIndex))";

            string[] queries = {

                                "create table if not exists TypedBaseItems (guid GUID primary key, type TEXT, data BLOB, ParentId GUID, Path TEXT)",

                                "create table if not exists AncestorIds (ItemId GUID, AncestorId GUID, AncestorIdText TEXT, PRIMARY KEY (ItemId, AncestorId))",
                                "create index if not exists idx_AncestorIds1 on AncestorIds(AncestorId)",
                                "create index if not exists idx_AncestorIds2 on AncestorIds(AncestorIdText)",

                                "create table if not exists UserDataKeys (ItemId GUID, UserDataKey TEXT Priority INT, PRIMARY KEY (ItemId, UserDataKey))",

                                "create table if not exists ItemValues (ItemId GUID, Type INT, Value TEXT, CleanValue TEXT)",

                                "create table if not exists ProviderIds (ItemId GUID, Name TEXT, Value TEXT, PRIMARY KEY (ItemId, Name))",
                                // covering index
                                "create index if not exists Idx_ProviderIds1 on ProviderIds(ItemId,Name,Value)",

                                "create table if not exists Images (ItemId GUID NOT NULL, Path TEXT NOT NULL, ImageType INT NOT NULL, DateModified DATETIME, IsPlaceHolder BIT NOT NULL, SortOrder INT)",
                                "create index if not exists idx_Images on Images(ItemId)",

                                "create table if not exists People (ItemId GUID, Name TEXT NOT NULL, Role TEXT, PersonType TEXT, SortOrder int, ListOrder int)",

                                "drop index if exists idxPeopleItemId",
                                "create index if not exists idxPeopleItemId1 on People(ItemId,ListOrder)",
                                "create index if not exists idxPeopleName on People(Name)",

                                "create table if not exists "+ChaptersTableName+" (ItemId GUID, ChapterIndex INT, StartPositionTicks BIGINT, Name TEXT, ImagePath TEXT, PRIMARY KEY (ItemId, ChapterIndex))",

                                createMediaStreamsTableCommand,

                                "create index if not exists idx_mediastreams1 on mediastreams(ItemId)",

                               };

            _connection.RunQueries(queries, Logger);

            _connection.AddColumn(Logger, "AncestorIds", "AncestorIdText", "Text");

            _connection.AddColumn(Logger, "TypedBaseItems", "Path", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "StartDate", "DATETIME");
            _connection.AddColumn(Logger, "TypedBaseItems", "EndDate", "DATETIME");
            _connection.AddColumn(Logger, "TypedBaseItems", "ChannelId", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsMovie", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsSports", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsKids", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "CommunityRating", "Float");
            _connection.AddColumn(Logger, "TypedBaseItems", "CustomRating", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "IndexNumber", "INT");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsLocked", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "Name", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "OfficialRating", "Text");

            _connection.AddColumn(Logger, "TypedBaseItems", "MediaType", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "Overview", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "ParentIndexNumber", "INT");
            _connection.AddColumn(Logger, "TypedBaseItems", "PremiereDate", "DATETIME");
            _connection.AddColumn(Logger, "TypedBaseItems", "ProductionYear", "INT");
            _connection.AddColumn(Logger, "TypedBaseItems", "ParentId", "GUID");
            _connection.AddColumn(Logger, "TypedBaseItems", "Genres", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "SchemaVersion", "INT");
            _connection.AddColumn(Logger, "TypedBaseItems", "SortName", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "RunTimeTicks", "BIGINT");

            _connection.AddColumn(Logger, "TypedBaseItems", "OfficialRatingDescription", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "HomePageUrl", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "VoteCount", "INT");
            _connection.AddColumn(Logger, "TypedBaseItems", "DisplayMediaType", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "DateCreated", "DATETIME");
            _connection.AddColumn(Logger, "TypedBaseItems", "DateModified", "DATETIME");

            _connection.AddColumn(Logger, "TypedBaseItems", "ForcedSortName", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsOffline", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "LocationType", "Text");

            _connection.AddColumn(Logger, "TypedBaseItems", "IsSeries", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsLive", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsNews", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsPremiere", "BIT");

            _connection.AddColumn(Logger, "TypedBaseItems", "EpisodeTitle", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsRepeat", "BIT");

            _connection.AddColumn(Logger, "TypedBaseItems", "PreferredMetadataLanguage", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "PreferredMetadataCountryCode", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsHD", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "ExternalEtag", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "DateLastRefreshed", "DATETIME");

            _connection.AddColumn(Logger, "TypedBaseItems", "DateLastSaved", "DATETIME");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsInMixedFolder", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "LockedFields", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "Studios", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "Audio", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "ExternalServiceId", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "Tags", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsFolder", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "InheritedParentalRatingValue", "INT");
            _connection.AddColumn(Logger, "TypedBaseItems", "UnratedType", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "TopParentId", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsItemByName", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "SourceType", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "TrailerTypes", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "CriticRating", "Float");
            _connection.AddColumn(Logger, "TypedBaseItems", "CriticRatingSummary", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "InheritedTags", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "CleanName", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "PresentationUniqueKey", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "SlugName", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "OriginalTitle", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "PrimaryVersionId", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "DateLastMediaAdded", "DATETIME");
            _connection.AddColumn(Logger, "TypedBaseItems", "Album", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "IsVirtualItem", "BIT");
            _connection.AddColumn(Logger, "TypedBaseItems", "SeriesName", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "UserDataKey", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "SeasonName", "Text");
            _connection.AddColumn(Logger, "TypedBaseItems", "SeasonId", "GUID");
            _connection.AddColumn(Logger, "TypedBaseItems", "SeriesId", "GUID");
            _connection.AddColumn(Logger, "TypedBaseItems", "SeriesSortName", "Text");

            _connection.AddColumn(Logger, "UserDataKeys", "Priority", "INT");
            _connection.AddColumn(Logger, "ItemValues", "CleanValue", "Text");

            _connection.AddColumn(Logger, ChaptersTableName, "ImageDateModified", "DATETIME");

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

                "create index if not exists idx_PathTypedBaseItems on TypedBaseItems(Path)",
                "create index if not exists idx_ParentIdTypedBaseItems on TypedBaseItems(ParentId)",

                "create index if not exists idx_PresentationUniqueKey on TypedBaseItems(PresentationUniqueKey)",
                "create index if not exists idx_GuidTypeIsFolderIsVirtualItem on TypedBaseItems(Guid,Type,IsFolder,IsVirtualItem)",
                //"create index if not exists idx_GuidMediaTypeIsFolderIsVirtualItem on TypedBaseItems(Guid,MediaType,IsFolder,IsVirtualItem)",
                "create index if not exists idx_CleanNameType on TypedBaseItems(CleanName,Type)",

                // covering index
                "create index if not exists idx_TopParentIdGuid on TypedBaseItems(TopParentId,Guid)",

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

                // covering index
                "create index if not exists idx_UserDataKeys3 on UserDataKeys(ItemId,Priority,UserDataKey)"
                };

            _connection.RunQueries(postQueries, Logger);

            PrepareStatements();

            new MediaStreamColumns(_connection, Logger).AddColumns();

            DataExtensions.Attach(_connection, Path.Combine(_config.ApplicationPaths.DataPath, "userdata_v2.db"), "UserDataDb");
            await userDataRepo.Initialize(_connection, WriteLock).ConfigureAwait(false);
            //await Vacuum(_connection).ConfigureAwait(false);
        }

        private readonly string[] _retriveItemColumns =
        {
            "type",
            "data",
            "StartDate",
            "EndDate",
            "IsOffline",
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
            "InheritedParentalRatingValue"
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

        /// <summary>
        /// Prepares the statements.
        /// </summary>
        private void PrepareStatements()
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
                "SchemaVersion",
                "SortName",
                "RunTimeTicks",
                "OfficialRatingDescription",
                "HomePageUrl",
                "VoteCount",
                "DisplayMediaType",
                "DateCreated",
                "DateModified",
                "ForcedSortName",
                "IsOffline",
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
                "SeriesSortName"
            };
            _saveItemCommand = _connection.CreateCommand();
            _saveItemCommand.CommandText = "replace into TypedBaseItems (" + string.Join(",", saveColumns.ToArray()) + ") values (";

            for (var i = 1; i <= saveColumns.Count; i++)
            {
                if (i > 1)
                {
                    _saveItemCommand.CommandText += ",";
                }
                _saveItemCommand.CommandText += "@" + i.ToString(CultureInfo.InvariantCulture);

                _saveItemCommand.Parameters.Add(_saveItemCommand, "@" + i.ToString(CultureInfo.InvariantCulture));
            }
            _saveItemCommand.CommandText += ")";

            _deleteItemCommand = _connection.CreateCommand();
            _deleteItemCommand.CommandText = "delete from TypedBaseItems where guid=@Id";
            _deleteItemCommand.Parameters.Add(_deleteItemCommand, "@Id");

            // People
            _deletePeopleCommand = _connection.CreateCommand();
            _deletePeopleCommand.CommandText = "delete from People where ItemId=@Id";
            _deletePeopleCommand.Parameters.Add(_deletePeopleCommand, "@Id");

            _savePersonCommand = _connection.CreateCommand();
            _savePersonCommand.CommandText = "insert into People (ItemId, Name, Role, PersonType, SortOrder, ListOrder) values (@ItemId, @Name, @Role, @PersonType, @SortOrder, @ListOrder)";
            _savePersonCommand.Parameters.Add(_savePersonCommand, "@ItemId");
            _savePersonCommand.Parameters.Add(_savePersonCommand, "@Name");
            _savePersonCommand.Parameters.Add(_savePersonCommand, "@Role");
            _savePersonCommand.Parameters.Add(_savePersonCommand, "@PersonType");
            _savePersonCommand.Parameters.Add(_savePersonCommand, "@SortOrder");
            _savePersonCommand.Parameters.Add(_savePersonCommand, "@ListOrder");

            // Ancestors
            _deleteAncestorsCommand = _connection.CreateCommand();
            _deleteAncestorsCommand.CommandText = "delete from AncestorIds where ItemId=@Id";
            _deleteAncestorsCommand.Parameters.Add(_deleteAncestorsCommand, "@Id");

            _saveAncestorCommand = _connection.CreateCommand();
            _saveAncestorCommand.CommandText = "insert into AncestorIds (ItemId, AncestorId, AncestorIdText) values (@ItemId, @AncestorId, @AncestorIdText)";
            _saveAncestorCommand.Parameters.Add(_saveAncestorCommand, "@ItemId");
            _saveAncestorCommand.Parameters.Add(_saveAncestorCommand, "@AncestorId");
            _saveAncestorCommand.Parameters.Add(_saveAncestorCommand, "@AncestorIdText");

            // Chapters
            _deleteChaptersCommand = _connection.CreateCommand();
            _deleteChaptersCommand.CommandText = "delete from " + ChaptersTableName + " where ItemId=@ItemId";
            _deleteChaptersCommand.Parameters.Add(_deleteChaptersCommand, "@ItemId");

            _saveChapterCommand = _connection.CreateCommand();
            _saveChapterCommand.CommandText = "replace into " + ChaptersTableName + " (ItemId, ChapterIndex, StartPositionTicks, Name, ImagePath) values (@ItemId, @ChapterIndex, @StartPositionTicks, @Name, @ImagePath)";

            _saveChapterCommand.Parameters.Add(_saveChapterCommand, "@ItemId");
            _saveChapterCommand.Parameters.Add(_saveChapterCommand, "@ChapterIndex");
            _saveChapterCommand.Parameters.Add(_saveChapterCommand, "@StartPositionTicks");
            _saveChapterCommand.Parameters.Add(_saveChapterCommand, "@Name");
            _saveChapterCommand.Parameters.Add(_saveChapterCommand, "@ImagePath");
            _saveChapterCommand.Parameters.Add(_saveChapterCommand, "@ImageDateModified");

            // MediaStreams
            _deleteStreamsCommand = _connection.CreateCommand();
            _deleteStreamsCommand.CommandText = "delete from mediastreams where ItemId=@ItemId";
            _deleteStreamsCommand.Parameters.Add(_deleteStreamsCommand, "@ItemId");

            _saveStreamCommand = _connection.CreateCommand();

            _saveStreamCommand.CommandText = string.Format("replace into mediastreams ({0}) values ({1})",
                string.Join(",", _mediaStreamSaveColumns),
                string.Join(",", _mediaStreamSaveColumns.Select(i => "@" + i).ToArray()));

            foreach (var col in _mediaStreamSaveColumns)
            {
                _saveStreamCommand.Parameters.Add(_saveStreamCommand, "@" + col);
            }

            _updateInheritedTagsCommand = _connection.CreateCommand();
            _updateInheritedTagsCommand.CommandText = "Update TypedBaseItems set InheritedTags=@InheritedTags where Guid=@Guid";
            _updateInheritedTagsCommand.Parameters.Add(_updateInheritedTagsCommand, "@Guid");
            _updateInheritedTagsCommand.Parameters.Add(_updateInheritedTagsCommand, "@InheritedTags");

            // user data
            _deleteUserDataKeysCommand = _connection.CreateCommand();
            _deleteUserDataKeysCommand.CommandText = "delete from UserDataKeys where ItemId=@Id";
            _deleteUserDataKeysCommand.Parameters.Add(_deleteUserDataKeysCommand, "@Id");

            _saveUserDataKeysCommand = _connection.CreateCommand();
            _saveUserDataKeysCommand.CommandText = "insert into UserDataKeys (ItemId, UserDataKey, Priority) values (@ItemId, @UserDataKey, @Priority)";
            _saveUserDataKeysCommand.Parameters.Add(_saveUserDataKeysCommand, "@ItemId");
            _saveUserDataKeysCommand.Parameters.Add(_saveUserDataKeysCommand, "@UserDataKey");
            _saveUserDataKeysCommand.Parameters.Add(_saveUserDataKeysCommand, "@Priority");

            // item values
            _deleteItemValuesCommand = _connection.CreateCommand();
            _deleteItemValuesCommand.CommandText = "delete from ItemValues where ItemId=@Id";
            _deleteItemValuesCommand.Parameters.Add(_deleteItemValuesCommand, "@Id");

            _saveItemValuesCommand = _connection.CreateCommand();
            _saveItemValuesCommand.CommandText = "insert into ItemValues (ItemId, Type, Value, CleanValue) values (@ItemId, @Type, @Value, @CleanValue)";
            _saveItemValuesCommand.Parameters.Add(_saveItemValuesCommand, "@ItemId");
            _saveItemValuesCommand.Parameters.Add(_saveItemValuesCommand, "@Type");
            _saveItemValuesCommand.Parameters.Add(_saveItemValuesCommand, "@Value");
            _saveItemValuesCommand.Parameters.Add(_saveItemValuesCommand, "@CleanValue");

            // provider ids
            _deleteProviderIdsCommand = _connection.CreateCommand();
            _deleteProviderIdsCommand.CommandText = "delete from ProviderIds where ItemId=@Id";
            _deleteProviderIdsCommand.Parameters.Add(_deleteProviderIdsCommand, "@Id");

            _saveProviderIdsCommand = _connection.CreateCommand();
            _saveProviderIdsCommand.CommandText = "insert into ProviderIds (ItemId, Name, Value) values (@ItemId, @Name, @Value)";
            _saveProviderIdsCommand.Parameters.Add(_saveProviderIdsCommand, "@ItemId");
            _saveProviderIdsCommand.Parameters.Add(_saveProviderIdsCommand, "@Name");
            _saveProviderIdsCommand.Parameters.Add(_saveProviderIdsCommand, "@Value");

            // images
            _deleteImagesCommand = _connection.CreateCommand();
            _deleteImagesCommand.CommandText = "delete from Images where ItemId=@Id";
            _deleteImagesCommand.Parameters.Add(_deleteImagesCommand, "@Id");

            _saveImagesCommand = _connection.CreateCommand();
            _saveImagesCommand.CommandText = "insert into Images (ItemId, ImageType, Path, DateModified, IsPlaceHolder, SortOrder) values (@ItemId, @ImageType, @Path, @DateModified, @IsPlaceHolder, @SortOrder)";
            _saveImagesCommand.Parameters.Add(_saveImagesCommand, "@ItemId");
            _saveImagesCommand.Parameters.Add(_saveImagesCommand, "@ImageType");
            _saveImagesCommand.Parameters.Add(_saveImagesCommand, "@Path");
            _saveImagesCommand.Parameters.Add(_saveImagesCommand, "@DateModified");
            _saveImagesCommand.Parameters.Add(_saveImagesCommand, "@IsPlaceHolder");
            _saveImagesCommand.Parameters.Add(_saveImagesCommand, "@SortOrder");
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

            return SaveItems(new[] { item }, cancellationToken);
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
        public async Task SaveItems(IEnumerable<BaseItem> items, CancellationToken cancellationToken)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            cancellationToken.ThrowIfCancellationRequested();

            CheckDisposed();

            await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var index = 0;

                    _saveItemCommand.GetParameter(index++).Value = item.Id;
                    _saveItemCommand.GetParameter(index++).Value = item.GetType().FullName;
                    _saveItemCommand.GetParameter(index++).Value = _jsonSerializer.SerializeToBytes(item);

                    _saveItemCommand.GetParameter(index++).Value = item.Path;

                    var hasStartDate = item as IHasStartDate;
                    if (hasStartDate != null)
                    {
                        _saveItemCommand.GetParameter(index++).Value = hasStartDate.StartDate;
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    _saveItemCommand.GetParameter(index++).Value = item.EndDate;
                    _saveItemCommand.GetParameter(index++).Value = item.ChannelId;

                    var hasProgramAttributes = item as IHasProgramAttributes;
                    if (hasProgramAttributes != null)
                    {
                        _saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsKids;
                        _saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsMovie;
                        _saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsSports;
                        _saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsSeries;
                        _saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsLive;
                        _saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsNews;
                        _saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsPremiere;
                        _saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.EpisodeTitle;
                        _saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsRepeat;
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                        _saveItemCommand.GetParameter(index++).Value = null;
                        _saveItemCommand.GetParameter(index++).Value = null;
                        _saveItemCommand.GetParameter(index++).Value = null;
                        _saveItemCommand.GetParameter(index++).Value = null;
                        _saveItemCommand.GetParameter(index++).Value = null;
                        _saveItemCommand.GetParameter(index++).Value = null;
                        _saveItemCommand.GetParameter(index++).Value = null;
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    _saveItemCommand.GetParameter(index++).Value = item.CommunityRating;
                    _saveItemCommand.GetParameter(index++).Value = item.CustomRating;

                    _saveItemCommand.GetParameter(index++).Value = item.IndexNumber;
                    _saveItemCommand.GetParameter(index++).Value = item.IsLocked;

                    _saveItemCommand.GetParameter(index++).Value = item.Name;
                    _saveItemCommand.GetParameter(index++).Value = item.OfficialRating;

                    _saveItemCommand.GetParameter(index++).Value = item.MediaType;
                    _saveItemCommand.GetParameter(index++).Value = item.Overview;
                    _saveItemCommand.GetParameter(index++).Value = item.ParentIndexNumber;
                    _saveItemCommand.GetParameter(index++).Value = item.PremiereDate;
                    _saveItemCommand.GetParameter(index++).Value = item.ProductionYear;

                    if (item.ParentId == Guid.Empty)
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = item.ParentId;
                    }

                    _saveItemCommand.GetParameter(index++).Value = string.Join("|", item.Genres.ToArray());
                    _saveItemCommand.GetParameter(index++).Value = item.GetInheritedParentalRatingValue() ?? 0;

                    _saveItemCommand.GetParameter(index++).Value = LatestSchemaVersion;
                    _saveItemCommand.GetParameter(index++).Value = item.SortName;
                    _saveItemCommand.GetParameter(index++).Value = item.RunTimeTicks;

                    _saveItemCommand.GetParameter(index++).Value = item.OfficialRatingDescription;
                    _saveItemCommand.GetParameter(index++).Value = item.HomePageUrl;
                    _saveItemCommand.GetParameter(index++).Value = item.VoteCount;
                    _saveItemCommand.GetParameter(index++).Value = item.DisplayMediaType;
                    _saveItemCommand.GetParameter(index++).Value = item.DateCreated;
                    _saveItemCommand.GetParameter(index++).Value = item.DateModified;

                    _saveItemCommand.GetParameter(index++).Value = item.ForcedSortName;
                    _saveItemCommand.GetParameter(index++).Value = item.IsOffline;
                    _saveItemCommand.GetParameter(index++).Value = item.LocationType.ToString();

                    _saveItemCommand.GetParameter(index++).Value = item.PreferredMetadataLanguage;
                    _saveItemCommand.GetParameter(index++).Value = item.PreferredMetadataCountryCode;
                    _saveItemCommand.GetParameter(index++).Value = item.IsHD;
                    _saveItemCommand.GetParameter(index++).Value = item.ExternalEtag;

                    if (item.DateLastRefreshed == default(DateTime))
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = item.DateLastRefreshed;
                    }

                    if (item.DateLastSaved == default(DateTime))
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = item.DateLastSaved;
                    }

                    _saveItemCommand.GetParameter(index++).Value = item.IsInMixedFolder;
                    _saveItemCommand.GetParameter(index++).Value = string.Join("|", item.LockedFields.Select(i => i.ToString()).ToArray());
                    _saveItemCommand.GetParameter(index++).Value = string.Join("|", item.Studios.ToArray());

                    if (item.Audio.HasValue)
                    {
                        _saveItemCommand.GetParameter(index++).Value = item.Audio.Value.ToString();
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    _saveItemCommand.GetParameter(index++).Value = item.ServiceName;

                    if (item.Tags.Count > 0)
                    {
                        _saveItemCommand.GetParameter(index++).Value = string.Join("|", item.Tags.ToArray());
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    _saveItemCommand.GetParameter(index++).Value = item.IsFolder;

                    _saveItemCommand.GetParameter(index++).Value = item.GetBlockUnratedType().ToString();

                    var topParent = item.GetTopParent();
                    if (topParent != null)
                    {
                        //Logger.Debug("Item {0} has top parent {1}", item.Id, topParent.Id);
                        _saveItemCommand.GetParameter(index++).Value = topParent.Id.ToString("N");
                    }
                    else
                    {
                        //Logger.Debug("Item {0} has null top parent", item.Id);
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    var isByName = false;
                    var byName = item as IItemByName;
                    if (byName != null)
                    {
                        var dualAccess = item as IHasDualAccess;
                        isByName = dualAccess == null || dualAccess.IsAccessedByName;
                    }
                    _saveItemCommand.GetParameter(index++).Value = isByName;

                    _saveItemCommand.GetParameter(index++).Value = item.SourceType.ToString();

                    var trailer = item as Trailer;
                    if (trailer != null && trailer.TrailerTypes.Count > 0)
                    {
                        _saveItemCommand.GetParameter(index++).Value = string.Join("|", trailer.TrailerTypes.Select(i => i.ToString()).ToArray());
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    _saveItemCommand.GetParameter(index++).Value = item.CriticRating;
                    _saveItemCommand.GetParameter(index++).Value = item.CriticRatingSummary;

                    var inheritedTags = item.GetInheritedTags();
                    if (inheritedTags.Count > 0)
                    {
                        _saveItemCommand.GetParameter(index++).Value = string.Join("|", inheritedTags.ToArray());
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    if (string.IsNullOrWhiteSpace(item.Name))
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = GetCleanValue(item.Name);
                    }

                    _saveItemCommand.GetParameter(index++).Value = item.GetPresentationUniqueKey();
                    _saveItemCommand.GetParameter(index++).Value = item.SlugName;
                    _saveItemCommand.GetParameter(index++).Value = item.OriginalTitle;

                    var video = item as Video;
                    if (video != null)
                    {
                        _saveItemCommand.GetParameter(index++).Value = video.PrimaryVersionId;
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    var folder = item as Folder;
                    if (folder != null && folder.DateLastMediaAdded.HasValue)
                    {
                        _saveItemCommand.GetParameter(index++).Value = folder.DateLastMediaAdded.Value;
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    _saveItemCommand.GetParameter(index++).Value = item.Album;

                    _saveItemCommand.GetParameter(index++).Value = item.IsVirtualItem;

                    var hasSeries = item as IHasSeries;
                    if (hasSeries != null)
                    {
                        _saveItemCommand.GetParameter(index++).Value = hasSeries.FindSeriesName();
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    _saveItemCommand.GetParameter(index++).Value = item.GetUserDataKeys().FirstOrDefault();

                    var episode = item as Episode;
                    if (episode != null)
                    {
                        _saveItemCommand.GetParameter(index++).Value = episode.FindSeasonName();
                        _saveItemCommand.GetParameter(index++).Value = episode.FindSeasonId();
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    if (hasSeries != null)
                    {
                        _saveItemCommand.GetParameter(index++).Value = hasSeries.FindSeriesId();
                        _saveItemCommand.GetParameter(index++).Value = hasSeries.FindSeriesSortName();
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    _saveItemCommand.Transaction = transaction;

                    _saveItemCommand.ExecuteNonQuery();

                    if (item.SupportsAncestors)
                    {
                        UpdateAncestors(item.Id, item.GetAncestorIds().Distinct().ToList(), transaction);
                    }

                    UpdateUserDataKeys(item.Id, item.GetUserDataKeys().Distinct(StringComparer.OrdinalIgnoreCase).ToList(), transaction);
                    UpdateImages(item.Id, item.ImageInfos, transaction);
                    UpdateProviderIds(item.Id, item.ProviderIds, transaction);
                    UpdateItemValues(item.Id, GetItemValuesToSave(item), transaction);
                }

                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            catch (Exception e)
            {
                Logger.ErrorException("Failed to save items:", e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }

                WriteLock.Release();
            }
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

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select " + string.Join(",", _retriveItemColumns) + " from TypedBaseItems where guid = @guid";
                cmd.Parameters.Add(cmd, "@guid", DbType.Guid).Value = id;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        return GetItem(reader);
                    }
                }
                return null;
            }
        }

        private BaseItem GetItem(IDataReader reader)
        {
            var typeString = reader.GetString(0);

            var type = _typeMapper.GetType(typeString);

            if (type == null)
            {
                //Logger.Debug("Unknown type {0}", typeString);

                return null;
            }

            BaseItem item = null;

            using (var stream = reader.GetMemoryStream(1))
            {
                try
                {
                    item = _jsonSerializer.DeserializeFromStream(stream, type) as BaseItem;
                }
                catch (SerializationException ex)
                {
                    Logger.ErrorException("Error deserializing item", ex);
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
            }

            if (!reader.IsDBNull(2))
            {
                var hasStartDate = item as IHasStartDate;
                if (hasStartDate != null)
                {
                    hasStartDate.StartDate = reader.GetDateTime(2).ToUniversalTime();
                }
            }

            if (!reader.IsDBNull(3))
            {
                item.EndDate = reader.GetDateTime(3).ToUniversalTime();
            }

            if (!reader.IsDBNull(4))
            {
                item.IsOffline = reader.GetBoolean(4);
            }

            if (!reader.IsDBNull(5))
            {
                item.ChannelId = reader.GetString(5);
            }

            var hasProgramAttributes = item as IHasProgramAttributes;
            if (hasProgramAttributes != null)
            {
                if (!reader.IsDBNull(6))
                {
                    hasProgramAttributes.IsMovie = reader.GetBoolean(6);
                }

                if (!reader.IsDBNull(7))
                {
                    hasProgramAttributes.IsSports = reader.GetBoolean(7);
                }

                if (!reader.IsDBNull(8))
                {
                    hasProgramAttributes.IsKids = reader.GetBoolean(8);
                }

                if (!reader.IsDBNull(9))
                {
                    hasProgramAttributes.IsSeries = reader.GetBoolean(9);
                }

                if (!reader.IsDBNull(10))
                {
                    hasProgramAttributes.IsLive = reader.GetBoolean(10);
                }

                if (!reader.IsDBNull(11))
                {
                    hasProgramAttributes.IsNews = reader.GetBoolean(11);
                }

                if (!reader.IsDBNull(12))
                {
                    hasProgramAttributes.IsPremiere = reader.GetBoolean(12);
                }

                if (!reader.IsDBNull(13))
                {
                    hasProgramAttributes.EpisodeTitle = reader.GetString(13);
                }

                if (!reader.IsDBNull(14))
                {
                    hasProgramAttributes.IsRepeat = reader.GetBoolean(14);
                }
            }

            if (!reader.IsDBNull(15))
            {
                item.CommunityRating = reader.GetFloat(15);
            }

            if (!reader.IsDBNull(16))
            {
                item.CustomRating = reader.GetString(16);
            }

            if (!reader.IsDBNull(17))
            {
                item.IndexNumber = reader.GetInt32(17);
            }

            if (!reader.IsDBNull(18))
            {
                item.IsLocked = reader.GetBoolean(18);
            }

            if (!reader.IsDBNull(19))
            {
                item.PreferredMetadataLanguage = reader.GetString(19);
            }

            if (!reader.IsDBNull(20))
            {
                item.PreferredMetadataCountryCode = reader.GetString(20);
            }

            if (!reader.IsDBNull(21))
            {
                item.IsHD = reader.GetBoolean(21);
            }

            if (!reader.IsDBNull(22))
            {
                item.ExternalEtag = reader.GetString(22);
            }

            if (!reader.IsDBNull(23))
            {
                item.DateLastRefreshed = reader.GetDateTime(23).ToUniversalTime();
            }

            if (!reader.IsDBNull(24))
            {
                item.Name = reader.GetString(24);
            }

            if (!reader.IsDBNull(25))
            {
                item.Path = reader.GetString(25);
            }

            if (!reader.IsDBNull(26))
            {
                item.PremiereDate = reader.GetDateTime(26).ToUniversalTime();
            }

            if (!reader.IsDBNull(27))
            {
                item.Overview = reader.GetString(27);
            }

            if (!reader.IsDBNull(28))
            {
                item.ParentIndexNumber = reader.GetInt32(28);
            }

            if (!reader.IsDBNull(29))
            {
                item.ProductionYear = reader.GetInt32(29);
            }

            if (!reader.IsDBNull(30))
            {
                item.OfficialRating = reader.GetString(30);
            }

            if (!reader.IsDBNull(31))
            {
                item.OfficialRatingDescription = reader.GetString(31);
            }

            if (!reader.IsDBNull(32))
            {
                item.HomePageUrl = reader.GetString(32);
            }

            if (!reader.IsDBNull(33))
            {
                item.DisplayMediaType = reader.GetString(33);
            }

            if (!reader.IsDBNull(34))
            {
                item.ForcedSortName = reader.GetString(34);
            }

            if (!reader.IsDBNull(35))
            {
                item.RunTimeTicks = reader.GetInt64(35);
            }

            if (!reader.IsDBNull(36))
            {
                item.VoteCount = reader.GetInt32(36);
            }

            if (!reader.IsDBNull(37))
            {
                item.DateCreated = reader.GetDateTime(37).ToUniversalTime();
            }

            if (!reader.IsDBNull(38))
            {
                item.DateModified = reader.GetDateTime(38).ToUniversalTime();
            }

            item.Id = reader.GetGuid(39);

            if (!reader.IsDBNull(40))
            {
                item.Genres = reader.GetString(40).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            }

            if (!reader.IsDBNull(41))
            {
                item.ParentId = reader.GetGuid(41);
            }

            if (!reader.IsDBNull(42))
            {
                item.Audio = (ProgramAudio)Enum.Parse(typeof(ProgramAudio), reader.GetString(42), true);
            }

            if (!reader.IsDBNull(43))
            {
                item.ServiceName = reader.GetString(43);
            }

            if (!reader.IsDBNull(44))
            {
                item.IsInMixedFolder = reader.GetBoolean(44);
            }

            if (!reader.IsDBNull(45))
            {
                item.DateLastSaved = reader.GetDateTime(45).ToUniversalTime();
            }

            if (!reader.IsDBNull(46))
            {
                item.LockedFields = reader.GetString(46).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => (MetadataFields)Enum.Parse(typeof(MetadataFields), i, true)).ToList();
            }

            if (!reader.IsDBNull(47))
            {
                item.Studios = reader.GetString(47).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            }

            if (!reader.IsDBNull(48))
            {
                item.Tags = reader.GetString(48).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
            }

            if (!reader.IsDBNull(49))
            {
                item.SourceType = (SourceType)Enum.Parse(typeof(SourceType), reader.GetString(49), true);
            }

            var trailer = item as Trailer;
            if (trailer != null)
            {
                if (!reader.IsDBNull(50))
                {
                    trailer.TrailerTypes = reader.GetString(50).Split('|').Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => (TrailerType)Enum.Parse(typeof(TrailerType), i, true)).ToList();
                }
            }

            var index = 51;

            if (!reader.IsDBNull(index))
            {
                item.OriginalTitle = reader.GetString(index);
            }
            index++;

            var video = item as Video;
            if (video != null)
            {
                if (!reader.IsDBNull(index))
                {
                    video.PrimaryVersionId = reader.GetString(index);
                }
            }
            index++;

            var folder = item as Folder;
            if (folder != null && !reader.IsDBNull(index))
            {
                folder.DateLastMediaAdded = reader.GetDateTime(index).ToUniversalTime();
            }
            index++;

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
                item.CriticRatingSummary = reader.GetString(index);
            }
            index++;

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
            catch (DirectoryNotFoundException)
            {
                return new List<ItemReview>();
            }
            catch (FileNotFoundException)
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
            Directory.CreateDirectory(_criticReviewsPath);

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
            var list = new List<ChapterInfo>();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select StartPositionTicks,Name,ImagePath,ImageDateModified from " + ChaptersTableName + " where ItemId = @ItemId order by ChapterIndex asc";

                cmd.Parameters.Add(cmd, "@ItemId", DbType.Guid).Value = id;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        list.Add(GetChapter(reader));
                    }
                }
            }

            return list;
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

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select StartPositionTicks,Name,ImagePath,ImageDateModified from " + ChaptersTableName + " where ItemId = @ItemId and ChapterIndex=@ChapterIndex";

                cmd.Parameters.Add(cmd, "@ItemId", DbType.Guid).Value = id;
                cmd.Parameters.Add(cmd, "@ChapterIndex", DbType.Int32).Value = index;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        return GetChapter(reader);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the chapter.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>ChapterInfo.</returns>
        private ChapterInfo GetChapter(IDataReader reader)
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
                chapter.ImageDateModified = reader.GetDateTime(3).ToUniversalTime();
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

            await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                // First delete chapters
                _deleteChaptersCommand.GetParameter(0).Value = id;

                _deleteChaptersCommand.Transaction = transaction;

                _deleteChaptersCommand.ExecuteNonQuery();

                var index = 0;

                foreach (var chapter in chapters)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _saveChapterCommand.GetParameter(0).Value = id;
                    _saveChapterCommand.GetParameter(1).Value = index;
                    _saveChapterCommand.GetParameter(2).Value = chapter.StartPositionTicks;
                    _saveChapterCommand.GetParameter(3).Value = chapter.Name;
                    _saveChapterCommand.GetParameter(4).Value = chapter.ImagePath;
                    _saveChapterCommand.GetParameter(5).Value = chapter.ImageDateModified;

                    _saveChapterCommand.Transaction = transaction;

                    _saveChapterCommand.ExecuteNonQuery();

                    index++;
                }

                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            catch (Exception e)
            {
                Logger.ErrorException("Failed to save chapters:", e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }

                WriteLock.Release();
            }
        }

        protected override void CloseConnection()
        {
            if (_connection != null)
            {
                if (_connection.IsOpen())
                {
                    _connection.Close();
                }

                _connection.Dispose();
                _connection = null;
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
                return true;
            }

            if (query.SortBy != null && query.SortBy.Length > 0)
            {
                if (query.SortBy.Contains(ItemSortBy.IsFavoriteOrLiked, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (query.SortBy.Contains(ItemSortBy.IsPlayed, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (query.SortBy.Contains(ItemSortBy.IsUnplayed, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (query.SortBy.Contains(ItemSortBy.PlayCount, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (query.SortBy.Contains(ItemSortBy.DatePlayed, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
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

        private string[] GetFinalColumnsToSelect(InternalItemsQuery query, string[] startColumns, IDbCommand cmd)
        {
            var list = startColumns.ToList();

            if (EnableJoinUserData(query))
            {
                list.Add("UserDataDb.UserData.UserId");
                list.Add("UserDataDb.UserData.lastPlayedDate");
                list.Add("UserDataDb.UserData.playbackPositionTicks");
                list.Add("UserDataDb.UserData.playcount");
                list.Add("UserDataDb.UserData.isFavorite");
                list.Add("UserDataDb.UserData.played");
                list.Add("UserDataDb.UserData.rating");
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

                //// genres
                builder.Append("+ ((Select count(CleanValue) from ItemValues where ItemId=Guid and Type=2 and CleanValue in (select CleanValue from itemvalues where ItemId=@SimilarItemId and type=2)) * 10)");

                //// tags
                builder.Append("+ ((Select count(CleanValue) from ItemValues where ItemId=Guid and Type=4 and CleanValue in (select CleanValue from itemvalues where ItemId=@SimilarItemId and type=4)) * 10)");

                builder.Append("+ ((Select count(CleanValue) from ItemValues where ItemId=Guid and Type=5 and CleanValue in (select CleanValue from itemvalues where ItemId=@SimilarItemId and type=5)) * 10)");

                builder.Append("+ ((Select count(CleanValue) from ItemValues where ItemId=Guid and Type=3 and CleanValue in (select CleanValue from itemvalues where ItemId=@SimilarItemId and type=3)) * 3)");

                //builder.Append("+ ((Select count(Name) from People where ItemId=Guid and Name in (select Name from People where ItemId=@SimilarItemId)) * 3)");

                ////builder.Append("(select group_concat((Select Name from People where ItemId=Guid and Name in (Select Name from People where ItemId=@SimilarItemId)), '|'))");

                builder.Append(") as SimilarityScore");

                list.Add(builder.ToString());
                cmd.Parameters.Add(cmd, "@ItemOfficialRating", DbType.String).Value = item.OfficialRating;
                cmd.Parameters.Add(cmd, "@ItemProductionYear", DbType.Int32).Value = item.ProductionYear ?? 0;
                cmd.Parameters.Add(cmd, "@SimilarItemId", DbType.Guid).Value = item.Id;

                var excludeIds = query.ExcludeItemIds.ToList();
                excludeIds.Add(item.Id.ToString("N"));
                query.ExcludeItemIds = excludeIds.ToArray();

                query.ExcludeProviderIds = item.ProviderIds;
            }

            return list.ToArray();
        }

        private string GetJoinUserDataText(InternalItemsQuery query)
        {
            if (!EnableJoinUserData(query))
            {
                return string.Empty;
            }

            if (_config.Configuration.SchemaVersion >= 96)
            {
                return " left join UserDataDb.UserData on UserDataKey=UserDataDb.UserData.Key And (UserId=@UserId)";
            }

            return " left join UserDataDb.UserData on (select UserDataKey from UserDataKeys where ItemId=Guid order by Priority LIMIT 1)=UserDataDb.UserData.Key And (UserId=@UserId)";
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

        public List<BaseItem> GetItemList(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            var now = DateTime.UtcNow;

            var list = new List<BaseItem>();

            // Hack for right now since we currently don't support filtering out these duplicates within a query
            if (query.Limit.HasValue && query.EnableGroupByMetadataKey)
            {
                query.Limit = query.Limit.Value + 4;
            }

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, _retriveItemColumns, cmd)) + GetFromText();
                cmd.CommandText += GetJoinUserDataText(query);

                if (EnableJoinUserData(query))
                {
                    cmd.Parameters.Add(cmd, "@UserId", DbType.Guid).Value = query.User.Id;
                }

                var whereClauses = GetWhereClauses(query, cmd);

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                cmd.CommandText += whereText;

                cmd.CommandText += GetGroupBy(query);

                cmd.CommandText += GetOrderByText(query);

                if (query.Limit.HasValue || query.StartIndex.HasValue)
                {
                    var offset = query.StartIndex ?? 0;

                    if (query.Limit.HasValue || offset > 0)
                    {
                        cmd.CommandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                    }

                    if (offset > 0)
                    {
                        cmd.CommandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                    }
                }

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    LogQueryTime("GetItemList", cmd, now);

                    while (reader.Read())
                    {
                        var item = GetItem(reader);
                        if (item != null)
                        {
                            list.Add(item);
                        }
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

            return list;
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

        private void LogQueryTime(string methodName, IDbCommand cmd, DateTime startDate)
        {
            var elapsed = (DateTime.UtcNow - startDate).TotalMilliseconds;

            var slowThreshold = 1000;

#if DEBUG
            slowThreshold = 50;
#endif

            if (elapsed >= slowThreshold)
            {
                Logger.Debug("{2} query time (slow): {0}ms. Query: {1}",
                    Convert.ToInt32(elapsed),
                    cmd.CommandText,
                    methodName);
            }
            else
            {
                //Logger.Debug("{2} query time: {0}ms. Query: {1}",
                //    Convert.ToInt32(elapsed),
                //    cmd.CommandText,
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
                var list = GetItemList(query);
                return new QueryResult<BaseItem>
                {
                    Items = list.ToArray(),
                    TotalRecordCount = list.Count
                };
            }

            var now = DateTime.UtcNow;

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, _retriveItemColumns, cmd)) + GetFromText();
                cmd.CommandText += GetJoinUserDataText(query);

                if (EnableJoinUserData(query))
                {
                    cmd.Parameters.Add(cmd, "@UserId", DbType.Guid).Value = query.User.Id;
                }

                var whereClauses = GetWhereClauses(query, cmd);

                var whereTextWithoutPaging = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                cmd.CommandText += whereText;

                cmd.CommandText += GetGroupBy(query);

                cmd.CommandText += GetOrderByText(query);

                if (query.Limit.HasValue || query.StartIndex.HasValue)
                {
                    var offset = query.StartIndex ?? 0;

                    if (query.Limit.HasValue || offset > 0)
                    {
                        cmd.CommandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                    }

                    if (offset > 0)
                    {
                        cmd.CommandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                    }
                }

                cmd.CommandText += ";";

                var isReturningZeroItems = query.Limit.HasValue && query.Limit <= 0;

                if (isReturningZeroItems)
                {
                    cmd.CommandText = "";
                }

                if (EnableGroupByPresentationUniqueKey(query))
                {
                    cmd.CommandText += " select count (distinct PresentationUniqueKey)" + GetFromText();
                }
                else
                {
                    cmd.CommandText += " select count (guid)" + GetFromText();
                }

                cmd.CommandText += GetJoinUserDataText(query);
                cmd.CommandText += whereTextWithoutPaging;

                var list = new List<BaseItem>();
                var count = 0;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    LogQueryTime("GetItems", cmd, now);

                    if (isReturningZeroItems)
                    {
                        if (reader.Read())
                        {
                            count = reader.GetInt32(0);
                        }
                    }
                    else
                    {
                        while (reader.Read())
                        {
                            var item = GetItem(reader);
                            if (item != null)
                            {
                                list.Add(item);
                            }
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            count = reader.GetInt32(0);
                        }
                    }
                }

                return new QueryResult<BaseItem>()
                {
                    Items = list.ToArray(),
                    TotalRecordCount = count
                };
            }
        }

        private string GetOrderByText(InternalItemsQuery query)
        {
            if (query.SimilarTo != null)
            {
                if (query.SortBy == null || query.SortBy.Length == 0)
                {
                    if (query.User != null)
                    {
                        query.SortBy = new[] { "SimilarityScore", ItemSortBy.Random };
                    }
                    else
                    {
                        query.SortBy = new[] { "SimilarityScore", ItemSortBy.Random };
                    }
                    query.SortOrder = SortOrder.Descending;
                }
            }

            if (query.SortBy == null || query.SortBy.Length == 0)
            {
                return string.Empty;
            }

            var isAscending = query.SortOrder != SortOrder.Descending;

            return " ORDER BY " + string.Join(",", query.SortBy.Select(i =>
            {
                var columnMap = MapOrderByField(i, query);
                var columnAscending = isAscending;
                if (columnMap.Item2)
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
                return new Tuple<string, bool>("(Select MAX(LastPlayedDate) from TypedBaseItems B" + GetJoinUserDataText(query) + " where B.Guid in (Select ItemId from AncestorIds where AncestorId in (select guid from typedbaseitems c where C.Type = 'MediaBrowser.Controller.Entities.TV.Series' And C.Guid in (Select AncestorId from AncestorIds where ItemId=A.Guid))))", false);
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

            var now = DateTime.UtcNow;

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "guid" }, cmd)) + GetFromText();
                cmd.CommandText += GetJoinUserDataText(query);

                if (EnableJoinUserData(query))
                {
                    cmd.Parameters.Add(cmd, "@UserId", DbType.Guid).Value = query.User.Id;
                }

                var whereClauses = GetWhereClauses(query, cmd);

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                cmd.CommandText += whereText;

                cmd.CommandText += GetGroupBy(query);

                cmd.CommandText += GetOrderByText(query);

                if (query.Limit.HasValue || query.StartIndex.HasValue)
                {
                    var offset = query.StartIndex ?? 0;

                    if (query.Limit.HasValue || offset > 0)
                    {
                        cmd.CommandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                    }

                    if (offset > 0)
                    {
                        cmd.CommandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                    }
                }

                var list = new List<Guid>();

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    LogQueryTime("GetItemIdsList", cmd, now);

                    while (reader.Read())
                    {
                        list.Add(reader.GetGuid(0));
                    }
                }

                return list;
            }
        }

        public QueryResult<Tuple<Guid, string>> GetItemIdsWithPath(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select guid,path from TypedBaseItems";

                var whereClauses = GetWhereClauses(query, cmd);

                var whereTextWithoutPaging = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                cmd.CommandText += whereText;

                cmd.CommandText += GetGroupBy(query);

                cmd.CommandText += GetOrderByText(query);

                if (query.Limit.HasValue || query.StartIndex.HasValue)
                {
                    var offset = query.StartIndex ?? 0;

                    if (query.Limit.HasValue || offset > 0)
                    {
                        cmd.CommandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                    }

                    if (offset > 0)
                    {
                        cmd.CommandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                    }
                }

                cmd.CommandText += "; select count (guid) from TypedBaseItems" + whereTextWithoutPaging;

                var list = new List<Tuple<Guid, string>>();
                var count = 0;

                Logger.Debug(cmd.CommandText);

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        var id = reader.GetGuid(0);
                        string path = null;

                        if (!reader.IsDBNull(1))
                        {
                            path = reader.GetString(1);
                        }
                        list.Add(new Tuple<Guid, string>(id, path));
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        count = reader.GetInt32(0);
                    }
                }

                return new QueryResult<Tuple<Guid, string>>()
                {
                    Items = list.ToArray(),
                    TotalRecordCount = count
                };
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
                var list = GetItemIdsList(query);
                return new QueryResult<Guid>
                {
                    Items = list.ToArray(),
                    TotalRecordCount = list.Count
                };
            }

            var now = DateTime.UtcNow;

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "guid" }, cmd)) + GetFromText();

                var whereClauses = GetWhereClauses(query, cmd);
                cmd.CommandText += GetJoinUserDataText(query);

                if (EnableJoinUserData(query))
                {
                    cmd.Parameters.Add(cmd, "@UserId", DbType.Guid).Value = query.User.Id;
                }

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                cmd.CommandText += whereText;

                cmd.CommandText += GetGroupBy(query);

                cmd.CommandText += GetOrderByText(query);

                if (query.Limit.HasValue || query.StartIndex.HasValue)
                {
                    var offset = query.StartIndex ?? 0;

                    if (query.Limit.HasValue || offset > 0)
                    {
                        cmd.CommandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                    }

                    if (offset > 0)
                    {
                        cmd.CommandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                    }
                }

                if (EnableGroupByPresentationUniqueKey(query))
                {
                    cmd.CommandText += "; select count (distinct PresentationUniqueKey)" + GetFromText();
                }
                else
                {
                    cmd.CommandText += "; select count (guid)" + GetFromText();
                }

                cmd.CommandText += GetJoinUserDataText(query);
                cmd.CommandText += whereText;

                var list = new List<Guid>();
                var count = 0;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    LogQueryTime("GetItemIds", cmd, now);

                    while (reader.Read())
                    {
                        list.Add(reader.GetGuid(0));
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        count = reader.GetInt32(0);
                    }
                }

                return new QueryResult<Guid>()
                {
                    Items = list.ToArray(),
                    TotalRecordCount = count
                };
            }
        }

        private List<string> GetWhereClauses(InternalItemsQuery query, IDbCommand cmd, string paramSuffix = "")
        {
            var whereClauses = new List<string>();

            if (EnableJoinUserData(query))
            {
                //whereClauses.Add("(UserId is null or UserId=@UserId)");
            }
            if (query.IsCurrentSchema.HasValue)
            {
                if (query.IsCurrentSchema.Value)
                {
                    whereClauses.Add("(SchemaVersion not null AND SchemaVersion=@SchemaVersion)");
                }
                else
                {
                    whereClauses.Add("(SchemaVersion is null or SchemaVersion<>@SchemaVersion)");
                }
                cmd.Parameters.Add(cmd, "@SchemaVersion", DbType.Int32).Value = LatestSchemaVersion;
            }
            if (query.IsHD.HasValue)
            {
                whereClauses.Add("IsHD=@IsHD");
                cmd.Parameters.Add(cmd, "@IsHD", DbType.Boolean).Value = query.IsHD;
            }
            if (query.IsLocked.HasValue)
            {
                whereClauses.Add("IsLocked=@IsLocked");
                cmd.Parameters.Add(cmd, "@IsLocked", DbType.Boolean).Value = query.IsLocked;
            }
            if (query.IsOffline.HasValue)
            {
                whereClauses.Add("IsOffline=@IsOffline");
                cmd.Parameters.Add(cmd, "@IsOffline", DbType.Boolean).Value = query.IsOffline;
            }
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
                }
                else
                {
                    whereClauses.Add("(IsMovie is null OR IsMovie=@IsMovie)");
                }
                cmd.Parameters.Add(cmd, "@IsMovie", DbType.Boolean).Value = query.IsMovie;
            }
            if (query.IsKids.HasValue)
            {
                whereClauses.Add("IsKids=@IsKids");
                cmd.Parameters.Add(cmd, "@IsKids", DbType.Boolean).Value = query.IsKids;
            }
            if (query.IsSports.HasValue)
            {
                whereClauses.Add("IsSports=@IsSports");
                cmd.Parameters.Add(cmd, "@IsSports", DbType.Boolean).Value = query.IsSports;
            }
            if (query.IsFolder.HasValue)
            {
                whereClauses.Add("IsFolder=@IsFolder");
                cmd.Parameters.Add(cmd, "@IsFolder", DbType.Boolean).Value = query.IsFolder;
            }

            var includeTypes = query.IncludeItemTypes.SelectMany(MapIncludeItemTypes).ToArray();
            if (includeTypes.Length == 1)
            {
                whereClauses.Add("type=@type" + paramSuffix);
                cmd.Parameters.Add(cmd, "@type" + paramSuffix, DbType.String).Value = includeTypes[0];
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
                cmd.Parameters.Add(cmd, "@type", DbType.String).Value = excludeTypes[0];
            }
            else if (excludeTypes.Length > 1)
            {
                var inClause = string.Join(",", excludeTypes.Select(i => "'" + i + "'").ToArray());
                whereClauses.Add(string.Format("type not in ({0})", inClause));
            }

            if (query.ChannelIds.Length == 1)
            {
                whereClauses.Add("ChannelId=@ChannelId");
                cmd.Parameters.Add(cmd, "@ChannelId", DbType.String).Value = query.ChannelIds[0];
            }
            if (query.ChannelIds.Length > 1)
            {
                var inClause = string.Join(",", query.ChannelIds.Select(i => "'" + i + "'").ToArray());
                whereClauses.Add(string.Format("ChannelId in ({0})", inClause));
            }

            if (query.ParentId.HasValue)
            {
                whereClauses.Add("ParentId=@ParentId");
                cmd.Parameters.Add(cmd, "@ParentId", DbType.Guid).Value = query.ParentId.Value;
            }

            if (!string.IsNullOrWhiteSpace(query.Path))
            {
                whereClauses.Add("Path=@Path");
                cmd.Parameters.Add(cmd, "@Path", DbType.String).Value = query.Path;
            }

            if (!string.IsNullOrWhiteSpace(query.PresentationUniqueKey))
            {
                whereClauses.Add("PresentationUniqueKey=@PresentationUniqueKey");
                cmd.Parameters.Add(cmd, "@PresentationUniqueKey", DbType.String).Value = query.PresentationUniqueKey;
            }

            if (query.MinCommunityRating.HasValue)
            {
                whereClauses.Add("CommunityRating>=@MinCommunityRating");
                cmd.Parameters.Add(cmd, "@MinCommunityRating", DbType.Double).Value = query.MinCommunityRating.Value;
            }

            if (query.MinIndexNumber.HasValue)
            {
                whereClauses.Add("IndexNumber>=@MinIndexNumber");
                cmd.Parameters.Add(cmd, "@MinIndexNumber", DbType.Int32).Value = query.MinIndexNumber.Value;
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
                cmd.Parameters.Add(cmd, "@IndexNumber", DbType.Int32).Value = query.IndexNumber.Value;
            }
            if (query.ParentIndexNumber.HasValue)
            {
                whereClauses.Add("ParentIndexNumber=@ParentIndexNumber");
                cmd.Parameters.Add(cmd, "@ParentIndexNumber", DbType.Int32).Value = query.ParentIndexNumber.Value;
            }
            if (query.ParentIndexNumberNotEquals.HasValue)
            {
                whereClauses.Add("(ParentIndexNumber<>@ParentIndexNumberNotEquals or ParentIndexNumber is null)");
                cmd.Parameters.Add(cmd, "@ParentIndexNumberNotEquals", DbType.Int32).Value = query.ParentIndexNumberNotEquals.Value;
            }
            if (query.MinEndDate.HasValue)
            {
                whereClauses.Add("EndDate>=@MinEndDate");
                cmd.Parameters.Add(cmd, "@MinEndDate", DbType.Date).Value = query.MinEndDate.Value;
            }

            if (query.MaxEndDate.HasValue)
            {
                whereClauses.Add("EndDate<=@MaxEndDate");
                cmd.Parameters.Add(cmd, "@MaxEndDate", DbType.Date).Value = query.MaxEndDate.Value;
            }

            if (query.MinStartDate.HasValue)
            {
                whereClauses.Add("StartDate>=@MinStartDate");
                cmd.Parameters.Add(cmd, "@MinStartDate", DbType.Date).Value = query.MinStartDate.Value;
            }

            if (query.MaxStartDate.HasValue)
            {
                whereClauses.Add("StartDate<=@MaxStartDate");
                cmd.Parameters.Add(cmd, "@MaxStartDate", DbType.Date).Value = query.MaxStartDate.Value;
            }

            if (query.MinPremiereDate.HasValue)
            {
                whereClauses.Add("PremiereDate>=@MinPremiereDate");
                cmd.Parameters.Add(cmd, "@MinPremiereDate", DbType.Date).Value = query.MinPremiereDate.Value;
            }
            if (query.MaxPremiereDate.HasValue)
            {
                whereClauses.Add("PremiereDate<=@MaxPremiereDate");
                cmd.Parameters.Add(cmd, "@MaxPremiereDate", DbType.Date).Value = query.MaxPremiereDate.Value;
            }

            if (query.SourceTypes.Length == 1)
            {
                whereClauses.Add("SourceType=@SourceType");
                cmd.Parameters.Add(cmd, "@SourceType", DbType.String).Value = query.SourceTypes[0];
            }
            else if (query.SourceTypes.Length > 1)
            {
                var inClause = string.Join(",", query.SourceTypes.Select(i => "'" + i + "'").ToArray());
                whereClauses.Add(string.Format("SourceType in ({0})", inClause));
            }

            if (query.ExcludeSourceTypes.Length == 1)
            {
                whereClauses.Add("SourceType<>@SourceType");
                cmd.Parameters.Add(cmd, "@SourceType", DbType.String).Value = query.SourceTypes[0];
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
                    clauses.Add("TrailerTypes like @TrailerTypes" + index);
                    cmd.Parameters.Add(cmd, "@TrailerTypes" + index, DbType.String).Value = "%" + type + "%";
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
                    cmd.Parameters.Add(cmd, "@MaxStartDate", DbType.Date).Value = DateTime.UtcNow;

                    whereClauses.Add("EndDate>=@MinEndDate");
                    cmd.Parameters.Add(cmd, "@MinEndDate", DbType.Date).Value = DateTime.UtcNow;
                }
                else
                {
                    whereClauses.Add("(StartDate>@IsAiringDate OR EndDate < @IsAiringDate)");
                    cmd.Parameters.Add(cmd, "@IsAiringDate", DbType.Date).Value = DateTime.UtcNow;
                }
            }

            if (query.PersonIds.Length > 0)
            {
                // Todo: improve without having to do this
                query.Person = query.PersonIds.Select(i => RetrieveItem(new Guid(i))).Where(i => i != null).Select(i => i.Name).FirstOrDefault();
            }

            if (!string.IsNullOrWhiteSpace(query.Person))
            {
                whereClauses.Add("Guid in (select ItemId from People where Name=@PersonName)");
                cmd.Parameters.Add(cmd, "@PersonName", DbType.String).Value = query.Person;
            }

            if (!string.IsNullOrWhiteSpace(query.SlugName))
            {
                whereClauses.Add("SlugName=@SlugName");
                cmd.Parameters.Add(cmd, "@SlugName", DbType.String).Value = query.SlugName;
            }

            if (!string.IsNullOrWhiteSpace(query.MinSortName))
            {
                whereClauses.Add("SortName>=@MinSortName");
                cmd.Parameters.Add(cmd, "@MinSortName", DbType.String).Value = query.MinSortName;
            }

            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                whereClauses.Add("CleanName=@Name");
                cmd.Parameters.Add(cmd, "@Name", DbType.String).Value = GetCleanValue(query.Name);
            }

            if (!string.IsNullOrWhiteSpace(query.NameContains))
            {
                whereClauses.Add("CleanName like @NameContains");
                cmd.Parameters.Add(cmd, "@NameContains", DbType.String).Value = "%" + GetCleanValue(query.NameContains) + "%";
            }
            if (!string.IsNullOrWhiteSpace(query.NameStartsWith))
            {
                whereClauses.Add("SortName like @NameStartsWith");
                cmd.Parameters.Add(cmd, "@NameStartsWith", DbType.String).Value = query.NameStartsWith + "%";
            }
            if (!string.IsNullOrWhiteSpace(query.NameStartsWithOrGreater))
            {
                whereClauses.Add("SortName >= @NameStartsWithOrGreater");
                // lowercase this because SortName is stored as lowercase
                cmd.Parameters.Add(cmd, "@NameStartsWithOrGreater", DbType.String).Value = query.NameStartsWithOrGreater.ToLower();
            }
            if (!string.IsNullOrWhiteSpace(query.NameLessThan))
            {
                whereClauses.Add("SortName < @NameLessThan");
                // lowercase this because SortName is stored as lowercase
                cmd.Parameters.Add(cmd, "@NameLessThan", DbType.String).Value = query.NameLessThan.ToLower();
            }

            if (query.ImageTypes.Length > 0 && _config.Configuration.SchemaVersion >= 87)
            {
                var requiredImageIndex = 0;

                foreach (var requiredImage in query.ImageTypes)
                {
                    var paramName = "@RequiredImageType" + requiredImageIndex;
                    whereClauses.Add("(select path from images where ItemId=Guid and ImageType=" + paramName + " limit 1) not null");
                    cmd.Parameters.Add(cmd, paramName, DbType.Int32).Value = (int)requiredImage;
                    requiredImageIndex++;
                }
            }

            if (query.IsLiked.HasValue)
            {
                if (query.IsLiked.Value)
                {
                    whereClauses.Add("rating>=@UserRating");
                    cmd.Parameters.Add(cmd, "@UserRating", DbType.Double).Value = UserItemData.MinLikeValue;
                }
                else
                {
                    whereClauses.Add("(rating is null or rating<@UserRating)");
                    cmd.Parameters.Add(cmd, "@UserRating", DbType.Double).Value = UserItemData.MinLikeValue;
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
                cmd.Parameters.Add(cmd, "@IsFavoriteOrLiked", DbType.Boolean).Value = query.IsFavoriteOrLiked.Value;
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
                cmd.Parameters.Add(cmd, "@IsFavorite", DbType.Boolean).Value = query.IsFavorite.Value;
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
                    cmd.Parameters.Add(cmd, "@IsPlayed", DbType.Boolean).Value = query.IsPlayed.Value;
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

            if (query.ArtistNames.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var artist in query.ArtistNames)
                {
                    clauses.Add("@ArtistName" + index + " in (select CleanValue from itemvalues where ItemId=Guid and Type <= 1)");
                    cmd.Parameters.Add(cmd, "@ArtistName" + index, DbType.String).Value = GetCleanValue(artist);
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
                    var artistItem = RetrieveItem(new Guid(artistId));
                    if (artistItem != null)
                    {
                        clauses.Add("@ExcludeArtistName" + index + " not in (select CleanValue from itemvalues where ItemId=Guid and Type <= 1)");
                        cmd.Parameters.Add(cmd, "@ExcludeArtistName" + index, DbType.String).Value = GetCleanValue(artistItem.Name);
                        index++;
                    }
                }
                var clause = "(" + string.Join(" AND ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.GenreIds.Length > 0)
            {
                // Todo: improve without having to do this
                query.Genres = query.GenreIds.Select(i => RetrieveItem(new Guid(i))).Where(i => i != null).Select(i => i.Name).ToArray();
            }

            if (query.Genres.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in query.Genres)
                {
                    clauses.Add("@Genre" + index + " in (select CleanValue from itemvalues where ItemId=Guid and Type=2)");
                    cmd.Parameters.Add(cmd, "@Genre" + index, DbType.String).Value = GetCleanValue(item);
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
                    cmd.Parameters.Add(cmd, "@Tag" + index, DbType.String).Value = GetCleanValue(item);
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.StudioIds.Length > 0)
            {
                // Todo: improve without having to do this
                query.Studios = query.StudioIds.Select(i => RetrieveItem(new Guid(i))).Where(i => i != null).Select(i => i.Name).ToArray();
            }

            if (query.Studios.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in query.Studios)
                {
                    clauses.Add("@Studio" + index + " in (select CleanValue from itemvalues where ItemId=Guid and Type=3)");
                    cmd.Parameters.Add(cmd, "@Studio" + index, DbType.String).Value = GetCleanValue(item);
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
                    cmd.Parameters.Add(cmd, "@Keyword" + index, DbType.String).Value = GetCleanValue(item);
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
                    cmd.Parameters.Add(cmd, "@OfficialRating" + index, DbType.String).Value = item;
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.MinParentalRating.HasValue)
            {
                whereClauses.Add("InheritedParentalRatingValue<=@MinParentalRating");
                cmd.Parameters.Add(cmd, "@MinParentalRating", DbType.Int32).Value = query.MinParentalRating.Value;
            }

            if (query.MaxParentalRating.HasValue)
            {
                whereClauses.Add("InheritedParentalRatingValue<=@MaxParentalRating");
                cmd.Parameters.Add(cmd, "@MaxParentalRating", DbType.Int32).Value = query.MaxParentalRating.Value;
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
                cmd.Parameters.Add(cmd, "@Years", DbType.Int32).Value = query.Years[0].ToString();
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
                    cmd.Parameters.Add(cmd, "@LocationType", DbType.String).Value = query.LocationTypes[0].ToString();
                }
            }
            else if (query.LocationTypes.Length > 1)
            {
                var val = string.Join(",", query.LocationTypes.Select(i => "'" + i + "'").ToArray());

                whereClauses.Add("LocationType in (" + val + ")");
            }
            if (query.ExcludeLocationTypes.Length == 1)
            {
                if (query.ExcludeLocationTypes[0] == LocationType.Virtual && _config.Configuration.SchemaVersion >= 90)
                {
                    query.IsVirtualItem = false;
                }
                else
                {
                    whereClauses.Add("LocationType<>@ExcludeLocationTypes");
                    cmd.Parameters.Add(cmd, "@ExcludeLocationTypes", DbType.String).Value = query.ExcludeLocationTypes[0].ToString();
                }
            }
            else if (query.ExcludeLocationTypes.Length > 1)
            {
                var val = string.Join(",", query.ExcludeLocationTypes.Select(i => "'" + i + "'").ToArray());

                whereClauses.Add("LocationType not in (" + val + ")");
            }
            if (query.IsVirtualItem.HasValue)
            {
                if (_config.Configuration.SchemaVersion >= 90)
                {
                    whereClauses.Add("IsVirtualItem=@IsVirtualItem");
                    cmd.Parameters.Add(cmd, "@IsVirtualItem", DbType.Boolean).Value = query.IsVirtualItem.Value;
                }
                else if (!query.IsVirtualItem.Value)
                {
                    whereClauses.Add("LocationType<>'Virtual'");
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
            if (query.IsMissing.HasValue && _config.Configuration.SchemaVersion >= 90)
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
            if (query.IsVirtualUnaired.HasValue && _config.Configuration.SchemaVersion >= 90)
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
                cmd.Parameters.Add(cmd, "@MediaTypes", DbType.String).Value = query.MediaTypes[0];
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
                    cmd.Parameters.Add(cmd, "@IncludeId" + index, DbType.Guid).Value = new Guid(id);
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
                    cmd.Parameters.Add(cmd, "@ExcludeId" + index, DbType.Guid).Value = new Guid(id);
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
                    excludeIds.Add("(COALESCE((select value from ProviderIds where ItemId=Guid and Name = '" + pair.Key + "'), '') <> " + paramName + ")");
                    cmd.Parameters.Add(cmd, paramName, DbType.String).Value = pair.Value;
                    index++;
                }

                whereClauses.Add(string.Join(" AND ", excludeIds.ToArray()));
            }

            if (query.HasImdbId.HasValue)
            {
                var fn = query.HasImdbId.Value ? "<>" : "=";
                whereClauses.Add("(COALESCE((select value from ProviderIds where ItemId=Guid and Name = 'Imdb'), '') " + fn + " '')");
            }

            if (query.HasTmdbId.HasValue)
            {
                var fn = query.HasTmdbId.Value ? "<>" : "=";
                whereClauses.Add("(COALESCE((select value from ProviderIds where ItemId=Guid and Name = 'Tmdb'), '') " + fn + " '')");
            }

            if (query.HasTvdbId.HasValue)
            {
                var fn = query.HasTvdbId.Value ? "<>" : "=";
                whereClauses.Add("(COALESCE((select value from ProviderIds where ItemId=Guid and Name = 'Tvdb'), '') " + fn + " '')");
            }

            if (query.AlbumNames.Length > 0)
            {
                var clause = "(";

                var index = 0;
                foreach (var name in query.AlbumNames)
                {
                    if (index > 0)
                    {
                        clause += " OR ";
                    }
                    clause += "Album=@AlbumName" + index;
                    cmd.Parameters.Add(cmd, "@AlbumName" + index, DbType.String).Value = name;
                    index++;
                }

                clause += ")";
                whereClauses.Add(clause);
            }

            //var enableItemsByName = query.IncludeItemsByName ?? query.IncludeItemTypes.Length > 0;
            var enableItemsByName = query.IncludeItemsByName ?? false;

            if (query.TopParentIds.Length == 1)
            {
                if (enableItemsByName)
                {
                    whereClauses.Add("(TopParentId=@TopParentId or IsItemByName=@IsItemByName)");
                    cmd.Parameters.Add(cmd, "@IsItemByName", DbType.Boolean).Value = true;
                }
                else
                {
                    whereClauses.Add("(TopParentId=@TopParentId)");
                }
                cmd.Parameters.Add(cmd, "@TopParentId", DbType.String).Value = query.TopParentIds[0];
            }
            if (query.TopParentIds.Length > 1)
            {
                var val = string.Join(",", query.TopParentIds.Select(i => "'" + i + "'").ToArray());

                if (enableItemsByName)
                {
                    whereClauses.Add("(IsItemByName=@IsItemByName or TopParentId in (" + val + "))");
                    cmd.Parameters.Add(cmd, "@IsItemByName", DbType.Boolean).Value = true;
                }
                else
                {
                    whereClauses.Add("(TopParentId in (" + val + "))");
                }
            }

            if (query.AncestorIds.Length == 1)
            {
                whereClauses.Add("Guid in (select itemId from AncestorIds where AncestorId=@AncestorId)");
                cmd.Parameters.Add(cmd, "@AncestorId", DbType.Guid).Value = new Guid(query.AncestorIds[0]);
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
                cmd.Parameters.Add(cmd, "@AncestorWithPresentationUniqueKey", DbType.String).Value = query.AncestorWithPresentationUniqueKey;
            }

            if (query.BlockUnratedItems.Length == 1)
            {
                whereClauses.Add("(InheritedParentalRatingValue > 0 or UnratedType <> @UnratedType)");
                cmd.Parameters.Add(cmd, "@UnratedType", DbType.String).Value = query.BlockUnratedItems[0].ToString();
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
                cmd.Parameters.Add(cmd, "@excludeTag" + excludeTagIndex, DbType.String).Value = "%" + excludeTag + "%";
                excludeTagIndex++;
            }

            excludeTagIndex = 0;
            foreach (var excludeTag in query.ExcludeInheritedTags)
            {
                whereClauses.Add("(InheritedTags is null OR InheritedTags not like @excludeInheritedTag" + excludeTagIndex + ")");
                cmd.Parameters.Add(cmd, "@excludeInheritedTag" + excludeTagIndex, DbType.String).Value = "%" + excludeTag + "%";
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

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select Guid,InheritedTags,(select group_concat(Tags, '|') from TypedBaseItems where (guid=outer.guid) OR (guid in (Select AncestorId from AncestorIds where ItemId=Outer.guid))) as NewInheritedTags from typedbaseitems as Outer where NewInheritedTags <> InheritedTags";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        var id = reader.GetGuid(0);
                        string value = reader.IsDBNull(2) ? null : reader.GetString(2);

                        newValues.Add(new Tuple<Guid, string>(id, value));
                    }
                }
            }

            Logger.Debug("UpdateInheritedTags - {0} rows", newValues.Count);
            if (newValues.Count == 0)
            {
                return;
            }

            await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                foreach (var item in newValues)
                {
                    _updateInheritedTagsCommand.GetParameter(0).Value = item.Item1;
                    _updateInheritedTagsCommand.GetParameter(1).Value = item.Item2;

                    _updateInheritedTagsCommand.Transaction = transaction;
                    _updateInheritedTagsCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            catch (Exception e)
            {
                Logger.ErrorException("Error running query:", e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }

                WriteLock.Release();
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

            await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                // Delete people
                _deletePeopleCommand.GetParameter(0).Value = id;
                _deletePeopleCommand.Transaction = transaction;
                _deletePeopleCommand.ExecuteNonQuery();

                // Delete chapters
                _deleteChaptersCommand.GetParameter(0).Value = id;
                _deleteChaptersCommand.Transaction = transaction;
                _deleteChaptersCommand.ExecuteNonQuery();

                // Delete media streams
                _deleteStreamsCommand.GetParameter(0).Value = id;
                _deleteStreamsCommand.Transaction = transaction;
                _deleteStreamsCommand.ExecuteNonQuery();

                // Delete ancestors
                _deleteAncestorsCommand.GetParameter(0).Value = id;
                _deleteAncestorsCommand.Transaction = transaction;
                _deleteAncestorsCommand.ExecuteNonQuery();

                // Delete user data keys
                _deleteUserDataKeysCommand.GetParameter(0).Value = id;
                _deleteUserDataKeysCommand.Transaction = transaction;
                _deleteUserDataKeysCommand.ExecuteNonQuery();

                // Delete item values
                _deleteItemValuesCommand.GetParameter(0).Value = id;
                _deleteItemValuesCommand.Transaction = transaction;
                _deleteItemValuesCommand.ExecuteNonQuery();

                // Delete provider ids
                _deleteProviderIdsCommand.GetParameter(0).Value = id;
                _deleteProviderIdsCommand.Transaction = transaction;
                _deleteProviderIdsCommand.ExecuteNonQuery();

                // Delete images
                _deleteImagesCommand.GetParameter(0).Value = id;
                _deleteImagesCommand.Transaction = transaction;
                _deleteImagesCommand.ExecuteNonQuery();

                // Delete the item
                _deleteItemCommand.GetParameter(0).Value = id;
                _deleteItemCommand.Transaction = transaction;
                _deleteItemCommand.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            catch (Exception e)
            {
                Logger.ErrorException("Failed to save children:", e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }

                WriteLock.Release();
            }
        }

        public List<string> GetPeopleNames(InternalPeopleQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select Distinct Name from People";

                var whereClauses = GetPeopleWhereClauses(query, cmd);

                if (whereClauses.Count > 0)
                {
                    cmd.CommandText += "  where " + string.Join(" AND ", whereClauses.ToArray());
                }

                cmd.CommandText += " order by ListOrder";

                var list = new List<string>();

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        list.Add(reader.GetString(0));
                    }
                }

                return list;
            }
        }

        public List<PersonInfo> GetPeople(InternalPeopleQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select ItemId, Name, Role, PersonType, SortOrder from People";

                var whereClauses = GetPeopleWhereClauses(query, cmd);

                if (whereClauses.Count > 0)
                {
                    cmd.CommandText += "  where " + string.Join(" AND ", whereClauses.ToArray());
                }

                cmd.CommandText += " order by ListOrder";

                var list = new List<PersonInfo>();

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        list.Add(GetPerson(reader));
                    }
                }

                return list;
            }
        }

        private List<string> GetPeopleWhereClauses(InternalPeopleQuery query, IDbCommand cmd)
        {
            var whereClauses = new List<string>();

            if (query.ItemId != Guid.Empty)
            {
                whereClauses.Add("ItemId=@ItemId");
                cmd.Parameters.Add(cmd, "@ItemId", DbType.Guid).Value = query.ItemId;
            }
            if (query.AppearsInItemId != Guid.Empty)
            {
                whereClauses.Add("Name in (Select Name from People where ItemId=@AppearsInItemId)");
                cmd.Parameters.Add(cmd, "@AppearsInItemId", DbType.Guid).Value = query.AppearsInItemId;
            }
            if (query.PersonTypes.Count == 1)
            {
                whereClauses.Add("PersonType=@PersonType");
                cmd.Parameters.Add(cmd, "@PersonType", DbType.String).Value = query.PersonTypes[0];
            }
            if (query.PersonTypes.Count > 1)
            {
                var val = string.Join(",", query.PersonTypes.Select(i => "'" + i + "'").ToArray());

                whereClauses.Add("PersonType in (" + val + ")");
            }
            if (query.ExcludePersonTypes.Count == 1)
            {
                whereClauses.Add("PersonType<>@PersonType");
                cmd.Parameters.Add(cmd, "@PersonType", DbType.String).Value = query.ExcludePersonTypes[0];
            }
            if (query.ExcludePersonTypes.Count > 1)
            {
                var val = string.Join(",", query.ExcludePersonTypes.Select(i => "'" + i + "'").ToArray());

                whereClauses.Add("PersonType not in (" + val + ")");
            }
            if (query.MaxListOrder.HasValue)
            {
                whereClauses.Add("ListOrder<=@MaxListOrder");
                cmd.Parameters.Add(cmd, "@MaxListOrder", DbType.Int32).Value = query.MaxListOrder.Value;
            }
            if (!string.IsNullOrWhiteSpace(query.NameContains))
            {
                whereClauses.Add("Name like @NameContains");
                cmd.Parameters.Add(cmd, "@NameContains", DbType.String).Value = "%" + query.NameContains + "%";
            }
            if (query.SourceTypes.Length == 1)
            {
                whereClauses.Add("(select sourcetype from typedbaseitems where guid=ItemId) = @SourceTypes");
                cmd.Parameters.Add(cmd, "@SourceTypes", DbType.String).Value = query.SourceTypes[0].ToString();
            }

            return whereClauses;
        }

        private void UpdateAncestors(Guid itemId, List<Guid> ancestorIds, IDbTransaction transaction)
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
            _deleteAncestorsCommand.GetParameter(0).Value = itemId;
            _deleteAncestorsCommand.Transaction = transaction;

            _deleteAncestorsCommand.ExecuteNonQuery();

            foreach (var ancestorId in ancestorIds)
            {
                _saveAncestorCommand.GetParameter(0).Value = itemId;
                _saveAncestorCommand.GetParameter(1).Value = ancestorId;
                _saveAncestorCommand.GetParameter(2).Value = ancestorId.ToString("N");

                _saveAncestorCommand.Transaction = transaction;

                _saveAncestorCommand.ExecuteNonQuery();
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

            var list = new List<string>();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "Select Value From ItemValues where " + typeClause;

                if (withItemTypes.Count > 0)
                {
                    var typeString = string.Join(",", withItemTypes.Select(i => "'" + i + "'").ToArray());
                    cmd.CommandText += " AND ItemId In (select guid from typedbaseitems where type in (" + typeString + "))";
                }
                if (excludeItemTypes.Count > 0)
                {
                    var typeString = string.Join(",", excludeItemTypes.Select(i => "'" + i + "'").ToArray());
                    cmd.CommandText += " AND ItemId not In (select guid from typedbaseitems where type in (" + typeString + "))";
                }

                cmd.CommandText += " Group By CleanValue";

                var commandBehavior = CommandBehavior.SequentialAccess | CommandBehavior.SingleResult;

                using (var reader = cmd.ExecuteReader(commandBehavior))
                {
                    LogQueryTime("GetItemValueNames", cmd, now);

                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            list.Add(reader.GetString(0));
                        }
                    }
                }

            }

            return list;
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

            var now = DateTime.UtcNow;

            var typeClause = itemValueTypes.Length == 1 ?
                ("Type=" + itemValueTypes[0].ToString(CultureInfo.InvariantCulture)) :
                ("Type in (" + string.Join(",", itemValueTypes.Select(i => i.ToString(CultureInfo.InvariantCulture)).ToArray()) + ")");

            using (var cmd = _connection.CreateCommand())
            {
                var itemCountColumns = new List<Tuple<string, string>>();

                var typesToCount = query.IncludeItemTypes.ToList();

                if (typesToCount.Count > 0)
                {
                    var itemCountColumnQuery = "select group_concat(type, '|')" + GetFromText("B");

                    var typeSubQuery = new InternalItemsQuery(query.User)
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
                    var whereClauses = GetWhereClauses(typeSubQuery, cmd, "itemTypes");

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

                cmd.CommandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, columns.ToArray(), cmd)) + GetFromText();
                cmd.CommandText += GetJoinUserDataText(query);

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

                var innerWhereClauses = GetWhereClauses(innerQuery, cmd);

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

                var outerWhereClauses = GetWhereClauses(outerQuery, cmd);

                whereText += outerWhereClauses.Count == 0 ?
                    string.Empty :
                    " AND " + string.Join(" AND ", outerWhereClauses.ToArray());
                //cmd.CommandText += GetGroupBy(query);

                cmd.CommandText += whereText;
                cmd.CommandText += " group by PresentationUniqueKey";

                cmd.Parameters.Add(cmd, "@SelectType", DbType.String).Value = returnType;

                if (EnableJoinUserData(query))
                {
                    cmd.Parameters.Add(cmd, "@UserId", DbType.Guid).Value = query.User.Id;
                }

                cmd.CommandText += " order by SortName";

                if (query.Limit.HasValue || query.StartIndex.HasValue)
                {
                    var offset = query.StartIndex ?? 0;

                    if (query.Limit.HasValue || offset > 0)
                    {
                        cmd.CommandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                    }

                    if (offset > 0)
                    {
                        cmd.CommandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                    }
                }

                cmd.CommandText += ";";

                var isReturningZeroItems = query.Limit.HasValue && query.Limit <= 0;

                if (isReturningZeroItems)
                {
                    cmd.CommandText = "";
                }

                if (query.EnableTotalRecordCount)
                {
                    cmd.CommandText += "select count (distinct PresentationUniqueKey)" + GetFromText();

                    cmd.CommandText += GetJoinUserDataText(query);
                    cmd.CommandText += whereText;
                }
                else
                {
                    cmd.CommandText = cmd.CommandText.TrimEnd(';');
                }

                var list = new List<Tuple<BaseItem, ItemCounts>>();
                var count = 0;

                var commandBehavior = isReturningZeroItems || !query.EnableTotalRecordCount
                    ? (CommandBehavior.SequentialAccess | CommandBehavior.SingleResult)
                    : CommandBehavior.SequentialAccess;

                //Logger.Debug("GetItemValues: " + cmd.CommandText);

                using (var reader = cmd.ExecuteReader(commandBehavior))
                {
                    LogQueryTime("GetItemValues", cmd, now);

                    if (isReturningZeroItems)
                    {
                        if (reader.Read())
                        {
                            count = reader.GetInt32(0);
                        }
                    }
                    else
                    {
                        while (reader.Read())
                        {
                            var item = GetItem(reader);
                            if (item != null)
                            {
                                var countStartColumn = columns.Count - 1;

                                list.Add(new Tuple<BaseItem, ItemCounts>(item, GetItemCounts(reader, countStartColumn, typesToCount)));
                            }
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            count = reader.GetInt32(0);
                        }
                    }
                }

                if (count == 0)
                {
                    count = list.Count;
                }

                return new QueryResult<Tuple<BaseItem, ItemCounts>>
                {
                    Items = list.ToArray(),
                    TotalRecordCount = count
                };

            }
        }

        private ItemCounts GetItemCounts(IDataReader reader, int countStartColumn, List<string> typesToCount)
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

        private void UpdateImages(Guid itemId, List<ItemImageInfo> images, IDbTransaction transaction)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentNullException("itemId");
            }

            if (images == null)
            {
                throw new ArgumentNullException("images");
            }

            CheckDisposed();

            // First delete 
            _deleteImagesCommand.GetParameter(0).Value = itemId;
            _deleteImagesCommand.Transaction = transaction;

            _deleteImagesCommand.ExecuteNonQuery();

            var index = 0;
            foreach (var image in images)
            {
                if (string.IsNullOrWhiteSpace(image.Path))
                {
                    // Invalid
                    continue;
                }

                _saveImagesCommand.GetParameter(0).Value = itemId;
                _saveImagesCommand.GetParameter(1).Value = image.Type;
                _saveImagesCommand.GetParameter(2).Value = image.Path;

                if (image.DateModified == default(DateTime))
                {
                    _saveImagesCommand.GetParameter(3).Value = null;
                }
                else
                {
                    _saveImagesCommand.GetParameter(3).Value = image.DateModified;
                }

                _saveImagesCommand.GetParameter(4).Value = image.IsPlaceholder;
                _saveImagesCommand.GetParameter(5).Value = index;

                _saveImagesCommand.Transaction = transaction;

                _saveImagesCommand.ExecuteNonQuery();
                index++;
            }
        }

        private void UpdateProviderIds(Guid itemId, Dictionary<string, string> values, IDbTransaction transaction)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentNullException("itemId");
            }

            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            // Just in case there might be case-insensitive duplicates, strip them out now
            var newValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in values)
            {
                newValues[pair.Key] = pair.Value;
            }

            CheckDisposed();

            // First delete 
            _deleteProviderIdsCommand.GetParameter(0).Value = itemId;
            _deleteProviderIdsCommand.Transaction = transaction;

            _deleteProviderIdsCommand.ExecuteNonQuery();

            foreach (var pair in newValues)
            {
                _saveProviderIdsCommand.GetParameter(0).Value = itemId;
                _saveProviderIdsCommand.GetParameter(1).Value = pair.Key;
                _saveProviderIdsCommand.GetParameter(2).Value = pair.Value;
                _saveProviderIdsCommand.Transaction = transaction;

                _saveProviderIdsCommand.ExecuteNonQuery();
            }
        }

        private void UpdateItemValues(Guid itemId, List<Tuple<int, string>> values, IDbTransaction transaction)
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
            _deleteItemValuesCommand.GetParameter(0).Value = itemId;
            _deleteItemValuesCommand.Transaction = transaction;

            _deleteItemValuesCommand.ExecuteNonQuery();

            foreach (var pair in values)
            {
                _saveItemValuesCommand.GetParameter(0).Value = itemId;
                _saveItemValuesCommand.GetParameter(1).Value = pair.Item1;
                _saveItemValuesCommand.GetParameter(2).Value = pair.Item2;
                if (pair.Item2 == null)
                {
                    _saveItemValuesCommand.GetParameter(3).Value = null;
                }
                else
                {
                    _saveItemValuesCommand.GetParameter(3).Value = GetCleanValue(pair.Item2);
                }
                _saveItemValuesCommand.Transaction = transaction;

                _saveItemValuesCommand.ExecuteNonQuery();
            }
        }

        private void UpdateUserDataKeys(Guid itemId, List<string> keys, IDbTransaction transaction)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentNullException("itemId");
            }

            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }

            CheckDisposed();

            // First delete 
            _deleteUserDataKeysCommand.GetParameter(0).Value = itemId;
            _deleteUserDataKeysCommand.Transaction = transaction;

            _deleteUserDataKeysCommand.ExecuteNonQuery();
            var index = 0;

            foreach (var key in keys)
            {
                _saveUserDataKeysCommand.GetParameter(0).Value = itemId;
                _saveUserDataKeysCommand.GetParameter(1).Value = key;
                _saveUserDataKeysCommand.GetParameter(2).Value = index;
                index++;
                _saveUserDataKeysCommand.Transaction = transaction;

                _saveUserDataKeysCommand.ExecuteNonQuery();
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

            var cancellationToken = CancellationToken.None;

            await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                // First delete 
                _deletePeopleCommand.GetParameter(0).Value = itemId;
                _deletePeopleCommand.Transaction = transaction;

                _deletePeopleCommand.ExecuteNonQuery();

                var listIndex = 0;

                foreach (var person in people)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _savePersonCommand.GetParameter(0).Value = itemId;
                    _savePersonCommand.GetParameter(1).Value = person.Name;
                    _savePersonCommand.GetParameter(2).Value = person.Role;
                    _savePersonCommand.GetParameter(3).Value = person.Type;
                    _savePersonCommand.GetParameter(4).Value = person.SortOrder;
                    _savePersonCommand.GetParameter(5).Value = listIndex;

                    _savePersonCommand.Transaction = transaction;

                    _savePersonCommand.ExecuteNonQuery();
                    listIndex++;
                }

                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            catch (Exception e)
            {
                Logger.ErrorException("Failed to save people:", e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }

                WriteLock.Release();
            }
        }

        private PersonInfo GetPerson(IDataReader reader)
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

            var list = new List<MediaStream>();

            using (var cmd = _connection.CreateCommand())
            {
                var cmdText = "select " + string.Join(",", _mediaStreamSaveColumns) + " from mediastreams where";

                cmdText += " ItemId=@ItemId";
                cmd.Parameters.Add(cmd, "@ItemId", DbType.Guid).Value = query.ItemId;

                if (query.Type.HasValue)
                {
                    cmdText += " AND StreamType=@StreamType";
                    cmd.Parameters.Add(cmd, "@StreamType", DbType.String).Value = query.Type.Value.ToString();
                }

                if (query.Index.HasValue)
                {
                    cmdText += " AND StreamIndex=@StreamIndex";
                    cmd.Parameters.Add(cmd, "@StreamIndex", DbType.Int32).Value = query.Index.Value;
                }

                cmdText += " order by StreamIndex ASC";

                cmd.CommandText = cmdText;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        list.Add(GetMediaStream(reader));
                    }
                }
            }

            return list;
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

            await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                // First delete chapters
                _deleteStreamsCommand.GetParameter(0).Value = id;

                _deleteStreamsCommand.Transaction = transaction;

                _deleteStreamsCommand.ExecuteNonQuery();

                foreach (var stream in streams)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var index = 0;

                    _saveStreamCommand.GetParameter(index++).Value = id;
                    _saveStreamCommand.GetParameter(index++).Value = stream.Index;
                    _saveStreamCommand.GetParameter(index++).Value = stream.Type.ToString();
                    _saveStreamCommand.GetParameter(index++).Value = stream.Codec;
                    _saveStreamCommand.GetParameter(index++).Value = stream.Language;
                    _saveStreamCommand.GetParameter(index++).Value = stream.ChannelLayout;
                    _saveStreamCommand.GetParameter(index++).Value = stream.Profile;
                    _saveStreamCommand.GetParameter(index++).Value = stream.AspectRatio;
                    _saveStreamCommand.GetParameter(index++).Value = stream.Path;

                    _saveStreamCommand.GetParameter(index++).Value = stream.IsInterlaced;

                    _saveStreamCommand.GetParameter(index++).Value = stream.BitRate;
                    _saveStreamCommand.GetParameter(index++).Value = stream.Channels;
                    _saveStreamCommand.GetParameter(index++).Value = stream.SampleRate;

                    _saveStreamCommand.GetParameter(index++).Value = stream.IsDefault;
                    _saveStreamCommand.GetParameter(index++).Value = stream.IsForced;
                    _saveStreamCommand.GetParameter(index++).Value = stream.IsExternal;

                    _saveStreamCommand.GetParameter(index++).Value = stream.Width;
                    _saveStreamCommand.GetParameter(index++).Value = stream.Height;
                    _saveStreamCommand.GetParameter(index++).Value = stream.AverageFrameRate;
                    _saveStreamCommand.GetParameter(index++).Value = stream.RealFrameRate;
                    _saveStreamCommand.GetParameter(index++).Value = stream.Level;
                    _saveStreamCommand.GetParameter(index++).Value = stream.PixelFormat;
                    _saveStreamCommand.GetParameter(index++).Value = stream.BitDepth;
                    _saveStreamCommand.GetParameter(index++).Value = stream.IsAnamorphic;
                    _saveStreamCommand.GetParameter(index++).Value = stream.RefFrames;

                    _saveStreamCommand.GetParameter(index++).Value = stream.CodecTag;
                    _saveStreamCommand.GetParameter(index++).Value = stream.Comment;
                    _saveStreamCommand.GetParameter(index++).Value = stream.NalLengthSize;
                    _saveStreamCommand.GetParameter(index++).Value = stream.IsAVC;
                    _saveStreamCommand.GetParameter(index++).Value = stream.Title;

                    _saveStreamCommand.GetParameter(index++).Value = stream.TimeBase;
                    _saveStreamCommand.GetParameter(index++).Value = stream.CodecTimeBase;

                    _saveStreamCommand.Transaction = transaction;
                    _saveStreamCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            catch (Exception e)
            {
                Logger.ErrorException("Failed to save media streams:", e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }

                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }

                WriteLock.Release();
            }
        }

        /// <summary>
        /// Gets the chapter.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>ChapterInfo.</returns>
        private MediaStream GetMediaStream(IDataReader reader)
        {
            var item = new MediaStream
            {
                Index = reader.GetInt32(1)
            };

            item.Type = (MediaStreamType)Enum.Parse(typeof(MediaStreamType), reader.GetString(2), true);

            if (!reader.IsDBNull(3))
            {
                item.Codec = reader.GetString(3);
            }

            if (!reader.IsDBNull(4))
            {
                item.Language = reader.GetString(4);
            }

            if (!reader.IsDBNull(5))
            {
                item.ChannelLayout = reader.GetString(5);
            }

            if (!reader.IsDBNull(6))
            {
                item.Profile = reader.GetString(6);
            }

            if (!reader.IsDBNull(7))
            {
                item.AspectRatio = reader.GetString(7);
            }

            if (!reader.IsDBNull(8))
            {
                item.Path = reader.GetString(8);
            }

            item.IsInterlaced = reader.GetBoolean(9);

            if (!reader.IsDBNull(10))
            {
                item.BitRate = reader.GetInt32(10);
            }

            if (!reader.IsDBNull(11))
            {
                item.Channels = reader.GetInt32(11);
            }

            if (!reader.IsDBNull(12))
            {
                item.SampleRate = reader.GetInt32(12);
            }

            item.IsDefault = reader.GetBoolean(13);
            item.IsForced = reader.GetBoolean(14);
            item.IsExternal = reader.GetBoolean(15);

            if (!reader.IsDBNull(16))
            {
                item.Width = reader.GetInt32(16);
            }

            if (!reader.IsDBNull(17))
            {
                item.Height = reader.GetInt32(17);
            }

            if (!reader.IsDBNull(18))
            {
                item.AverageFrameRate = reader.GetFloat(18);
            }

            if (!reader.IsDBNull(19))
            {
                item.RealFrameRate = reader.GetFloat(19);
            }

            if (!reader.IsDBNull(20))
            {
                item.Level = reader.GetFloat(20);
            }

            if (!reader.IsDBNull(21))
            {
                item.PixelFormat = reader.GetString(21);
            }

            if (!reader.IsDBNull(22))
            {
                item.BitDepth = reader.GetInt32(22);
            }

            if (!reader.IsDBNull(23))
            {
                item.IsAnamorphic = reader.GetBoolean(23);
            }

            if (!reader.IsDBNull(24))
            {
                item.RefFrames = reader.GetInt32(24);
            }

            if (!reader.IsDBNull(25))
            {
                item.CodecTag = reader.GetString(25);
            }

            if (!reader.IsDBNull(26))
            {
                item.Comment = reader.GetString(26);
            }

            if (!reader.IsDBNull(27))
            {
                item.NalLengthSize = reader.GetString(27);
            }

            if (!reader.IsDBNull(28))
            {
                item.IsAVC = reader.GetBoolean(28);
            }

            if (!reader.IsDBNull(29))
            {
                item.Title = reader.GetString(29);
            }

            if (!reader.IsDBNull(30))
            {
                item.TimeBase = reader.GetString(30);
            }

            if (!reader.IsDBNull(31))
            {
                item.CodecTimeBase = reader.GetString(31);
            }

            return item;
        }

    }
}