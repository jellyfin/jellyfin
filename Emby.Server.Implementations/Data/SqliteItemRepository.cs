#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using Emby.Server.Implementations.Playlists;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Json;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    /// <summary>
    /// Class SQLiteItemRepository.
    /// </summary>
    public class SqliteItemRepository : BaseSqliteRepository, IItemRepository
    {
        private const string ChaptersTableName = "Chapters2";

        private readonly IServerConfigurationManager _config;
        private readonly IServerApplicationHost _appHost;
        private readonly ILocalizationManager _localization;
        // TODO: Remove this dependency. GetImageCacheTag() is the only method used and it can be converted to a static helper method
        private readonly IImageProcessor _imageProcessor;

        private readonly TypeMapper _typeMapper;
        private readonly JsonSerializerOptions _jsonOptions;

        static SqliteItemRepository()
        {
            var queryPrefixText = new StringBuilder();
            queryPrefixText.Append("insert into mediaattachments (");
            foreach (var column in _mediaAttachmentSaveColumns)
            {
                queryPrefixText.Append(column)
                    .Append(',');
            }

            queryPrefixText.Length -= 1;
            queryPrefixText.Append(") values ");
            _mediaAttachmentInsertPrefix = queryPrefixText.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteItemRepository"/> class.
        /// </summary>
        public SqliteItemRepository(
            IServerConfigurationManager config,
            IServerApplicationHost appHost,
            ILogger<SqliteItemRepository> logger,
            ILocalizationManager localization,
            IImageProcessor imageProcessor)
            : base(logger)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _config = config;
            _appHost = appHost;
            _localization = localization;
            _imageProcessor = imageProcessor;

            _typeMapper = new TypeMapper();
            _jsonOptions = JsonDefaults.GetOptions();

            DbFilePath = Path.Combine(_config.ApplicationPaths.DataPath, "library.db");
        }

        /// <inheritdoc />
        public string Name => "SQLite";

        /// <inheritdoc />
        protected override int? CacheSize => 20000;

        /// <inheritdoc />
        protected override TempStoreMode TempStore => TempStoreMode.Memory;

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        public void Initialize(SqliteUserDataRepository userDataRepo, IUserManager userManager)
        {
            const string CreateMediaStreamsTableCommand
                    = "create table if not exists mediastreams (ItemId GUID, StreamIndex INT, StreamType TEXT, Codec TEXT, Language TEXT, ChannelLayout TEXT, Profile TEXT, AspectRatio TEXT, Path TEXT, IsInterlaced BIT, BitRate INT NULL, Channels INT NULL, SampleRate INT NULL, IsDefault BIT, IsForced BIT, IsExternal BIT, Height INT NULL, Width INT NULL, AverageFrameRate FLOAT NULL, RealFrameRate FLOAT NULL, Level FLOAT NULL, PixelFormat TEXT, BitDepth INT NULL, IsAnamorphic BIT NULL, RefFrames INT NULL, CodecTag TEXT NULL, Comment TEXT NULL, NalLengthSize TEXT NULL, IsAvc BIT NULL, Title TEXT NULL, TimeBase TEXT NULL, CodecTimeBase TEXT NULL, ColorPrimaries TEXT NULL, ColorSpace TEXT NULL, ColorTransfer TEXT NULL, PRIMARY KEY (ItemId, StreamIndex))";
            const string CreateMediaAttachmentsTableCommand
                    = "create table if not exists mediaattachments (ItemId GUID, AttachmentIndex INT, Codec TEXT, CodecTag TEXT NULL, Comment TEXT NULL, Filename TEXT NULL, MIMEType TEXT NULL, PRIMARY KEY (ItemId, AttachmentIndex))";

            string[] queries =
            {
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

                "create table if not exists " + ChaptersTableName + " (ItemId GUID, ChapterIndex INT NOT NULL, StartPositionTicks BIGINT NOT NULL, Name TEXT, ImagePath TEXT, PRIMARY KEY (ItemId, ChapterIndex))",

                CreateMediaStreamsTableCommand,
                CreateMediaAttachmentsTableCommand,

                "pragma shrink_memory"
            };


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

            using (var connection = GetConnection())
            {
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
                    AddColumn(db, "TypedBaseItems", "Size", "BIGINT", existingColumnNames);

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

                connection.RunQueries(postQueries);
            }

            userDataRepo.Initialize(userManager, WriteLock, WriteConnection);
        }

        private static readonly string[] _retriveItemColumns =
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
            "Size",
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

        private static readonly string[] _mediaStreamSaveColumns =
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

        private static readonly string[] _mediaAttachmentSaveColumns =
        {
            "ItemId",
            "AttachmentIndex",
            "Codec",
            "CodecTag",
            "Comment",
            "Filename",
            "MIMEType"
        };

        private static readonly string _mediaAttachmentInsertPrefix;

        private static string GetSaveItemCommandText()
        {
            var saveColumns = new[]
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
                "Size",
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

            var saveItemCommandCommandText = "replace into TypedBaseItems (" + string.Join(",", saveColumns) + ") values (";

            for (var i = 0; i < saveColumns.Length; i++)
            {
                if (i != 0)
                {
                    saveItemCommandCommandText += ",";
                }

                saveItemCommandCommandText += "@" + saveColumns[i];
            }

            return saveItemCommandCommandText + ")";
        }

        /// <summary>
        /// Save a standard item in the repo
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">item</exception>
        public void SaveItem(BaseItem item, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            SaveItems(new[] { item }, cancellationToken);
        }

        public void SaveImages(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            CheckDisposed();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    using (var saveImagesStatement = base.PrepareStatement(db, "Update TypedBaseItems set Images=@Images where guid=@Id"))
                    {
                        saveImagesStatement.TryBind("@Id", item.Id.ToByteArray());
                        saveImagesStatement.TryBind("@Images", SerializeImages(item));

                        saveImagesStatement.MoveNext();
                    }
                }, TransactionMode);
            }
        }

        /// <summary>
        /// Saves the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentNullException">
        /// items
        /// or
        /// cancellationToken
        /// </exception>
        public void SaveItems(IEnumerable<BaseItem> items, CancellationToken cancellationToken)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            cancellationToken.ThrowIfCancellationRequested();

            CheckDisposed();

            var tuples = new List<(BaseItem, List<Guid>, BaseItem, string, List<string>)>();
            foreach (var item in items)
            {
                var ancestorIds = item.SupportsAncestors ?
                    item.GetAncestorIds().Distinct().ToList() :
                    null;

                var topParent = item.GetTopParent();

                var userdataKey = item.GetUserDataKeys().FirstOrDefault();
                var inheritedTags = item.GetInheritedTags();

                tuples.Add((item, ancestorIds, topParent, userdataKey, inheritedTags));
            }

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    SaveItemsInTranscation(db, tuples);
                }, TransactionMode);
            }
        }

        private void SaveItemsInTranscation(IDatabaseConnection db, IEnumerable<(BaseItem, List<Guid>, BaseItem, string, List<string>)> tuples)
        {
            var statements = PrepareAll(db, new string[]
            {
                GetSaveItemCommandText(),
                "delete from AncestorIds where ItemId=@ItemId"
            }).ToList();

            using (var saveItemStatement = statements[0])
            using (var deleteAncestorsStatement = statements[1])
            {
                var requiresReset = false;
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
            Type type = item.GetType();

            saveItemStatement.TryBind("@guid", item.Id);
            saveItemStatement.TryBind("@type", type.FullName);

            if (TypeRequiresDeserialization(type))
            {
                saveItemStatement.TryBind("@data", JsonSerializer.SerializeToUtf8Bytes(item, type, _jsonOptions));
            }
            else
            {
                saveItemStatement.TryBindNull("@data");
            }

            saveItemStatement.TryBind("@Path", GetPathToSave(item.Path));

            if (item is IHasStartDate hasStartDate)
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

            saveItemStatement.TryBind("@ChannelId", item.ChannelId.Equals(Guid.Empty) ? null : item.ChannelId.ToString("N", CultureInfo.InvariantCulture));

            if (item is IHasProgramAttributes hasProgramAttributes)
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
            saveItemStatement.TryBind("@Size", item.Size);

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
                saveItemStatement.TryBind("@LockedFields", string.Join("|", item.LockedFields));
            }
            else
            {
                saveItemStatement.TryBindNull("@LockedFields");
            }

            if (item.Studios.Length > 0)
            {
                saveItemStatement.TryBind("@Studios", string.Join("|", item.Studios));
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

            if (item is LiveTvChannel liveTvChannel)
            {
                saveItemStatement.TryBind("@ExternalServiceId", liveTvChannel.ServiceName);
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

            if (topParent == null)
            {
                saveItemStatement.TryBindNull("@TopParentId");
            }
            else
            {
                saveItemStatement.TryBind("@TopParentId", topParent.Id.ToString("N", CultureInfo.InvariantCulture));
            }

            if (item is Trailer trailer && trailer.TrailerTypes.Length > 0)
            {
                saveItemStatement.TryBind("@TrailerTypes", string.Join("|", trailer.TrailerTypes));
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

            if (item is Video video)
            {
                saveItemStatement.TryBind("@PrimaryVersionId", video.PrimaryVersionId);
            }
            else
            {
                saveItemStatement.TryBindNull("@PrimaryVersionId");
            }

            if (item is Folder folder && folder.DateLastMediaAdded.HasValue)
            {
                saveItemStatement.TryBind("@DateLastMediaAdded", folder.DateLastMediaAdded.Value);
            }
            else
            {
                saveItemStatement.TryBindNull("@DateLastMediaAdded");
            }

            saveItemStatement.TryBind("@Album", item.Album);
            saveItemStatement.TryBind("@IsVirtualItem", item.IsVirtualItem);

            if (item is IHasSeries hasSeriesName)
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

            if (item is Episode episode)
            {
                saveItemStatement.TryBind("@SeasonName", episode.SeasonName);

                var nullableSeasonId = episode.SeasonId == Guid.Empty ? (Guid?)null : episode.SeasonId;

                saveItemStatement.TryBind("@SeasonId", nullableSeasonId);
            }
            else
            {
                saveItemStatement.TryBindNull("@SeasonName");
                saveItemStatement.TryBindNull("@SeasonId");
            }

            if (item is IHasSeries hasSeries)
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
                saveItemStatement.TryBind("@ExtraIds", string.Join("|", item.ExtraIds));
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
            if (item is IHasArtist hasArtists && hasArtists.Artists.Count > 0)
            {
                artists = string.Join("|", hasArtists.Artists);
            }
            saveItemStatement.TryBind("@Artists", artists);

            string albumArtists = null;
            if (item is IHasAlbumArtist hasAlbumArtists
                && hasAlbumArtists.AlbumArtists.Count > 0)
            {
                albumArtists = string.Join("|", hasAlbumArtists.AlbumArtists);
            }

            saveItemStatement.TryBind("@AlbumArtists", albumArtists);
            saveItemStatement.TryBind("@ExternalId", item.ExternalId);

            if (item is LiveTvProgram program)
            {
                saveItemStatement.TryBind("@ShowId", program.ShowId);
            }
            else
            {
                saveItemStatement.TryBindNull("@ShowId");
            }

            Guid ownerId = item.OwnerId;
            if (ownerId == Guid.Empty)
            {
                saveItemStatement.TryBindNull("@OwnerId");
            }
            else
            {
                saveItemStatement.TryBind("@OwnerId", ownerId);
            }

            saveItemStatement.MoveNext();
        }

        private static string SerializeProviderIds(BaseItem item)
        {
            StringBuilder str = new StringBuilder();
            foreach (var i in item.ProviderIds)
            {
                // Ideally we shouldn't need this IsNullOrWhiteSpace check,
                // but we're seeing some cases of bad data slip through
                if (string.IsNullOrWhiteSpace(i.Value))
                {
                    continue;
                }

                str.Append($"{i.Key}={i.Value}|");
            }

            if (str.Length == 0)
            {
                return null;
            }

            str.Length -= 1; // Remove last |
            return str.ToString();
        }

        private static void DeserializeProviderIds(string value, BaseItem item)
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

            StringBuilder str = new StringBuilder();
            foreach (var i in images)
            {
                if (string.IsNullOrWhiteSpace(i.Path))
                {
                    continue;
                }
                str.Append(ToValueString(i) + "|");
            }

            str.Length -= 1; // Remove last |
            return str.ToString();
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
            const string Delimeter = "*";

            var path = image.Path ?? string.Empty;
            var hash = image.BlurHash ?? string.Empty;

            return GetPathToSave(path) +
                   Delimeter +
                   image.DateModified.Ticks.ToString(CultureInfo.InvariantCulture) +
                   Delimeter +
                   image.Type +
                   Delimeter +
                   image.Width.ToString(CultureInfo.InvariantCulture) +
                   Delimeter +
                   image.Height.ToString(CultureInfo.InvariantCulture) +
                   Delimeter +
                   // Replace delimiters with other characters.
                   // This can be removed when we migrate to a proper DB.
                   hash.Replace('*', '/').Replace('|', '\\');
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

            if (long.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var ticks))
            {
                image.DateModified = new DateTime(ticks, DateTimeKind.Utc);
            }

            if (Enum.TryParse(parts[2], true, out ImageType type))
            {
                image.Type = type;
            }

            if (parts.Length >= 5)
            {
                if (int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var width)
                    && int.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var height))
                {
                    image.Width = width;
                    image.Height = height;
                }

                if (parts.Length >= 6)
                {
                    image.BlurHash = parts[5].Replace('/', '*').Replace('\\', '|');
                }
            }

            return image;
        }

        /// <summary>
        /// Internal retrieve from items or users table
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        /// <exception cref="ArgumentException"></exception>
        public BaseItem RetrieveItem(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Guid can't be empty", nameof(id));
            }

            CheckDisposed();

            using (var connection = GetConnection(true))
            {
                using (var statement = PrepareStatement(connection, "select " + string.Join(",", _retriveItemColumns) + " from TypedBaseItems where guid = @guid"))
                {
                    statement.TryBind("@guid", id);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        return GetItem(row, new InternalItemsQuery());
                    }
                }
            }

            return null;
        }

        private bool TypeRequiresDeserialization(Type type)
        {
            if (_config.Configuration.SkipDeserializationForBasicTypes)
            {
                if (type == typeof(Channel))
                {
                    return false;
                }
                else if (type == typeof(UserRootFolder))
                {
                    return false;
                }
            }

            if (type == typeof(Season))
            {
                return false;
            }
            else if (type == typeof(MusicArtist))
            {
                return false;
            }
            else if (type == typeof(Person))
            {
                return false;
            }
            else if (type == typeof(MusicGenre))
            {
                return false;
            }
            else if (type == typeof(Genre))
            {
                return false;
            }
            else if (type == typeof(Studio))
            {
                return false;
            }
            else if (type == typeof(PlaylistsFolder))
            {
                return false;
            }
            else if (type == typeof(PhotoAlbum))
            {
                return false;
            }
            else if (type == typeof(Year))
            {
                return false;
            }
            else if (type == typeof(Book))
            {
                return false;
            }
            else if (type == typeof(LiveTvProgram))
            {
                return false;
            }
            else if (type == typeof(AudioBook))
            {
                return false;
            }
            else if (type == typeof(Audio))
            {
                return false;
            }
            else if (type == typeof(MusicAlbum))
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
                return null;
            }

            BaseItem item = null;

            if (TypeRequiresDeserialization(type))
            {
                try
                {
                    item = JsonSerializer.Deserialize(reader[1].ToBlob(), type, _jsonOptions) as BaseItem;
                }
                catch (JsonException ex)
                {
                    Logger.LogError(ex, "Error deserializing item with JSON: {Data}", reader.GetString(1));
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
                    if (item is IHasStartDate hasStartDate)
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
                if (item is IHasProgramAttributes hasProgramAttributes)
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

            if (!reader.IsDBNull(index))
            {
                item.Size = reader.GetInt64(index);
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
                if (Enum.TryParse(reader.GetString(index), true, out ProgramAudio audio))
                {
                    item.Audio = audio;
                }
            }
            index++;

            // TODO: Even if not needed by apps, the server needs it internally
            // But get this excluded from contexts where it is not needed
            if (hasServiceName)
            {
                if (item is LiveTvChannel liveTvChannel)
                {
                    if (!reader.IsDBNull(index))
                    {
                        liveTvChannel.ServiceName = reader.GetString(index);
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
                    IEnumerable<MetadataFields> GetLockedFields(string s)
                    {
                        foreach (var i in s.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (Enum.TryParse(i, true, out MetadataFields parsedValue))
                            {
                                yield return parsedValue;
                            }
                        }
                    }
                    item.LockedFields = GetLockedFields(reader.GetString(index)).ToArray();
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
                if (item is Trailer trailer)
                {
                    if (!reader.IsDBNull(index))
                    {
                        IEnumerable<TrailerType> GetTrailerTypes(string s)
                        {
                            foreach (var i in s.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                if (Enum.TryParse(i, true, out TrailerType parsedValue))
                                {
                                    yield return parsedValue;
                                }
                            }
                        }
                        trailer.TrailerTypes = GetTrailerTypes(reader.GetString(index)).ToArray();
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

            if (item is Video video)
            {
                if (!reader.IsDBNull(index))
                {
                    video.PrimaryVersionId = reader.GetString(index);
                }
            }
            index++;

            if (HasField(query, ItemFields.DateLastMediaAdded))
            {
                if (item is Folder folder && !reader.IsDBNull(index))
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

            if (item is IHasSeries hasSeriesName)
            {
                if (!reader.IsDBNull(index))
                {
                    hasSeriesName.SeriesName = reader.GetString(index);
                }
            }
            index++;

            if (hasEpisodeAttributes)
            {
                if (item is Episode episode)
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
                    item.ProductionLocations = reader.GetString(index).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
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
                if (Enum.TryParse(reader.GetString(index), true, out ExtraType extraType))
                {
                    item.ExtraType = extraType;
                }
            }
            index++;

            if (hasArtistFields)
            {
                if (item is IHasArtist hasArtists && !reader.IsDBNull(index))
                {
                    hasArtists.Artists = reader.GetString(index).Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                }
                index++;

                if (item is IHasAlbumArtist hasAlbumArtists && !reader.IsDBNull(index))
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
                if (item is LiveTvProgram program && !reader.IsDBNull(index))
                {
                    program.ShowId = reader.GetString(index);
                }
                index++;
            }

            if (!reader.IsDBNull(index))
            {
                item.OwnerId = reader.GetGuid(index);
            }
            index++;

            return item;
        }

        private static Guid[] SplitToGuids(string value)
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
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{ChapterInfo}.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        public List<ChapterInfo> GetChapters(BaseItem item)
        {
            CheckDisposed();

            using (var connection = GetConnection(true))
            {
                var chapters = new List<ChapterInfo>();

                using (var statement = PrepareStatement(connection, "select StartPositionTicks,Name,ImagePath,ImageDateModified from " + ChaptersTableName + " where ItemId = @ItemId order by ChapterIndex asc"))
                {
                    statement.TryBind("@ItemId", item.Id);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        chapters.Add(GetChapter(row, item));
                    }
                }

                return chapters;
            }
        }

        /// <summary>
        /// Gets a single chapter for an item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="index">The index.</param>
        /// <returns>ChapterInfo.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        public ChapterInfo GetChapter(BaseItem item, int index)
        {
            CheckDisposed();

            using (var connection = GetConnection(true))
            {
                using (var statement = PrepareStatement(connection, "select StartPositionTicks,Name,ImagePath,ImageDateModified from " + ChaptersTableName + " where ItemId = @ItemId and ChapterIndex=@ChapterIndex"))
                {
                    statement.TryBind("@ItemId", item.Id);
                    statement.TryBind("@ChapterIndex", index);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        return GetChapter(row, item);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the chapter.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="item">The item.</param>
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
                    try
                    {
                        chapter.ImageTag = _imageProcessor.GetImageCacheTag(item, chapter);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to create image cache tag.");
                    }
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
        public void SaveChapters(Guid id, IReadOnlyList<ChapterInfo> chapters)
        {
            CheckDisposed();

            if (id.Equals(Guid.Empty))
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (chapters == null)
            {
                throw new ArgumentNullException(nameof(chapters));
            }

            var idBlob = id.ToByteArray();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    // First delete chapters
                    db.Execute("delete from " + ChaptersTableName + " where ItemId=@ItemId", idBlob);

                    InsertChapters(idBlob, chapters, db);

                }, TransactionMode);
            }
        }

        private void InsertChapters(byte[] idBlob, IReadOnlyList<ChapterInfo> chapters, IDatabaseConnection db)
        {
            var startIndex = 0;
            var limit = 100;
            var chapterIndex = 0;

            const string StartInsertText = "insert into " + ChaptersTableName + " (ItemId, ChapterIndex, StartPositionTicks, Name, ImagePath, ImageDateModified) values ";
            var insertText = new StringBuilder(StartInsertText, 256);

            while (startIndex < chapters.Count)
            {
                var endIndex = Math.Min(chapters.Count, startIndex + limit);

                for (var i = startIndex; i < endIndex; i++)
                {
                    insertText.AppendFormat("(@ItemId, @ChapterIndex{0}, @StartPositionTicks{0}, @Name{0}, @ImagePath{0}, @ImageDateModified{0}),", i.ToString(CultureInfo.InvariantCulture));
                }

                insertText.Length -= 1; // Remove last ,

                using (var statement = PrepareStatement(db, insertText.ToString()))
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
                insertText.Length = StartInsertText.Length;
            }
        }

        private static bool EnableJoinUserData(InternalItemsQuery query)
        {
            if (query.User == null)
            {
                return false;
            }

            var sortingFields = new HashSet<string>(query.OrderBy.Select(i => i.Item1), StringComparer.OrdinalIgnoreCase);

            return sortingFields.Contains(ItemSortBy.IsFavoriteOrLiked)
                    || sortingFields.Contains(ItemSortBy.IsPlayed)
                    || sortingFields.Contains(ItemSortBy.IsUnplayed)
                    || sortingFields.Contains(ItemSortBy.PlayCount)
                    || sortingFields.Contains(ItemSortBy.DatePlayed)
                    || sortingFields.Contains(ItemSortBy.SeriesDatePlayed)
                    || query.IsFavoriteOrLiked.HasValue
                    || query.IsFavorite.HasValue
                    || query.IsResumable.HasValue
                    || query.IsPlayed.HasValue
                    || query.IsLiked.HasValue;
        }

        private readonly ItemFields[] _allFields = Enum.GetNames(typeof(ItemFields))
            .Select(i => (ItemFields)Enum.Parse(typeof(ItemFields), i, true))
            .ToArray();

        private string[] GetColumnNamesFromField(ItemFields field)
        {
            switch (field)
            {
                case ItemFields.Settings:
                    return new[] { "IsLocked", "PreferredMetadataCountryCode", "PreferredMetadataLanguage", "LockedFields" };
                case ItemFields.ServiceName:
                    return new[] { "ExternalServiceId" };
                case ItemFields.SortName:
                    return new[] { "ForcedSortName" };
                case ItemFields.Taglines:
                    return new[] { "Tagline" };
                case ItemFields.Tags:
                    return new[] { "Tags" };
                case ItemFields.IsHD:
                    return Array.Empty<string>();
                default:
                    return new[] { field.ToString() };
            }
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

        private static readonly HashSet<string> _programExcludeParentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Series",
            "Season",
            "MusicAlbum",
            "MusicArtist",
            "PhotoAlbum"
        };

        private static readonly HashSet<string> _programTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Program",
            "TvChannel",
            "LiveTvProgram",
            "LiveTvTvChannel"
        };

        private bool HasProgramAttributes(InternalItemsQuery query)
        {
            if (_programExcludeParentTypes.Contains(query.ParentType))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Any(x => _programTypes.Contains(x));
        }

        private static readonly HashSet<string> _serviceTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "TvChannel",
            "LiveTvTvChannel"
        };

        private bool HasServiceName(InternalItemsQuery query)
        {
            if (_programExcludeParentTypes.Contains(query.ParentType))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Any(x => _serviceTypes.Contains(x));
        }

        private static readonly HashSet<string> _startDateTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Program",
            "LiveTvProgram"
        };

        private bool HasStartDate(InternalItemsQuery query)
        {
            if (_programExcludeParentTypes.Contains(query.ParentType))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Any(x => _startDateTypes.Contains(x));
        }

        private bool HasEpisodeAttributes(InternalItemsQuery query)
        {
            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Contains("Episode", StringComparer.OrdinalIgnoreCase);
        }

        private bool HasTrailerTypes(InternalItemsQuery query)
        {
            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Contains("Trailer", StringComparer.OrdinalIgnoreCase);
        }


        private static readonly HashSet<string> _artistExcludeParentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Series",
            "Season",
            "PhotoAlbum"
        };

        private static readonly HashSet<string> _artistsTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Audio",
            "MusicAlbum",
            "MusicVideo",
            "AudioBook",
            "AudioPodcast"
        };

        private bool HasArtistFields(InternalItemsQuery query)
        {
            if (_artistExcludeParentTypes.Contains(query.ParentType))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Any(x => _artistsTypes.Contains(x));
        }

        private static readonly HashSet<string> _seriesTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Book",
            "AudioBook",
            "Episode",
            "Season"
        };

        private bool HasSeriesFields(InternalItemsQuery query)
        {
            if (string.Equals(query.ParentType, "PhotoAlbum", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (query.IncludeItemTypes.Length == 0)
            {
                return true;
            }

            return query.IncludeItemTypes.Any(x => _seriesTypes.Contains(x));
        }

        private List<string> GetFinalColumnsToSelect(InternalItemsQuery query, IEnumerable<string> startColumns)
        {
            var list = startColumns.ToList();

            foreach (var field in _allFields)
            {
                if (!HasField(query, field))
                {
                    foreach (var fieldToRemove in GetColumnNamesFromField(field))
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
                    builder.Append("+(Select Case When Abs(COALESCE(ProductionYear, 0) - @ItemProductionYear) < 10 Then 10 Else 0 End )");
                    builder.Append("+(Select Case When Abs(COALESCE(ProductionYear, 0) - @ItemProductionYear) < 5 Then 5 Else 0 End )");
                }

                //// genres, tags
                builder.Append("+ ((Select count(CleanValue) from ItemValues where ItemId=Guid and CleanValue in (select CleanValue from itemvalues where ItemId=@SimilarItemId)) * 10)");

                builder.Append(") as SimilarityScore");

                list.Add(builder.ToString());

                var oldLen = query.ExcludeItemIds.Length;
                var newLen = oldLen + item.ExtraIds.Length + 1;
                var excludeIds = new Guid[newLen];
                query.ExcludeItemIds.CopyTo(excludeIds, 0);
                excludeIds[oldLen] = item.Id;
                item.ExtraIds.CopyTo(excludeIds, oldLen + 1);

                query.ExcludeItemIds = excludeIds;
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

            return list;
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
                return " Group by " + string.Join(",", groups);
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
                throw new ArgumentNullException(nameof(query));
            }

            CheckDisposed();

            var now = DateTime.UtcNow;

            // Hack for right now since we currently don't support filtering out these duplicates within a query
            if (query.Limit.HasValue && query.EnableGroupByMetadataKey)
            {
                query.Limit = query.Limit.Value + 4;
            }

            var commandText = "select "
                            + string.Join(",", GetFinalColumnsToSelect(query, new[] { "count(distinct PresentationUniqueKey)" }))
                            + GetFromText()
                            + GetJoinUserDataText(query);

            var whereClauses = GetWhereClauses(query, null);
            if (whereClauses.Count != 0)
            {
                commandText += " where " + string.Join(" AND ", whereClauses);
            }

            int count;
            using (var connection = GetConnection(true))
            {
                using (var statement = PrepareStatement(connection, commandText))
                {
                    if (EnableJoinUserData(query))
                    {
                        statement.TryBind("@UserId", query.User.InternalId);
                    }

                    BindSimilarParams(query, statement);
                    BindSearchParams(query, statement);

                    // Running this again will bind the params
                    GetWhereClauses(query, statement);

                    count = statement.ExecuteQuery().SelectScalarInt().First();
                }
            }

            LogQueryTime("GetCount", commandText, now);
            return count;
        }

        public List<BaseItem> GetItemList(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            CheckDisposed();

            var now = DateTime.UtcNow;

            // Hack for right now since we currently don't support filtering out these duplicates within a query
            if (query.Limit.HasValue && query.EnableGroupByMetadataKey)
            {
                query.Limit = query.Limit.Value + 4;
            }

            var commandText = "select "
                            + string.Join(",", GetFinalColumnsToSelect(query, _retriveItemColumns))
                            + GetFromText()
                            + GetJoinUserDataText(query);

            var whereClauses = GetWhereClauses(query, null);

            if (whereClauses.Count != 0)
            {
                commandText += " where " + string.Join(" AND ", whereClauses);
            }

            commandText += GetGroupBy(query)
                        + GetOrderByText(query);

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

            var items = new List<BaseItem>();
            using (var connection = GetConnection(true))
            {
                using (var statement = PrepareStatement(connection, commandText))
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
                            items.Add(item);
                        }
                    }
                }

                // Hack for right now since we currently don't support filtering out these duplicates within a query
                if (query.EnableGroupByMetadataKey)
                {
                    var limit = query.Limit ?? int.MaxValue;
                    limit -= 4;
                    var newList = new List<BaseItem>();

                    foreach (var item in items)
                    {
                        AddItem(newList, item);

                        if (newList.Count >= limit)
                        {
                            break;
                        }
                    }

                    items = newList;
                }
            }

            LogQueryTime("GetItemList", commandText, now);

            return items;
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
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                foreach (var providerId in newItem.ProviderIds)
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

#if DEBUG
            const int SlowThreshold = 100;
#else
            const int SlowThreshold = 10;
#endif

            if (elapsed >= SlowThreshold)
            {
                Logger.LogDebug(
                    "{Method} query time (slow): {ElapsedMs}ms. Query: {Query}",
                    methodName,
                    elapsed,
                    commandText);
            }
        }

        public QueryResult<BaseItem> GetItems(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            CheckDisposed();

            if (!query.EnableTotalRecordCount || (!query.Limit.HasValue && (query.StartIndex ?? 0) == 0))
            {
                var returnList = GetItemList(query);
                return new QueryResult<BaseItem>
                {
                    Items = returnList,
                    TotalRecordCount = returnList.Count
                };
            }

            var now = DateTime.UtcNow;

            // Hack for right now since we currently don't support filtering out these duplicates within a query
            if (query.Limit.HasValue && query.EnableGroupByMetadataKey)
            {
                query.Limit = query.Limit.Value + 4;
            }

            var commandText = "select "
                            + string.Join(",", GetFinalColumnsToSelect(query, _retriveItemColumns))
                            + GetFromText()
                            + GetJoinUserDataText(query);

            var whereClauses = GetWhereClauses(query, null);

            var whereText = whereClauses.Count == 0 ?
                string.Empty :
                " where " + string.Join(" AND ", whereClauses);

            commandText += whereText
                        + GetGroupBy(query)
                        + GetOrderByText(query);

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

                commandText += GetJoinUserDataText(query)
                            + whereText;
                statementTexts.Add(commandText);
            }

            var list = new List<BaseItem>();
            var result = new QueryResult<BaseItem>();
            using (var connection = GetConnection(true))
            {
                connection.RunInTransaction(db =>
                {

                    var statements = PrepareAll(db, statementTexts).ToList();

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
                }, ReadTransactionMode);
            }

            LogQueryTime("GetItems", commandText, now);
            result.Items = list;
            return result;
        }

        private string GetOrderByText(InternalItemsQuery query)
        {
            var orderBy = query.OrderBy;
            bool hasSimilar = query.SimilarTo != null;
            bool hasSearch = !string.IsNullOrEmpty(query.SearchTerm);

            if (hasSimilar || hasSearch)
            {
                List<(string, SortOrder)> prepend = new List<(string, SortOrder)>(4);
                if (hasSearch)
                {
                    prepend.Add(("SearchScore", SortOrder.Descending));
                    prepend.Add((ItemSortBy.SortName, SortOrder.Ascending));
                }

                if (hasSimilar)
                {
                    prepend.Add(("SimilarityScore", SortOrder.Descending));
                    prepend.Add((ItemSortBy.Random, SortOrder.Ascending));
                }

                var arr = new (string, SortOrder)[prepend.Count + orderBy.Count];
                prepend.CopyTo(arr, 0);
                orderBy.CopyTo(arr, prepend.Count);
                orderBy = query.OrderBy = arr;
            }
            else if (orderBy.Count == 0)
            {
                return string.Empty;
            }

            return " ORDER BY " + string.Join(",", orderBy.Select(i =>
            {
                var columnMap = MapOrderByField(i.Item1, query);

                var sortOrder = i.Item2 == SortOrder.Ascending ? "ASC" : "DESC";

                return columnMap.Item1 + " " + sortOrder;
            }));
        }

        private (string, bool) MapOrderByField(string name, InternalItemsQuery query)
        {
            if (string.Equals(name, ItemSortBy.AirTime, StringComparison.OrdinalIgnoreCase))
            {
                // TODO
                return ("SortName", false);
            }
            else if (string.Equals(name, ItemSortBy.Runtime, StringComparison.OrdinalIgnoreCase))
            {
                return ("RuntimeTicks", false);
            }
            else if (string.Equals(name, ItemSortBy.Random, StringComparison.OrdinalIgnoreCase))
            {
                return ("RANDOM()", false);
            }
            else if (string.Equals(name, ItemSortBy.DatePlayed, StringComparison.OrdinalIgnoreCase))
            {
                if (query.GroupBySeriesPresentationUniqueKey)
                {
                    return ("MAX(LastPlayedDate)", false);
                }

                return ("LastPlayedDate", false);
            }
            else if (string.Equals(name, ItemSortBy.PlayCount, StringComparison.OrdinalIgnoreCase))
            {
                return ("PlayCount", false);
            }
            else if (string.Equals(name, ItemSortBy.IsFavoriteOrLiked, StringComparison.OrdinalIgnoreCase))
            {
                return ("(Select Case When IsFavorite is null Then 0 Else IsFavorite End )", true);
            }
            else if (string.Equals(name, ItemSortBy.IsFolder, StringComparison.OrdinalIgnoreCase))
            {
                return ("IsFolder", true);
            }
            else if (string.Equals(name, ItemSortBy.IsPlayed, StringComparison.OrdinalIgnoreCase))
            {
                return ("played", true);
            }
            else if (string.Equals(name, ItemSortBy.IsUnplayed, StringComparison.OrdinalIgnoreCase))
            {
                return ("played", false);
            }
            else if (string.Equals(name, ItemSortBy.DateLastContentAdded, StringComparison.OrdinalIgnoreCase))
            {
                return ("DateLastMediaAdded", false);
            }
            else if (string.Equals(name, ItemSortBy.Artist, StringComparison.OrdinalIgnoreCase))
            {
                return ("(select CleanValue from itemvalues where ItemId=Guid and Type=0 LIMIT 1)", false);
            }
            else if (string.Equals(name, ItemSortBy.AlbumArtist, StringComparison.OrdinalIgnoreCase))
            {
                return ("(select CleanValue from itemvalues where ItemId=Guid and Type=1 LIMIT 1)", false);
            }
            else if (string.Equals(name, ItemSortBy.OfficialRating, StringComparison.OrdinalIgnoreCase))
            {
                return ("InheritedParentalRatingValue", false);
            }
            else if (string.Equals(name, ItemSortBy.Studio, StringComparison.OrdinalIgnoreCase))
            {
                return ("(select CleanValue from itemvalues where ItemId=Guid and Type=3 LIMIT 1)", false);
            }
            else if (string.Equals(name, ItemSortBy.SeriesDatePlayed, StringComparison.OrdinalIgnoreCase))
            {
                return ("(Select MAX(LastPlayedDate) from TypedBaseItems B" + GetJoinUserDataText(query) + " where Played=1 and B.SeriesPresentationUniqueKey=A.PresentationUniqueKey)", false);
            }
            else if (string.Equals(name, ItemSortBy.SeriesSortName, StringComparison.OrdinalIgnoreCase))
            {
                return ("SeriesName", false);
            }

            return (name, false);
        }

        public List<Guid> GetItemIdsList(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            CheckDisposed();

            var now = DateTime.UtcNow;

            var commandText = "select "
                            + string.Join(",", GetFinalColumnsToSelect(query, new[] { "guid" }))
                            + GetFromText()
                            + GetJoinUserDataText(query);

            var whereClauses = GetWhereClauses(query, null);
            if (whereClauses.Count != 0)
            {
                commandText += " where " + string.Join(" AND ", whereClauses);
            }

            commandText += GetGroupBy(query)
                        + GetOrderByText(query);

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
            using (var connection = GetConnection(true))
            {
                using (var statement = PrepareStatement(connection, commandText))
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

            LogQueryTime("GetItemList", commandText, now);
            return list;
        }

        public List<Tuple<Guid, string>> GetItemIdsWithPath(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            CheckDisposed();

            var now = DateTime.UtcNow;

            var commandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "guid", "path" })) + GetFromText();

            var whereClauses = GetWhereClauses(query, null);
            if (whereClauses.Count != 0)
            {
                commandText += " where " + string.Join(" AND ", whereClauses);
            }

            commandText += GetGroupBy(query)
                        + GetOrderByText(query);

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
            using (var connection = GetConnection(true))
            {
                using (var statement = PrepareStatement(connection, commandText))
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

        public QueryResult<Guid> GetItemIds(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            CheckDisposed();

            if (!query.EnableTotalRecordCount || (!query.Limit.HasValue && (query.StartIndex ?? 0) == 0))
            {
                var returnList = GetItemIdsList(query);
                return new QueryResult<Guid>
                {
                    Items = returnList,
                    TotalRecordCount = returnList.Count
                };
            }

            var now = DateTime.UtcNow;

            var commandText = "select "
                            + string.Join(",", GetFinalColumnsToSelect(query, new[] { "guid" }))
                            + GetFromText()
                            + GetJoinUserDataText(query);

            var whereClauses = GetWhereClauses(query, null);

            var whereText = whereClauses.Count == 0 ?
                string.Empty :
                " where " + string.Join(" AND ", whereClauses);

            commandText += whereText
                        + GetGroupBy(query)
                        + GetOrderByText(query);

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

                commandText += GetJoinUserDataText(query)
                            + whereText;
                statementTexts.Add(commandText);
            }

            var list = new List<Guid>();
            var result = new QueryResult<Guid>();
            using (var connection = GetConnection(true))
            {
                connection.RunInTransaction(db =>
                {
                    var statements = PrepareAll(db, statementTexts).ToList();

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
                }, ReadTransactionMode);
            }

            LogQueryTime("GetItemIds", commandText, now);

            result.Items = list;
            return result;
        }

        private bool IsAlphaNumeric(string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }

            for (int i = 0; i < str.Length; i++)
            {
                if (!char.IsLetter(str[i]) && !char.IsNumber(str[i]))
                {
                    return false;
                }
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

        private bool IsValidPersonType(string value)
        {
            return IsAlphaNumeric(value);
        }

        private List<string> GetWhereClauses(InternalItemsQuery query, IStatement statement)
        {
            if (query.IsResumable ?? false)
            {
                query.IsVirtualItem = false;
            }

            var minWidth = query.MinWidth;
            var maxWidth = query.MaxWidth;

            if (query.IsHD.HasValue)
            {
                const int Threshold = 1200;
                if (query.IsHD.Value)
                {
                    minWidth = Threshold;
                }
                else
                {
                    maxWidth = Threshold - 1;
                }
            }

            if (query.Is4K.HasValue)
            {
                const int Threshold = 3800;
                if (query.Is4K.Value)
                {
                    minWidth = Threshold;
                }
                else
                {
                    maxWidth = Threshold - 1;
                }
            }

            var whereClauses = new List<string>();

            if (minWidth.HasValue)
            {
                whereClauses.Add("Width>=@MinWidth");
                statement?.TryBind("@MinWidth", minWidth);
            }

            if (query.MinHeight.HasValue)
            {
                whereClauses.Add("Height>=@MinHeight");
                statement?.TryBind("@MinHeight", query.MinHeight);
            }

            if (maxWidth.HasValue)
            {
                whereClauses.Add("Width<=@MaxWidth");
                statement?.TryBind("@MaxWidth", maxWidth);
            }

            if (query.MaxHeight.HasValue)
            {
                whereClauses.Add("Height<=@MaxHeight");
                statement?.TryBind("@MaxHeight", query.MaxHeight);
            }

            if (query.IsLocked.HasValue)
            {
                whereClauses.Add("IsLocked=@IsLocked");
                statement?.TryBind("@IsLocked", query.IsLocked);
            }

            var tags = query.Tags.ToList();
            var excludeTags = query.ExcludeTags.ToList();

            if (query.IsMovie == true)
            {
                if (query.IncludeItemTypes.Length == 0
                    || query.IncludeItemTypes.Contains(nameof(Movie))
                    || query.IncludeItemTypes.Contains(nameof(Trailer)))
                {
                    whereClauses.Add("(IsMovie is null OR IsMovie=@IsMovie)");
                }
                else
                {
                    whereClauses.Add("IsMovie=@IsMovie");
                }

                statement?.TryBind("@IsMovie", true);
            }
            else if (query.IsMovie.HasValue)
            {
                whereClauses.Add("IsMovie=@IsMovie");
                statement?.TryBind("@IsMovie", query.IsMovie);
            }

            if (query.IsSeries.HasValue)
            {
                whereClauses.Add("IsSeries=@IsSeries");
                statement?.TryBind("@IsSeries", query.IsSeries);
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
                statement?.TryBind("@IsFolder", query.IsFolder);
            }

            var includeTypes = query.IncludeItemTypes.SelectMany(MapIncludeItemTypes).ToArray();
            // Only specify excluded types if no included types are specified
            if (includeTypes.Length == 0)
            {
                var excludeTypes = query.ExcludeItemTypes.SelectMany(MapIncludeItemTypes).ToArray();
                if (excludeTypes.Length == 1)
                {
                    whereClauses.Add("type<>@type");
                    statement?.TryBind("@type", excludeTypes[0]);
                }
                else if (excludeTypes.Length > 1)
                {
                    var inClause = string.Join(",", excludeTypes.Select(i => "'" + i + "'"));
                    whereClauses.Add($"type not in ({inClause})");
                }
            }
            else if (includeTypes.Length == 1)
            {
                whereClauses.Add("type=@type");
                statement?.TryBind("@type", includeTypes[0]);
            }
            else if (includeTypes.Length > 1)
            {
                var inClause = string.Join(",", includeTypes.Select(i => "'" + i + "'"));
                whereClauses.Add($"type in ({inClause})");
            }

            if (query.ChannelIds.Length == 1)
            {
                whereClauses.Add("ChannelId=@ChannelId");
                statement?.TryBind("@ChannelId", query.ChannelIds[0].ToString("N", CultureInfo.InvariantCulture));
            }
            else if (query.ChannelIds.Length > 1)
            {
                var inClause = string.Join(",", query.ChannelIds.Select(i => "'" + i.ToString("N", CultureInfo.InvariantCulture) + "'"));
                whereClauses.Add($"ChannelId in ({inClause})");
            }

            if (!query.ParentId.Equals(Guid.Empty))
            {
                whereClauses.Add("ParentId=@ParentId");
                statement?.TryBind("@ParentId", query.ParentId);
            }

            if (!string.IsNullOrWhiteSpace(query.Path))
            {
                whereClauses.Add("Path=@Path");
                statement?.TryBind("@Path", GetPathToSave(query.Path));
            }

            if (!string.IsNullOrWhiteSpace(query.PresentationUniqueKey))
            {
                whereClauses.Add("PresentationUniqueKey=@PresentationUniqueKey");
                statement?.TryBind("@PresentationUniqueKey", query.PresentationUniqueKey);
            }

            if (query.MinCommunityRating.HasValue)
            {
                whereClauses.Add("CommunityRating>=@MinCommunityRating");
                statement?.TryBind("@MinCommunityRating", query.MinCommunityRating.Value);
            }

            if (query.MinIndexNumber.HasValue)
            {
                whereClauses.Add("IndexNumber>=@MinIndexNumber");
                statement?.TryBind("@MinIndexNumber", query.MinIndexNumber.Value);
            }

            if (query.MinDateCreated.HasValue)
            {
                whereClauses.Add("DateCreated>=@MinDateCreated");
                statement?.TryBind("@MinDateCreated", query.MinDateCreated.Value);
            }

            if (query.MinDateLastSaved.HasValue)
            {
                whereClauses.Add("(DateLastSaved not null and DateLastSaved>=@MinDateLastSavedForUser)");
                statement?.TryBind("@MinDateLastSaved", query.MinDateLastSaved.Value);
            }

            if (query.MinDateLastSavedForUser.HasValue)
            {
                whereClauses.Add("(DateLastSaved not null and DateLastSaved>=@MinDateLastSavedForUser)");
                statement?.TryBind("@MinDateLastSavedForUser", query.MinDateLastSavedForUser.Value);
            }

            if (query.IndexNumber.HasValue)
            {
                whereClauses.Add("IndexNumber=@IndexNumber");
                statement?.TryBind("@IndexNumber", query.IndexNumber.Value);
            }
            if (query.ParentIndexNumber.HasValue)
            {
                whereClauses.Add("ParentIndexNumber=@ParentIndexNumber");
                statement?.TryBind("@ParentIndexNumber", query.ParentIndexNumber.Value);
            }
            if (query.ParentIndexNumberNotEquals.HasValue)
            {
                whereClauses.Add("(ParentIndexNumber<>@ParentIndexNumberNotEquals or ParentIndexNumber is null)");
                statement?.TryBind("@ParentIndexNumberNotEquals", query.ParentIndexNumberNotEquals.Value);
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
                statement?.TryBind("@MinEndDate", minEndDate.Value);
            }

            if (maxEndDate.HasValue)
            {
                whereClauses.Add("EndDate<=@MaxEndDate");
                statement?.TryBind("@MaxEndDate", maxEndDate.Value);
            }

            if (query.MinStartDate.HasValue)
            {
                whereClauses.Add("StartDate>=@MinStartDate");
                statement?.TryBind("@MinStartDate", query.MinStartDate.Value);
            }

            if (query.MaxStartDate.HasValue)
            {
                whereClauses.Add("StartDate<=@MaxStartDate");
                statement?.TryBind("@MaxStartDate", query.MaxStartDate.Value);
            }

            if (query.MinPremiereDate.HasValue)
            {
                whereClauses.Add("PremiereDate>=@MinPremiereDate");
                statement?.TryBind("@MinPremiereDate", query.MinPremiereDate.Value);
            }

            if (query.MaxPremiereDate.HasValue)
            {
                whereClauses.Add("PremiereDate<=@MaxPremiereDate");
                statement?.TryBind("@MaxPremiereDate", query.MaxPremiereDate.Value);
            }

            var trailerTypes = query.TrailerTypes;
            int trailerTypesLen = trailerTypes.Length;
            if (trailerTypesLen > 0)
            {
                const string Or = " OR ";
                StringBuilder clause = new StringBuilder("(", trailerTypesLen * 32);
                for (int i = 0; i < trailerTypesLen; i++)
                {
                    var paramName = "@TrailerTypes" + i;
                    clause.Append("TrailerTypes like ")
                        .Append(paramName)
                        .Append(Or);
                    statement?.TryBind(paramName, "%" + trailerTypes[i] + "%");
                }

                // Remove last " OR "
                clause.Length -= Or.Length;
                clause.Append(')');

                whereClauses.Add(clause.ToString());
            }

            if (query.IsAiring.HasValue)
            {
                if (query.IsAiring.Value)
                {
                    whereClauses.Add("StartDate<=@MaxStartDate");
                    statement?.TryBind("@MaxStartDate", DateTime.UtcNow);

                    whereClauses.Add("EndDate>=@MinEndDate");
                    statement?.TryBind("@MinEndDate", DateTime.UtcNow);
                }
                else
                {
                    whereClauses.Add("(StartDate>@IsAiringDate OR EndDate < @IsAiringDate)");
                    statement?.TryBind("@IsAiringDate", DateTime.UtcNow);
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
                    statement?.TryBind(paramName, personId.ToByteArray());
                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
                whereClauses.Add(clause);
            }

            if (!string.IsNullOrWhiteSpace(query.Person))
            {
                whereClauses.Add("Guid in (select ItemId from People where Name=@PersonName)");
                statement?.TryBind("@PersonName", query.Person);
            }

            if (!string.IsNullOrWhiteSpace(query.MinSortName))
            {
                whereClauses.Add("SortName>=@MinSortName");
                statement?.TryBind("@MinSortName", query.MinSortName);
            }

            if (!string.IsNullOrWhiteSpace(query.ExternalSeriesId))
            {
                whereClauses.Add("ExternalSeriesId=@ExternalSeriesId");
                statement?.TryBind("@ExternalSeriesId", query.ExternalSeriesId);
            }

            if (!string.IsNullOrWhiteSpace(query.ExternalId))
            {
                whereClauses.Add("ExternalId=@ExternalId");
                statement?.TryBind("@ExternalId", query.ExternalId);
            }

            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                whereClauses.Add("CleanName=@Name");
                statement?.TryBind("@Name", GetCleanValue(query.Name));
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
                statement?.TryBind("@NameStartsWith", query.NameStartsWith + "%");
            }

            if (!string.IsNullOrWhiteSpace(query.NameStartsWithOrGreater))
            {
                whereClauses.Add("SortName >= @NameStartsWithOrGreater");
                // lowercase this because SortName is stored as lowercase
                statement?.TryBind("@NameStartsWithOrGreater", query.NameStartsWithOrGreater.ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(query.NameLessThan))
            {
                whereClauses.Add("SortName < @NameLessThan");
                // lowercase this because SortName is stored as lowercase
                statement?.TryBind("@NameLessThan", query.NameLessThan.ToLowerInvariant());
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
                    statement?.TryBind("@UserRating", UserItemData.MinLikeValue);
                }
                else
                {
                    whereClauses.Add("(rating is null or rating<@UserRating)");
                    statement?.TryBind("@UserRating", UserItemData.MinLikeValue);
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

                statement?.TryBind("@IsFavoriteOrLiked", query.IsFavoriteOrLiked.Value);
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

                statement?.TryBind("@IsFavorite", query.IsFavorite.Value);
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

                        statement?.TryBind("@IsPlayed", query.IsPlayed.Value);
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
                        statement.TryBind(paramName, artistId.ToByteArray());
                    }
                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
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
                        statement.TryBind(paramName, artistId.ToByteArray());
                    }
                    index++;
                }

                var clause = "(" + string.Join(" OR ", clauses) + ")";
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
                        statement.TryBind(paramName, artistId.ToByteArray());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses) + ")";
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
                        statement.TryBind(paramName, albumId.ToByteArray());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses) + ")";
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
                        statement.TryBind(paramName, artistId.ToByteArray());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses) + ")";
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
                        statement.TryBind(paramName, genreId.ToByteArray());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses) + ")";
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
                var clause = "(" + string.Join(" OR ", clauses) + ")";
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
                var clause = "(" + string.Join(" OR ", clauses) + ")";
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
                var clause = "(" + string.Join(" OR ", clauses) + ")";
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
                        statement.TryBind(paramName, studioId.ToByteArray());
                    }
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses) + ")";
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
                var clause = "(" + string.Join(" OR ", clauses) + ")";
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
                var val = string.Join(",", query.Years);

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
                var val = string.Join(",", queryMediaTypes.Select(i => "'" + i + "'"));

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

                whereClauses.Add("(" + string.Join(" OR ", includeIds) + ")");
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

                whereClauses.Add(string.Join(" AND ", excludeIds));
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
                    whereClauses.Add(string.Join(" AND ", excludeIds));
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

                    // TODO this seems to be an idea for a better schema where ProviderIds are their own table
                    //      buut this is not implemented
                    //hasProviderIds.Add("(COALESCE((select value from ProviderIds where ItemId=Guid and Name = '" + pair.Key + "'), '') <> " + paramName + ")");

                    // TODO this is a really BAD way to do it since the pair:
                    //      Tmdb, 1234 matches Tmdb=1234 but also Tmdb=1234567
                    //      and maybe even NotTmdb=1234.

                    // this is a placeholder for this specific pair to correlate it in the bigger query
                    var paramName = "@HasAnyProviderId" + index;

                    // this is a search for the placeholder
                    hasProviderIds.Add("ProviderIds like " + paramName + "");

                    // this replaces the placeholder with a value, here: %key=val%
                    if (statement != null)
                    {
                        statement.TryBind(paramName, "%" + pair.Key + "=" + pair.Value + "%");
                    }
                    index++;

                    break;
                }

                if (hasProviderIds.Count > 0)
                {
                    whereClauses.Add("(" + string.Join(" OR ", hasProviderIds) + ")");
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
                    var itemByNameTypeVal = string.Join(",", includedItemByNameTypes.Select(i => "'" + i + "'"));
                    whereClauses.Add("(TopParentId=@TopParentId or Type in (" + itemByNameTypeVal + "))");
                }
                else
                {
                    whereClauses.Add("(TopParentId=@TopParentId)");
                }
                if (statement != null)
                {
                    statement.TryBind("@TopParentId", queryTopParentIds[0].ToString("N", CultureInfo.InvariantCulture));
                }
            }
            else if (queryTopParentIds.Length > 1)
            {
                var val = string.Join(",", queryTopParentIds.Select(i => "'" + i.ToString("N", CultureInfo.InvariantCulture) + "'"));

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
                    var itemByNameTypeVal = string.Join(",", includedItemByNameTypes.Select(i => "'" + i + "'"));
                    whereClauses.Add("(Type in (" + itemByNameTypeVal + ") or TopParentId in (" + val + "))");
                }
                else
                {
                    whereClauses.Add("TopParentId in (" + val + ")");
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
                var inClause = string.Join(",", query.AncestorIds.Select(i => "'" + i.ToString("N", CultureInfo.InvariantCulture) + "'"));
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
                var inClause = string.Join(",", query.BlockUnratedItems.Select(i => "'" + i.ToString() + "'"));
                whereClauses.Add(string.Format("(InheritedParentalRatingValue > 0 or UnratedType not in ({0}))", inClause));
            }

            if (query.ExcludeInheritedTags.Length > 0)
            {
                var paramName = "@ExcludeInheritedTags";
                if (statement == null)
                {
                    int index = 0;
                    string excludedTags = string.Join(",", query.ExcludeInheritedTags.Select(t => paramName + index++));
                    whereClauses.Add("((select CleanValue from itemvalues where ItemId=Guid and Type=6 and cleanvalue in (" + excludedTags + ")) is null)");
                }
                else
                {
                    for (int index = 0; index < query.ExcludeInheritedTags.Length; index++)
                    {
                        statement.TryBind(paramName + index, GetCleanValue(query.ExcludeInheritedTags[index]));
                    }
                }
            }

            if (query.SeriesStatuses.Length > 0)
            {
                var statuses = new List<string>();

                foreach (var seriesStatus in query.SeriesStatuses)
                {
                    statuses.Add("data like  '%" + seriesStatus + "%'");
                }

                whereClauses.Add("(" + string.Join(" OR ", statuses) + ")");
            }

            if (query.BoxSetLibraryFolders.Length > 0)
            {
                var folderIdQueries = new List<string>();

                foreach (var folderId in query.BoxSetLibraryFolders)
                {
                    folderIdQueries.Add("data like '%" + folderId.ToString("N", CultureInfo.InvariantCulture) + "%'");
                }

                whereClauses.Add("(" + string.Join(" OR ", folderIdQueries) + ")");
            }

            if (query.VideoTypes.Length > 0)
            {
                var videoTypes = new List<string>();

                foreach (var videoType in query.VideoTypes)
                {
                    videoTypes.Add("data like '%\"VideoType\":\"" + videoType.ToString() + "\"%'");
                }

                whereClauses.Add("(" + string.Join(" OR ", videoTypes) + ")");
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

            return value.RemoveDiacritics().ToLowerInvariant();
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

            var types = new[]
            {
                typeof(Episode).Name,
                typeof(Video).Name,
                typeof(Movie).Name,
                typeof(MusicVideo).Name,
                typeof(Series).Name,
                typeof(Season).Name
            };

            if (types.Any(i => query.IncludeItemTypes.Contains(i, StringComparer.OrdinalIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private static readonly Type[] _knownTypes =
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
            string sql = string.Join(
                ";",
                new string[]
                {
                    "delete from itemvalues where type = 6",

                    "insert into itemvalues (ItemId, Type, Value, CleanValue)  select ItemId, 6, Value, CleanValue from ItemValues where Type=4",

                    @"insert into itemvalues (ItemId, Type, Value, CleanValue) select AncestorIds.itemid, 6, ItemValues.Value, ItemValues.CleanValue
FROM AncestorIds
LEFT JOIN ItemValues ON (AncestorIds.AncestorId = ItemValues.ItemId)
where AncestorIdText not null and ItemValues.Value not null and ItemValues.Type = 4 "
                });

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    connection.ExecuteAll(sql);

                }, TransactionMode);
            }
        }

        private static Dictionary<string, string[]> GetTypeMapDictionary()
        {
            var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

            foreach (var t in _knownTypes)
            {
                dict[t.Name] = new[] { t.FullName };
            }

            dict["Program"] = new[] { typeof(LiveTvProgram).FullName };
            dict["TvChannel"] = new[] { typeof(LiveTvChannel).FullName };

            return dict;
        }

        // Not crazy about having this all the way down here, but at least it's in one place
        private readonly Dictionary<string, string[]> _types = GetTypeMapDictionary();

        private string[] MapIncludeItemTypes(string value)
        {
            if (_types.TryGetValue(value, out string[] result))
            {
                return result;
            }

            if (IsValidType(value))
            {
                return new[] { value };
            }

            return Array.Empty<string>();
        }

        public void DeleteItem(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            CheckDisposed();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    var idBlob = id.ToByteArray();

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

        private void ExecuteWithSingleParam(IDatabaseConnection db, string query, ReadOnlySpan<byte> value)
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
                throw new ArgumentNullException(nameof(query));
            }

            CheckDisposed();

            var commandText = "select Distinct Name from People";

            var whereClauses = GetPeopleWhereClauses(query, null);

            if (whereClauses.Count != 0)
            {
                commandText += "  where " + string.Join(" AND ", whereClauses);
            }

            commandText += " order by ListOrder";

            if (query.Limit > 0)
            {
                commandText += " LIMIT " + query.Limit;
            }

            using (var connection = GetConnection(true))
            {
                var list = new List<string>();
                using (var statement = PrepareStatement(connection, commandText))
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

        public List<PersonInfo> GetPeople(InternalPeopleQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            CheckDisposed();

            var commandText = "select ItemId, Name, Role, PersonType, SortOrder from People";

            var whereClauses = GetPeopleWhereClauses(query, null);

            if (whereClauses.Count != 0)
            {
                commandText += "  where " + string.Join(" AND ", whereClauses);
            }

            commandText += " order by ListOrder";

            if (query.Limit > 0)
            {
                commandText += " LIMIT " + query.Limit;
            }

            using (var connection = GetConnection(true))
            {
                var list = new List<PersonInfo>();

                using (var statement = PrepareStatement(connection, commandText))
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

        private List<string> GetPeopleWhereClauses(InternalPeopleQuery query, IStatement statement)
        {
            var whereClauses = new List<string>();

            if (!query.ItemId.Equals(Guid.Empty))
            {
                whereClauses.Add("ItemId=@ItemId");
                if (statement != null)
                {
                    statement.TryBind("@ItemId", query.ItemId.ToByteArray());
                }
            }
            if (!query.AppearsInItemId.Equals(Guid.Empty))
            {
                whereClauses.Add("Name in (Select Name from People where ItemId=@AppearsInItemId)");
                if (statement != null)
                {
                    statement.TryBind("@AppearsInItemId", query.AppearsInItemId.ToByteArray());
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
                var val = string.Join(",", queryPersonTypes.Select(i => "'" + i + "'"));

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
                var val = string.Join(",", queryExcludePersonTypes.Select(i => "'" + i + "'"));

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
                throw new ArgumentNullException(nameof(itemId));
            }

            if (ancestorIds == null)
            {
                throw new ArgumentNullException(nameof(ancestorIds));
            }

            CheckDisposed();

            var itemIdBlob = itemId.ToByteArray();

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

            using (var statement = PrepareStatement(db, insertText.ToString()))
            {
                statement.TryBind("@ItemId", itemIdBlob);

                for (var i = 0; i < ancestorIds.Count; i++)
                {
                    var index = i.ToString(CultureInfo.InvariantCulture);

                    var ancestorId = ancestorIds[i];

                    statement.TryBind("@AncestorId" + index, ancestorId.ToByteArray());
                    statement.TryBind("@AncestorIdText" + index, ancestorId.ToString("N", CultureInfo.InvariantCulture));
                }

                statement.Reset();
                statement.MoveNext();
            }
        }

        public QueryResult<(BaseItem, ItemCounts)> GetAllArtists(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 0, 1 }, typeof(MusicArtist).FullName);
        }

        public QueryResult<(BaseItem, ItemCounts)> GetArtists(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 0 }, typeof(MusicArtist).FullName);
        }

        public QueryResult<(BaseItem, ItemCounts)> GetAlbumArtists(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 1 }, typeof(MusicArtist).FullName);
        }

        public QueryResult<(BaseItem, ItemCounts)> GetStudios(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 3 }, typeof(Studio).FullName);
        }

        public QueryResult<(BaseItem, ItemCounts)> GetGenres(InternalItemsQuery query)
        {
            return GetItemValues(query, new[] { 2 }, typeof(Genre).FullName);
        }

        public QueryResult<(BaseItem, ItemCounts)> GetMusicGenres(InternalItemsQuery query)
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

        public List<string> GetGenreNames()
        {
            return GetItemValueNames(new[] { 2 }, new List<string>(), new List<string> { "Audio", "MusicVideo", "MusicAlbum", "MusicArtist" });
        }

        private List<string> GetItemValueNames(int[] itemValueTypes, List<string> withItemTypes, List<string> excludeItemTypes)
        {
            CheckDisposed();

            withItemTypes = withItemTypes.SelectMany(MapIncludeItemTypes).ToList();
            excludeItemTypes = excludeItemTypes.SelectMany(MapIncludeItemTypes).ToList();

            var now = DateTime.UtcNow;

            var typeClause = itemValueTypes.Length == 1 ?
                ("Type=" + itemValueTypes[0].ToString(CultureInfo.InvariantCulture)) :
                ("Type in (" + string.Join(",", itemValueTypes.Select(i => i.ToString(CultureInfo.InvariantCulture))) + ")");

            var commandText = "Select Value From ItemValues where " + typeClause;

            if (withItemTypes.Count > 0)
            {
                var typeString = string.Join(",", withItemTypes.Select(i => "'" + i + "'"));
                commandText += " AND ItemId In (select guid from typedbaseitems where type in (" + typeString + "))";
            }
            if (excludeItemTypes.Count > 0)
            {
                var typeString = string.Join(",", excludeItemTypes.Select(i => "'" + i + "'"));
                commandText += " AND ItemId not In (select guid from typedbaseitems where type in (" + typeString + "))";
            }

            commandText += " Group By CleanValue";

            var list = new List<string>();
            using (var connection = GetConnection(true))
            {
                using (var statement = PrepareStatement(connection, commandText))
                {
                    foreach (var row in statement.ExecuteQuery())
                    {
                        if (!row.IsDBNull(0))
                        {
                            list.Add(row.GetString(0));
                        }
                    }
                }

            }

            LogQueryTime("GetItemValueNames", commandText, now);
            return list;
        }

        private QueryResult<(BaseItem, ItemCounts)> GetItemValues(InternalItemsQuery query, int[] itemValueTypes, string returnType)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (!query.Limit.HasValue)
            {
                query.EnableTotalRecordCount = false;
            }

            CheckDisposed();

            var now = DateTime.UtcNow;

            var typeClause = itemValueTypes.Length == 1 ?
                ("Type=" + itemValueTypes[0].ToString(CultureInfo.InvariantCulture)) :
                ("Type in (" + string.Join(",", itemValueTypes.Select(i => i.ToString(CultureInfo.InvariantCulture))) + ")");

            InternalItemsQuery typeSubQuery = null;

            Dictionary<string, string> itemCountColumns = null;

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

                itemCountColumnQuery += " where " + string.Join(" AND ", whereClauses);

                itemCountColumns = new Dictionary<string, string>()
                {
                    { "itemTypes", "(" + itemCountColumnQuery + ") as itemTypes"}
                };
            }

            List<string> columns = _retriveItemColumns.ToList();
            if (itemCountColumns != null)
            {
                columns.AddRange(itemCountColumns.Values);
            }

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

            columns = GetFinalColumnsToSelect(query, columns);

            var commandText = "select "
                            + string.Join(",", columns)
                            + GetFromText()
                            + GetJoinUserDataText(query);

            var innerWhereClauses = GetWhereClauses(innerQuery, null);

            var innerWhereText = innerWhereClauses.Count == 0 ?
                string.Empty :
                " where " + string.Join(" AND ", innerWhereClauses);

            var whereText = " where Type=@SelectType And CleanName In (Select CleanValue from ItemValues where " + typeClause + " AND ItemId in (select guid from TypedBaseItems" + innerWhereText + "))";

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

            if (outerWhereClauses.Count != 0)
            {
                whereText += " AND " + string.Join(" AND ", outerWhereClauses);
            }

            commandText += whereText + " group by PresentationUniqueKey";

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
                var countText = "select "
                            + string.Join(",", GetFinalColumnsToSelect(query, new[] { "count (distinct PresentationUniqueKey)" }))
                            + GetFromText()
                            + GetJoinUserDataText(query)
                            + whereText;

                statementTexts.Add(countText);
            }

            var list = new List<(BaseItem, ItemCounts)>();
            var result = new QueryResult<(BaseItem, ItemCounts)>();
            using (var connection = GetConnection(true))
            {
                connection.RunInTransaction(
                    db =>
                    {
                        var statements = PrepareAll(db, statementTexts).ToList();

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

                                        list.Add((item, GetItemCounts(row, countStartColumn, typesToCount)));
                                    }
                                }
                            }
                        }

                        if (query.EnableTotalRecordCount)
                        {
                            commandText = "select "
                                        + string.Join(",", GetFinalColumnsToSelect(query, new[] { "count (distinct PresentationUniqueKey)" }))
                                        + GetFromText()
                                        + GetJoinUserDataText(query)
                                        + whereText;

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
                            }
                        }
                    },
                    ReadTransactionMode);
            }

            LogQueryTime("GetItemValues", commandText, now);

            if (result.TotalRecordCount == 0)
            {
                result.TotalRecordCount = list.Count;
            }

            result.Items = list;

            return result;
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
                .ToLookup(x => x);

            foreach (var type in allTypes)
            {
                var value = type.Count();
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
                else if (string.Equals(typeName, typeof(Trailer).FullName, StringComparison.OrdinalIgnoreCase))
                {
                    counts.TrailerCount = value;
                }

                counts.ItemCount += value;
            }

            return counts;
        }

        private List<(int, string)> GetItemValuesToSave(BaseItem item, List<string> inheritedTags)
        {
            var list = new List<(int, string)>();

            if (item is IHasArtist hasArtist)
            {
                list.AddRange(hasArtist.Artists.Select(i => (0, i)));
            }

            if (item is IHasAlbumArtist hasAlbumArtist)
            {
                list.AddRange(hasAlbumArtist.AlbumArtists.Select(i => (1, i)));
            }

            list.AddRange(item.Genres.Select(i => (2, i)));
            list.AddRange(item.Studios.Select(i => (3, i)));
            list.AddRange(item.Tags.Select(i => (4, i)));

            // keywords was 5

            list.AddRange(inheritedTags.Select(i => (6, i)));

            return list;
        }

        private void UpdateItemValues(Guid itemId, List<(int, string)> values, IDatabaseConnection db)
        {
            if (itemId.Equals(Guid.Empty))
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            CheckDisposed();

            var guidBlob = itemId.ToByteArray();

            // First delete
            db.Execute("delete from ItemValues where ItemId=@Id", guidBlob);

            InsertItemValues(guidBlob, values, db);
        }

        private void InsertItemValues(byte[] idBlob, List<(int, string)> values, IDatabaseConnection db)
        {
            const int Limit = 100;
            var startIndex = 0;

            while (startIndex < values.Count)
            {
                var insertText = new StringBuilder("insert into ItemValues (ItemId, Type, Value, CleanValue) values ");

                var endIndex = Math.Min(values.Count, startIndex + Limit);

                for (var i = startIndex; i < endIndex; i++)
                {
                    insertText.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "(@ItemId, @Type{0}, @Value{0}, @CleanValue{0}),",
                        i);
                }

                // Remove last comma
                insertText.Length--;

                using (var statement = PrepareStatement(db, insertText.ToString()))
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

                startIndex += Limit;
            }
        }

        public void UpdatePeople(Guid itemId, List<PersonInfo> people)
        {
            if (itemId.Equals(Guid.Empty))
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            if (people == null)
            {
                throw new ArgumentNullException(nameof(people));
            }

            CheckDisposed();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    var itemIdBlob = itemId.ToByteArray();

                    // First delete chapters
                    db.Execute("delete from People where ItemId=@ItemId", itemIdBlob);

                    InsertPeople(itemIdBlob, people, db);

                }, TransactionMode);
            }
        }

        private void InsertPeople(byte[] idBlob, List<PersonInfo> people, IDatabaseConnection db)
        {
            const int Limit = 100;
            var startIndex = 0;
            var listIndex = 0;

            while (startIndex < people.Count)
            {
                var insertText = new StringBuilder("insert into People (ItemId, Name, Role, PersonType, SortOrder, ListOrder) values ");

                var endIndex = Math.Min(people.Count, startIndex + Limit);
                for (var i = startIndex; i < endIndex; i++)
                {
                    insertText.AppendFormat("(@ItemId, @Name{0}, @Role{0}, @PersonType{0}, @SortOrder{0}, @ListOrder{0}),", i.ToString(CultureInfo.InvariantCulture));
                }

                // Remove last comma
                insertText.Length--;

                using (var statement = PrepareStatement(db, insertText.ToString()))
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

                startIndex += Limit;
            }
        }

        private PersonInfo GetPerson(IReadOnlyList<IResultSetValue> reader)
        {
            var item = new PersonInfo
            {
                ItemId = reader.GetGuid(0),
                Name = reader.GetString(1)
            };

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
                throw new ArgumentNullException(nameof(query));
            }

            var cmdText = "select "
                        + string.Join(",", _mediaStreamSaveColumns)
                        + " from mediastreams where"
                        + " ItemId=@ItemId";

            if (query.Type.HasValue)
            {
                cmdText += " AND StreamType=@StreamType";
            }

            if (query.Index.HasValue)
            {
                cmdText += " AND StreamIndex=@StreamIndex";
            }

            cmdText += " order by StreamIndex ASC";

            using (var connection = GetConnection(true))
            {
                var list = new List<MediaStream>();

                using (var statement = PrepareStatement(connection, cmdText))
                {
                    statement.TryBind("@ItemId", query.ItemId.ToByteArray());

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

        public void SaveMediaStreams(Guid id, List<MediaStream> streams, CancellationToken cancellationToken)
        {
            CheckDisposed();

            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (streams == null)
            {
                throw new ArgumentNullException(nameof(streams));
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    var itemIdBlob = id.ToByteArray();

                    // First delete chapters
                    db.Execute("delete from mediastreams where ItemId=@ItemId", itemIdBlob);

                    InsertMediaStreams(itemIdBlob, streams, db);

                }, TransactionMode);
            }
        }

        private void InsertMediaStreams(byte[] idBlob, List<MediaStream> streams, IDatabaseConnection db)
        {
            const int Limit = 10;
            var startIndex = 0;

            while (startIndex < streams.Count)
            {
                var insertText = new StringBuilder("insert into mediastreams (");
                foreach (var column in _mediaStreamSaveColumns)
                {
                    insertText.Append(column).Append(',');
                }

                // Remove last comma
                insertText.Length--;
                insertText.Append(") values ");

                var endIndex = Math.Min(streams.Count, startIndex + Limit);

                for (var i = startIndex; i < endIndex; i++)
                {
                    if (i != startIndex)
                    {
                        insertText.Append(',');
                    }

                    var index = i.ToString(CultureInfo.InvariantCulture);
                    insertText.Append("(@ItemId, ");

                    foreach (var column in _mediaStreamSaveColumns.Skip(1))
                    {
                        insertText.Append('@').Append(column).Append(index).Append(',');
                    }

                    insertText.Length -= 1; // Remove the last comma

                    insertText.Append(')');
                }

                using (var statement = PrepareStatement(db, insertText.ToString()))
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

                startIndex += Limit;
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

            item.Type = Enum.Parse<MediaStreamType>(reader[2].ToString(), true);

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

            if (item.Type == MediaStreamType.Subtitle)
            {
                item.localizedUndefined = _localization.GetLocalizedString("Undefined");
                item.localizedDefault = _localization.GetLocalizedString("Default");
                item.localizedForced = _localization.GetLocalizedString("Forced");
            }

            return item;
        }

        public List<MediaAttachment> GetMediaAttachments(MediaAttachmentQuery query)
        {
            CheckDisposed();

            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var cmdText = "select "
                        + string.Join(",", _mediaAttachmentSaveColumns)
                        + " from mediaattachments where"
                        + " ItemId=@ItemId";

            if (query.Index.HasValue)
            {
                cmdText += " AND AttachmentIndex=@AttachmentIndex";
            }

            cmdText += " order by AttachmentIndex ASC";

            var list = new List<MediaAttachment>();
            using (var connection = GetConnection(true))
            using (var statement = PrepareStatement(connection, cmdText))
            {
                statement.TryBind("@ItemId", query.ItemId.ToByteArray());

                if (query.Index.HasValue)
                {
                    statement.TryBind("@AttachmentIndex", query.Index.Value);
                }

                foreach (var row in statement.ExecuteQuery())
                {
                    list.Add(GetMediaAttachment(row));
                }
            }

            return list;
        }

        public void SaveMediaAttachments(
            Guid id,
            IReadOnlyList<MediaAttachment> attachments,
            CancellationToken cancellationToken)
        {
            CheckDisposed();
            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            if (attachments == null)
            {
                throw new ArgumentNullException(nameof(attachments));
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    var itemIdBlob = id.ToByteArray();

                    db.Execute("delete from mediaattachments where ItemId=@ItemId", itemIdBlob);

                    InsertMediaAttachments(itemIdBlob, attachments, db, cancellationToken);

                }, TransactionMode);
            }
        }

        private void InsertMediaAttachments(
            byte[] idBlob,
            IReadOnlyList<MediaAttachment> attachments,
            IDatabaseConnection db,
            CancellationToken cancellationToken)
        {
            const int InsertAtOnce = 10;

            for (var startIndex = 0; startIndex < attachments.Count; startIndex += InsertAtOnce)
            {
                var insertText = new StringBuilder(_mediaAttachmentInsertPrefix);

                var endIndex = Math.Min(attachments.Count, startIndex + InsertAtOnce);

                for (var i = startIndex; i < endIndex; i++)
                {
                    var index = i.ToString(CultureInfo.InvariantCulture);
                    insertText.Append("(@ItemId, ");

                    foreach (var column in _mediaAttachmentSaveColumns.Skip(1))
                    {
                        insertText.Append("@" + column + index + ",");
                    }

                    insertText.Length -= 1;

                    insertText.Append("),");
                }

                insertText.Length--;

                cancellationToken.ThrowIfCancellationRequested();

                using (var statement = PrepareStatement(db, insertText.ToString()))
                {
                    statement.TryBind("@ItemId", idBlob);

                    for (var i = startIndex; i < endIndex; i++)
                    {
                        var index = i.ToString(CultureInfo.InvariantCulture);

                        var attachment = attachments[i];

                        statement.TryBind("@AttachmentIndex" + index, attachment.Index);
                        statement.TryBind("@Codec" + index, attachment.Codec);
                        statement.TryBind("@CodecTag" + index, attachment.CodecTag);
                        statement.TryBind("@Comment" + index, attachment.Comment);
                        statement.TryBind("@Filename" + index, attachment.FileName);
                        statement.TryBind("@MIMEType" + index, attachment.MimeType);
                    }

                    statement.Reset();
                    statement.MoveNext();
                }
            }
        }

        /// <summary>
        /// Gets the attachment.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>MediaAttachment</returns>
        private MediaAttachment GetMediaAttachment(IReadOnlyList<IResultSetValue> reader)
        {
            var item = new MediaAttachment
            {
                Index = reader[1].ToInt()
            };

            if (reader[2].SQLiteType != SQLiteType.Null)
            {
                item.Codec = reader[2].ToString();
            }

            if (reader[2].SQLiteType != SQLiteType.Null)
            {
                item.CodecTag = reader[3].ToString();
            }

            if (reader[4].SQLiteType != SQLiteType.Null)
            {
                item.Comment = reader[4].ToString();
            }

            if (reader[6].SQLiteType != SQLiteType.Null)
            {
                item.FileName = reader[5].ToString();
            }

            if (reader[6].SQLiteType != SQLiteType.Null)
            {
                item.MimeType = reader[6].ToString();
            }

            return item;
        }
    }
}
