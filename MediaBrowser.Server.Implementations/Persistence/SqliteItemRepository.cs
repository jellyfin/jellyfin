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
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Server.Implementations.Persistence
{
    /// <summary>
    /// Class SQLiteItemRepository
    /// </summary>
    public class SqliteItemRepository : BaseSqliteRepository, IItemRepository
    {
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

        private readonly string _criticReviewsPath;

        public const int LatestSchemaVersion = 91;

        private IDbConnection _connection;

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

        protected override async Task<IDbConnection> CreateConnection(bool isReadOnly = false)
        {
            var connection = await DbConnector.Connect(DbFilePath, false, true, 20000).ConfigureAwait(false);

            //AttachUserDataDb(connection);

            //connection.RunQueries(new[]
            //{
            //    "pragma locking_mode=NORMAL"

            //}, Logger);

            return connection;
        }

        private const string ChaptersTableName = "Chapters2";

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize(IDbConnector dbConnector)
        {
            //_connection = await CreateConnection().ConfigureAwait(false);

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                var createMediaStreamsTableCommand
                   = "create table if not exists mediastreams (ItemId GUID, StreamIndex INT, StreamType TEXT, Codec TEXT, Language TEXT, ChannelLayout TEXT, Profile TEXT, AspectRatio TEXT, Path TEXT, IsInterlaced BIT, BitRate INT NULL, Channels INT NULL, SampleRate INT NULL, IsDefault BIT, IsForced BIT, IsExternal BIT, Height INT NULL, Width INT NULL, AverageFrameRate FLOAT NULL, RealFrameRate FLOAT NULL, Level FLOAT NULL, PixelFormat TEXT, BitDepth INT NULL, IsAnamorphic BIT NULL, RefFrames INT NULL, CodecTag TEXT NULL, Comment TEXT NULL, NalLengthSize TEXT NULL, IsAvc BIT NULL, Title TEXT NULL, TimeBase TEXT NULL, CodecTimeBase TEXT NULL, PRIMARY KEY (ItemId, StreamIndex))";

                string[] queries = {

                                "create table if not exists TypedBaseItems (guid GUID primary key, type TEXT, data BLOB, ParentId GUID, Path TEXT)",
                                "create index if not exists idx_PathTypedBaseItems on TypedBaseItems(Path)",
                                "create index if not exists idx_ParentIdTypedBaseItems on TypedBaseItems(ParentId)",

                                "create table if not exists AncestorIds (ItemId GUID, AncestorId GUID, AncestorIdText TEXT, PRIMARY KEY (ItemId, AncestorId))",
                                "create index if not exists idx_AncestorIds1 on AncestorIds(AncestorId)",
                                "create index if not exists idx_AncestorIds2 on AncestorIds(AncestorIdText)",

                                "create table if not exists UserDataKeys (ItemId GUID, UserDataKey TEXT, PRIMARY KEY (ItemId, UserDataKey))",
                                "create index if not exists idx_UserDataKeys1 on UserDataKeys(ItemId)",

                                "create table if not exists ItemValues (ItemId GUID, Type INT, Value TEXT)",
                                "create index if not exists idx_ItemValues on ItemValues(ItemId)",
                                "create index if not exists idx_ItemValues2 on ItemValues(ItemId,Type)",

                                "create table if not exists ProviderIds (ItemId GUID, Name TEXT, Value TEXT, PRIMARY KEY (ItemId, Name))",
                                "create index if not exists Idx_ProviderIds on ProviderIds(ItemId)",

                                "create table if not exists Images (ItemId GUID NOT NULL, Path TEXT NOT NULL, ImageType INT NOT NULL, DateModified DATETIME, IsPlaceHolder BIT NOT NULL, SortOrder INT)",
                                "create index if not exists idx_Images on Images(ItemId)",

                                "create table if not exists People (ItemId GUID, Name TEXT NOT NULL, Role TEXT, PersonType TEXT, SortOrder int, ListOrder int)",
                                "create index if not exists idxPeopleItemId on People(ItemId)",
                                "create index if not exists idxPeopleName on People(Name)",

                                "create table if not exists "+ChaptersTableName+" (ItemId GUID, ChapterIndex INT, StartPositionTicks BIGINT, Name TEXT, ImagePath TEXT, PRIMARY KEY (ItemId, ChapterIndex))",
                                "create index if not exists idx_"+ChaptersTableName+"1 on "+ChaptersTableName+"(ItemId)",

                                createMediaStreamsTableCommand,
                                "create index if not exists idx_mediastreams1 on mediastreams(ItemId)",
                                
                               };

                connection.RunQueries(queries, Logger);

                connection.AddColumn(Logger, "AncestorIds", "AncestorIdText", "Text");

                connection.AddColumn(Logger, "TypedBaseItems", "Path", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "StartDate", "DATETIME");
                connection.AddColumn(Logger, "TypedBaseItems", "EndDate", "DATETIME");
                connection.AddColumn(Logger, "TypedBaseItems", "ChannelId", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "IsMovie", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "IsSports", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "IsKids", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "CommunityRating", "Float");
                connection.AddColumn(Logger, "TypedBaseItems", "CustomRating", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "IndexNumber", "INT");
                connection.AddColumn(Logger, "TypedBaseItems", "IsLocked", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "Name", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "OfficialRating", "Text");

                connection.AddColumn(Logger, "TypedBaseItems", "MediaType", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "Overview", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "ParentIndexNumber", "INT");
                connection.AddColumn(Logger, "TypedBaseItems", "PremiereDate", "DATETIME");
                connection.AddColumn(Logger, "TypedBaseItems", "ProductionYear", "INT");
                connection.AddColumn(Logger, "TypedBaseItems", "ParentId", "GUID");
                connection.AddColumn(Logger, "TypedBaseItems", "Genres", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "ParentalRatingValue", "INT");
                connection.AddColumn(Logger, "TypedBaseItems", "SchemaVersion", "INT");
                connection.AddColumn(Logger, "TypedBaseItems", "SortName", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "RunTimeTicks", "BIGINT");

                connection.AddColumn(Logger, "TypedBaseItems", "OfficialRatingDescription", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "HomePageUrl", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "VoteCount", "INT");
                connection.AddColumn(Logger, "TypedBaseItems", "DisplayMediaType", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "DateCreated", "DATETIME");
                connection.AddColumn(Logger, "TypedBaseItems", "DateModified", "DATETIME");

                connection.AddColumn(Logger, "TypedBaseItems", "ForcedSortName", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "IsOffline", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "LocationType", "Text");

                connection.AddColumn(Logger, "TypedBaseItems", "IsSeries", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "IsLive", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "IsNews", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "IsPremiere", "BIT");

                connection.AddColumn(Logger, "TypedBaseItems", "EpisodeTitle", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "IsRepeat", "BIT");

                connection.AddColumn(Logger, "TypedBaseItems", "PreferredMetadataLanguage", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "PreferredMetadataCountryCode", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "IsHD", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "ExternalEtag", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "DateLastRefreshed", "DATETIME");

                connection.AddColumn(Logger, "TypedBaseItems", "DateLastSaved", "DATETIME");
                connection.AddColumn(Logger, "TypedBaseItems", "IsInMixedFolder", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "LockedFields", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "Studios", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "Audio", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "ExternalServiceId", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "Tags", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "IsFolder", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "InheritedParentalRatingValue", "INT");
                connection.AddColumn(Logger, "TypedBaseItems", "UnratedType", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "TopParentId", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "IsItemByName", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "SourceType", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "TrailerTypes", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "CriticRating", "Float");
                connection.AddColumn(Logger, "TypedBaseItems", "CriticRatingSummary", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "DateModifiedDuringLastRefresh", "DATETIME");
                connection.AddColumn(Logger, "TypedBaseItems", "InheritedTags", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "CleanName", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "PresentationUniqueKey", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "SlugName", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "OriginalTitle", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "PrimaryVersionId", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "DateLastMediaAdded", "DATETIME");
                connection.AddColumn(Logger, "TypedBaseItems", "Album", "Text");
                connection.AddColumn(Logger, "TypedBaseItems", "IsVirtualItem", "BIT");
                connection.AddColumn(Logger, "TypedBaseItems", "SeriesName", "Text");

                connection.AddColumn(Logger, "UserDataKeys", "Priority", "INT");

                string[] postQueries =
                    {
                "create index if not exists idx_PresentationUniqueKey on TypedBaseItems(PresentationUniqueKey)",
                "create index if not exists idx_GuidType on TypedBaseItems(Guid,Type)",
                "create index if not exists idx_Type on TypedBaseItems(Type)",
                "create index if not exists idx_TopParentId on TypedBaseItems(TopParentId)",
                "create index if not exists idx_TypeTopParentId on TypedBaseItems(Type,TopParentId)",
                "create index if not exists idx_TypeTopParentId2 on TypedBaseItems(TopParentId,MediaType,IsVirtualItem)",
                "create index if not exists idx_TypeTopParentId3 on TypedBaseItems(TopParentId,IsFolder,IsVirtualItem)",
                "create index if not exists idx_TypeTopParentId4 on TypedBaseItems(TopParentId,Type,IsVirtualItem)",
                "create index if not exists idx_TypeTopParentId5 on TypedBaseItems(TopParentId,IsVirtualItem)"
                };

                connection.RunQueries(postQueries, Logger);

                new MediaStreamColumns(connection, Logger).AddColumns();

                //AttachUserDataDb(connection);
            }
        }

        private void AttachUserDataDb(IDbConnection connection)
        {
            DataExtensions.Attach(connection, Path.Combine(_config.ApplicationPaths.DataPath, "userdata_v2.db"), "UserDataDb" + Guid.NewGuid().ToString("N"));
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
            "DateModifiedDuringLastRefresh",
            "OriginalTitle",
            "PrimaryVersionId",
            "DateLastMediaAdded",
            "Album",
            "CriticRating",
            "CriticRatingSummary",
            "IsVirtualItem"
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

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                IDbTransaction transaction = null;

                try
                {
                    transaction = connection.BeginTransaction();

                    using (var saveItemCommand = connection.CreateCommand())
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
                            "ParentalRatingValue",
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
                            "DateModifiedDuringLastRefresh",
                            "InheritedTags",
                            "CleanName",
                            "PresentationUniqueKey",
                            "SlugName",
                            "OriginalTitle",
                            "PrimaryVersionId",
                            "DateLastMediaAdded",
                            "Album",
                            "IsVirtualItem",
                            "SeriesName"
                        };

                        saveItemCommand.CommandText = "replace into TypedBaseItems (" + string.Join(",", saveColumns.ToArray()) + ") values (";

                        for (var i = 1; i <= saveColumns.Count; i++)
                        {
                            if (i > 1)
                            {
                                saveItemCommand.CommandText += ",";
                            }
                            saveItemCommand.CommandText += "@" + i.ToString(CultureInfo.InvariantCulture);

                            saveItemCommand.Parameters.Add(saveItemCommand, "@" + i.ToString(CultureInfo.InvariantCulture));
                        }
                        saveItemCommand.CommandText += ")";

                        foreach (var item in items)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var index = 0;

                            saveItemCommand.GetParameter(index++).Value = item.Id;
                            saveItemCommand.GetParameter(index++).Value = item.GetType().FullName;
                            saveItemCommand.GetParameter(index++).Value = _jsonSerializer.SerializeToBytes(item);

                            saveItemCommand.GetParameter(index++).Value = item.Path;

                            var hasStartDate = item as IHasStartDate;
                            if (hasStartDate != null)
                            {
                                saveItemCommand.GetParameter(index++).Value = hasStartDate.StartDate;
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }

                            saveItemCommand.GetParameter(index++).Value = item.EndDate;
                            saveItemCommand.GetParameter(index++).Value = item.ChannelId;

                            var hasProgramAttributes = item as IHasProgramAttributes;
                            if (hasProgramAttributes != null)
                            {
                                saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsKids;
                                saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsMovie;
                                saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsSports;
                                saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsSeries;
                                saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsLive;
                                saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsNews;
                                saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsPremiere;
                                saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.EpisodeTitle;
                                saveItemCommand.GetParameter(index++).Value = hasProgramAttributes.IsRepeat;
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                                saveItemCommand.GetParameter(index++).Value = null;
                                saveItemCommand.GetParameter(index++).Value = null;
                                saveItemCommand.GetParameter(index++).Value = null;
                                saveItemCommand.GetParameter(index++).Value = null;
                                saveItemCommand.GetParameter(index++).Value = null;
                                saveItemCommand.GetParameter(index++).Value = null;
                                saveItemCommand.GetParameter(index++).Value = null;
                                saveItemCommand.GetParameter(index++).Value = null;
                            }

                            saveItemCommand.GetParameter(index++).Value = item.CommunityRating;
                            saveItemCommand.GetParameter(index++).Value = item.CustomRating;

                            saveItemCommand.GetParameter(index++).Value = item.IndexNumber;
                            saveItemCommand.GetParameter(index++).Value = item.IsLocked;

                            saveItemCommand.GetParameter(index++).Value = item.Name;
                            saveItemCommand.GetParameter(index++).Value = item.OfficialRating;

                            saveItemCommand.GetParameter(index++).Value = item.MediaType;
                            saveItemCommand.GetParameter(index++).Value = item.Overview;
                            saveItemCommand.GetParameter(index++).Value = item.ParentIndexNumber;
                            saveItemCommand.GetParameter(index++).Value = item.PremiereDate;
                            saveItemCommand.GetParameter(index++).Value = item.ProductionYear;

                            if (item.ParentId == Guid.Empty)
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = item.ParentId;
                            }

                            saveItemCommand.GetParameter(index++).Value = string.Join("|", item.Genres.ToArray());
                            saveItemCommand.GetParameter(index++).Value = item.GetParentalRatingValue() ?? 0;
                            saveItemCommand.GetParameter(index++).Value = item.GetInheritedParentalRatingValue() ?? 0;

                            saveItemCommand.GetParameter(index++).Value = LatestSchemaVersion;
                            saveItemCommand.GetParameter(index++).Value = item.SortName;
                            saveItemCommand.GetParameter(index++).Value = item.RunTimeTicks;

                            saveItemCommand.GetParameter(index++).Value = item.OfficialRatingDescription;
                            saveItemCommand.GetParameter(index++).Value = item.HomePageUrl;
                            saveItemCommand.GetParameter(index++).Value = item.VoteCount;
                            saveItemCommand.GetParameter(index++).Value = item.DisplayMediaType;
                            saveItemCommand.GetParameter(index++).Value = item.DateCreated;
                            saveItemCommand.GetParameter(index++).Value = item.DateModified;

                            saveItemCommand.GetParameter(index++).Value = item.ForcedSortName;
                            saveItemCommand.GetParameter(index++).Value = item.IsOffline;
                            saveItemCommand.GetParameter(index++).Value = item.LocationType.ToString();

                            saveItemCommand.GetParameter(index++).Value = item.PreferredMetadataLanguage;
                            saveItemCommand.GetParameter(index++).Value = item.PreferredMetadataCountryCode;
                            saveItemCommand.GetParameter(index++).Value = item.IsHD;
                            saveItemCommand.GetParameter(index++).Value = item.ExternalEtag;

                            if (item.DateLastRefreshed == default(DateTime))
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = item.DateLastRefreshed;
                            }

                            if (item.DateLastSaved == default(DateTime))
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = item.DateLastSaved;
                            }

                            saveItemCommand.GetParameter(index++).Value = item.IsInMixedFolder;
                            saveItemCommand.GetParameter(index++).Value = string.Join("|", item.LockedFields.Select(i => i.ToString()).ToArray());
                            saveItemCommand.GetParameter(index++).Value = string.Join("|", item.Studios.ToArray());

                            if (item.Audio.HasValue)
                            {
                                saveItemCommand.GetParameter(index++).Value = item.Audio.Value.ToString();
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }

                            saveItemCommand.GetParameter(index++).Value = item.ServiceName;

                            if (item.Tags.Count > 0)
                            {
                                saveItemCommand.GetParameter(index++).Value = string.Join("|", item.Tags.ToArray());
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }

                            saveItemCommand.GetParameter(index++).Value = item.IsFolder;

                            saveItemCommand.GetParameter(index++).Value = item.GetBlockUnratedType().ToString();

                            var topParent = item.GetTopParent();
                            if (topParent != null)
                            {
                                //Logger.Debug("Item {0} has top parent {1}", item.Id, topParent.Id);
                                saveItemCommand.GetParameter(index++).Value = topParent.Id.ToString("N");
                            }
                            else
                            {
                                //Logger.Debug("Item {0} has null top parent", item.Id);
                                saveItemCommand.GetParameter(index++).Value = null;
                            }

                            var isByName = false;
                            var byName = item as IItemByName;
                            if (byName != null)
                            {
                                var dualAccess = item as IHasDualAccess;
                                isByName = dualAccess == null || dualAccess.IsAccessedByName;
                            }
                            saveItemCommand.GetParameter(index++).Value = isByName;

                            saveItemCommand.GetParameter(index++).Value = item.SourceType.ToString();

                            var trailer = item as Trailer;
                            if (trailer != null && trailer.TrailerTypes.Count > 0)
                            {
                                saveItemCommand.GetParameter(index++).Value = string.Join("|", trailer.TrailerTypes.Select(i => i.ToString()).ToArray());
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }

                            saveItemCommand.GetParameter(index++).Value = item.CriticRating;
                            saveItemCommand.GetParameter(index++).Value = item.CriticRatingSummary;

                            if (!item.DateModifiedDuringLastRefresh.HasValue || item.DateModifiedDuringLastRefresh.Value == default(DateTime))
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = item.DateModifiedDuringLastRefresh.Value;
                            }

                            var inheritedTags = item.GetInheritedTags();
                            if (inheritedTags.Count > 0)
                            {
                                saveItemCommand.GetParameter(index++).Value = string.Join("|", inheritedTags.ToArray());
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }

                            if (string.IsNullOrWhiteSpace(item.Name))
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = item.Name.RemoveDiacritics();
                            }

                            saveItemCommand.GetParameter(index++).Value = item.PresentationUniqueKey;
                            saveItemCommand.GetParameter(index++).Value = item.SlugName;
                            saveItemCommand.GetParameter(index++).Value = item.OriginalTitle;

                            var video = item as Video;
                            if (video != null)
                            {
                                saveItemCommand.GetParameter(index++).Value = video.PrimaryVersionId;
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }

                            var folder = item as Folder;
                            if (folder != null && folder.DateLastMediaAdded.HasValue)
                            {
                                saveItemCommand.GetParameter(index++).Value = folder.DateLastMediaAdded.Value;
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }

                            saveItemCommand.GetParameter(index++).Value = item.Album;

                            saveItemCommand.GetParameter(index++).Value = item.IsVirtualItem || (!item.IsFolder && item.LocationType == LocationType.Virtual);

                            var hasSeries = item as IHasSeries;
                            if (hasSeries != null)
                            {
                                saveItemCommand.GetParameter(index++).Value = hasSeries.SeriesName;
                            }
                            else
                            {
                                saveItemCommand.GetParameter(index++).Value = null;
                            }

                            saveItemCommand.Transaction = transaction;

                            saveItemCommand.ExecuteNonQuery();

                            if (item.SupportsAncestors)
                            {
                                UpdateAncestors(item.Id, item.GetAncestorIds().Distinct().ToList(), connection, transaction);
                            }

                            UpdateUserDataKeys(item.Id, item.GetUserDataKeys().Distinct(StringComparer.OrdinalIgnoreCase).ToList(), connection, transaction);
                            UpdateImages(item.Id, item.ImageInfos, connection, transaction);
                            UpdateProviderIds(item.Id, item.ProviderIds, connection, transaction);
                            UpdateItemValues(item.Id, GetItemValues(item), connection, transaction);
                        }
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
                }
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

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
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

            if (!reader.IsDBNull(51))
            {
                item.DateModifiedDuringLastRefresh = reader.GetDateTime(51).ToUniversalTime();
            }

            if (!reader.IsDBNull(52))
            {
                item.OriginalTitle = reader.GetString(52);
            }

            var video = item as Video;
            if (video != null)
            {
                if (!reader.IsDBNull(53))
                {
                    video.PrimaryVersionId = reader.GetString(53);
                }
            }

            var folder = item as Folder;
            if (folder != null && !reader.IsDBNull(54))
            {
                folder.DateLastMediaAdded = reader.GetDateTime(54).ToUniversalTime();
            }

            if (!reader.IsDBNull(55))
            {
                item.Album = reader.GetString(55);
            }

            if (!reader.IsDBNull(56))
            {
                item.CriticRating = reader.GetFloat(56);
            }

            if (!reader.IsDBNull(57))
            {
                item.CriticRatingSummary = reader.GetString(57);
            }

            if (!reader.IsDBNull(58))
            {
                item.IsVirtualItem = reader.GetBoolean(58);
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

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select StartPositionTicks,Name,ImagePath from " + ChaptersTableName + " where ItemId = @ItemId order by ChapterIndex asc";

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

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select StartPositionTicks,Name,ImagePath from " + ChaptersTableName + " where ItemId = @ItemId and ChapterIndex=@ChapterIndex";

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

            return chapter;
        }

        private void DeleteChapters(IDbConnection connection, IDbTransaction transaction, Guid id)
        {
            using (var deleteChaptersCommand = connection.CreateCommand())
            {
                deleteChaptersCommand.CommandText = "delete from " + ChaptersTableName + " where ItemId=@ItemId";
                deleteChaptersCommand.Parameters.Add(deleteChaptersCommand, "@ItemId");

                deleteChaptersCommand.GetParameter(0).Value = id;

                deleteChaptersCommand.Transaction = transaction;

                deleteChaptersCommand.ExecuteNonQuery();
            }
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

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                IDbTransaction transaction = null;

                try
                {
                    transaction = connection.BeginTransaction();

                    // First delete chapters
                    DeleteChapters(connection, transaction, id);

                    var index = 0;

                    if (chapters.Count > 0)
                    {
                        using (var saveChapterCommand = connection.CreateCommand())
                        {
                            saveChapterCommand.CommandText = "replace into " + ChaptersTableName + " (ItemId, ChapterIndex, StartPositionTicks, Name, ImagePath) values (@ItemId, @ChapterIndex, @StartPositionTicks, @Name, @ImagePath)";
                            saveChapterCommand.Parameters.Add(saveChapterCommand, "@ItemId");
                            saveChapterCommand.Parameters.Add(saveChapterCommand, "@ChapterIndex");
                            saveChapterCommand.Parameters.Add(saveChapterCommand, "@StartPositionTicks");
                            saveChapterCommand.Parameters.Add(saveChapterCommand, "@Name");
                            saveChapterCommand.Parameters.Add(saveChapterCommand, "@ImagePath");

                            if (chapters.Count > 1)
                            {
                                saveChapterCommand.Prepare();
                            }

                            foreach (var chapter in chapters)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                saveChapterCommand.GetParameter(0).Value = id;
                                saveChapterCommand.GetParameter(1).Value = index;
                                saveChapterCommand.GetParameter(2).Value = chapter.StartPositionTicks;
                                saveChapterCommand.GetParameter(3).Value = chapter.Name;
                                saveChapterCommand.GetParameter(4).Value = chapter.ImagePath;

                                saveChapterCommand.Transaction = transaction;

                                saveChapterCommand.ExecuteNonQuery();

                                index++;
                            }
                        }
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
                }
            }
        }

        private bool EnableJoinUserData(InternalItemsQuery query)
        {
            if (query.User == null)
            {
                return false;
            }

            if (query.SimilarTo != null)
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

                //// genres
                builder.Append("+ ((Select count(value) from ItemValues where ItemId=Guid and Type=2 and value in (select value from itemvalues where ItemId=@SimilarItemId and type=2)) * 10)");

                //// tags
                builder.Append("+ ((Select count(value) from ItemValues where ItemId=Guid and Type=4 and value in (select value from itemvalues where ItemId=@SimilarItemId and type=4)) * 10)");

                builder.Append("+ ((Select count(value) from ItemValues where ItemId=Guid and Type=5 and value in (select value from itemvalues where ItemId=@SimilarItemId and type=5)) * 10)");

                builder.Append("+ ((Select count(value) from ItemValues where ItemId=Guid and Type=3 and value in (select value from itemvalues where ItemId=@SimilarItemId and type=3)) * 3)");

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

            return " left join UserData on (select UserDataKey from UserDataKeys where ItemId=Guid order by Priority LIMIT 1)=UserData.Key";
        }

        public IEnumerable<BaseItem> GetItemList(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            var now = DateTime.UtcNow;

            using (var connection = CreateConnection(true).Result)
            {
                if (EnableJoinUserData(query))
                {
                    AttachUserDataDb(connection);
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, _retriveItemColumns, cmd)) + " from TypedBaseItems";
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

                    if (EnableGroupByPresentationUniqueKey(query))
                    {
                        cmd.CommandText += " Group by PresentationUniqueKey";
                    }

                    cmd.CommandText += GetOrderByText(query);

                    if (query.Limit.HasValue || query.StartIndex.HasValue)
                    {
                        var limit = query.Limit ?? int.MaxValue;

                        cmd.CommandText += " LIMIT " + limit.ToString(CultureInfo.InvariantCulture);

                        if (query.StartIndex.HasValue)
                        {
                            cmd.CommandText += " OFFSET " + query.StartIndex.Value.ToString(CultureInfo.InvariantCulture);
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
                                yield return item;
                            }
                        }
                    }
                }
            }
        }

        private void LogQueryTime(string methodName, IDbCommand cmd, DateTime startDate)
        {
            var elapsed = (DateTime.UtcNow - startDate).TotalMilliseconds;

            var slowThreshold = 1000;

#if DEBUG
            slowThreshold = 30;
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

            var now = DateTime.UtcNow;

            using (var connection = CreateConnection(true).Result)
            {
                if (EnableJoinUserData(query))
                {
                    AttachUserDataDb(connection);
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, _retriveItemColumns, cmd)) + " from TypedBaseItems";
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

                    if (EnableGroupByPresentationUniqueKey(query))
                    {
                        cmd.CommandText += " Group by PresentationUniqueKey";
                    }

                    cmd.CommandText += GetOrderByText(query);

                    if (query.Limit.HasValue || query.StartIndex.HasValue)
                    {
                        var limit = query.Limit ?? int.MaxValue;

                        cmd.CommandText += " LIMIT " + limit.ToString(CultureInfo.InvariantCulture);

                        if (query.StartIndex.HasValue)
                        {
                            cmd.CommandText += " OFFSET " + query.StartIndex.Value.ToString(CultureInfo.InvariantCulture);
                        }
                    }

                    if (EnableGroupByPresentationUniqueKey(query))
                    {
                        cmd.CommandText += "; select count (distinct PresentationUniqueKey) from TypedBaseItems";
                    }
                    else
                    {
                        cmd.CommandText += "; select count (guid) from TypedBaseItems";
                    }

                    cmd.CommandText += GetJoinUserDataText(query);
                    cmd.CommandText += whereTextWithoutPaging;

                    var list = new List<BaseItem>();
                    var count = 0;

                    using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        LogQueryTime("GetItems", cmd, now);

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

                    return new QueryResult<BaseItem>()
                    {
                        Items = list.ToArray(),
                        TotalRecordCount = count
                    };
                }
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
                        query.SortBy = new[] { "SimilarityScore", "IsPlayed", "Random" };
                    }
                    else
                    {
                        query.SortBy = new[] { "SimilarityScore", "Random" };
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
                var columnMap = MapOrderByField(i);
                var columnAscending = isAscending;
                if (columnMap.Item2)
                {
                    columnAscending = !columnAscending;
                }

                var sortOrder = columnAscending ? "ASC" : "DESC";

                return columnMap.Item1 + " " + sortOrder;
            }).ToArray());
        }

        private Tuple<string, bool> MapOrderByField(string name)
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
                return new Tuple<string, bool>("(select value from itemvalues where ItemId=Guid and Type=0 LIMIT 1)", false);
            }
            if (string.Equals(name, ItemSortBy.AlbumArtist, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("(select value from itemvalues where ItemId=Guid and Type=1 LIMIT 1)", false);
            }
            if (string.Equals(name, ItemSortBy.OfficialRating, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("ParentalRatingValue", false);
            }
            if (string.Equals(name, ItemSortBy.Studio, StringComparison.OrdinalIgnoreCase))
            {
                return new Tuple<string, bool>("(select value from itemvalues where ItemId=Guid and Type=3 LIMIT 1)", false);
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

            using (var connection = CreateConnection(true).Result)
            {
                if (EnableJoinUserData(query))
                {
                    AttachUserDataDb(connection);
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "guid" }, cmd)) + " from TypedBaseItems";
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

                    if (EnableGroupByPresentationUniqueKey(query))
                    {
                        cmd.CommandText += " Group by PresentationUniqueKey";
                    }

                    cmd.CommandText += GetOrderByText(query);

                    if (query.Limit.HasValue || query.StartIndex.HasValue)
                    {
                        var limit = query.Limit ?? int.MaxValue;

                        cmd.CommandText += " LIMIT " + limit.ToString(CultureInfo.InvariantCulture);

                        if (query.StartIndex.HasValue)
                        {
                            cmd.CommandText += " OFFSET " + query.StartIndex.Value.ToString(CultureInfo.InvariantCulture);
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
        }

        public QueryResult<Tuple<Guid, string>> GetItemIdsWithPath(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
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

                    if (EnableGroupByPresentationUniqueKey(query))
                    {
                        cmd.CommandText += " Group by PresentationUniqueKey";
                    }

                    cmd.CommandText += GetOrderByText(query);

                    if (query.Limit.HasValue || query.StartIndex.HasValue)
                    {
                        var limit = query.Limit ?? int.MaxValue;

                        cmd.CommandText += " LIMIT " + limit.ToString(CultureInfo.InvariantCulture);

                        if (query.StartIndex.HasValue)
                        {
                            cmd.CommandText += " OFFSET " + query.StartIndex.Value.ToString(CultureInfo.InvariantCulture);
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
        }

        public QueryResult<Guid> GetItemIds(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            var now = DateTime.UtcNow;

            using (var connection = CreateConnection(true).Result)
            {
                if (EnableJoinUserData(query))
                {
                    AttachUserDataDb(connection);
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select " + string.Join(",", GetFinalColumnsToSelect(query, new[] { "guid" }, cmd)) + " from TypedBaseItems";

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

                    if (EnableGroupByPresentationUniqueKey(query))
                    {
                        cmd.CommandText += " Group by PresentationUniqueKey";
                    }

                    cmd.CommandText += GetOrderByText(query);

                    if (query.Limit.HasValue || query.StartIndex.HasValue)
                    {
                        var limit = query.Limit ?? int.MaxValue;

                        cmd.CommandText += " LIMIT " + limit.ToString(CultureInfo.InvariantCulture);

                        if (query.StartIndex.HasValue)
                        {
                            cmd.CommandText += " OFFSET " + query.StartIndex.Value.ToString(CultureInfo.InvariantCulture);
                        }
                    }

                    if (EnableGroupByPresentationUniqueKey(query))
                    {
                        cmd.CommandText += "; select count (distinct PresentationUniqueKey) from TypedBaseItems";
                    }
                    else
                    {
                        cmd.CommandText += "; select count (guid) from TypedBaseItems";
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
        }

        private List<string> GetWhereClauses(InternalItemsQuery query, IDbCommand cmd)
        {
            var whereClauses = new List<string>();

            if (EnableJoinUserData(query))
            {
                whereClauses.Add("(UserId is null or UserId=@UserId)");
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

            var includeTypes = query.IncludeItemTypes.SelectMany(MapIncludeItemTypes).ToArray();
            if (includeTypes.Length == 1)
            {
                whereClauses.Add("type=@type");
                cmd.Parameters.Add(cmd, "@type", DbType.String).Value = includeTypes[0];
            }
            else if (includeTypes.Length > 1)
            {
                var inClause = string.Join(",", includeTypes.Select(i => "'" + i + "'").ToArray());
                whereClauses.Add(string.Format("type in ({0})", inClause));
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

            if (!string.IsNullOrWhiteSpace(query.Name))
            {
                whereClauses.Add("CleanName=@Name");
                cmd.Parameters.Add(cmd, "@Name", DbType.String).Value = query.Name.RemoveDiacritics();
            }

            if (!string.IsNullOrWhiteSpace(query.NameContains))
            {
                whereClauses.Add("CleanName like @NameContains");
                cmd.Parameters.Add(cmd, "@NameContains", DbType.String).Value = "%" + query.NameContains.RemoveDiacritics() + "%";
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
                    clauses.Add("@ArtistName" + index + " in (select value from itemvalues where ItemId=Guid and Type <= 1)");
                    cmd.Parameters.Add(cmd, "@ArtistName" + index, DbType.String).Value = artist;
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
                    clauses.Add("@Genre" + index + " in (select value from itemvalues where ItemId=Guid and Type=2)");
                    cmd.Parameters.Add(cmd, "@Genre" + index, DbType.String).Value = item;
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
                    clauses.Add("@Tag" + index + " in (select value from itemvalues where ItemId=Guid and Type=4)");
                    cmd.Parameters.Add(cmd, "@Tag" + index, DbType.String).Value = item;
                    index++;
                }
                var clause = "(" + string.Join(" OR ", clauses.ToArray()) + ")";
                whereClauses.Add(clause);
            }

            if (query.Studios.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in query.Studios)
                {
                    clauses.Add("@Studio" + index + " in (select value from itemvalues where ItemId=Guid and Type=3)");
                    cmd.Parameters.Add(cmd, "@Studio" + index, DbType.String).Value = item;
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
                    clauses.Add("@Keyword" + index + " in (select value from itemvalues where ItemId=Guid and Type=5)");
                    cmd.Parameters.Add(cmd, "@Keyword" + index, DbType.String).Value = item;
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
                whereClauses.Add("IsVirtualItem=@IsVirtualItem");
                cmd.Parameters.Add(cmd, "@IsVirtualItem", DbType.Boolean).Value = query.IsVirtualItem.Value;
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
            await UpdateInheritedParentalRating(cancellationToken).ConfigureAwait(false);
            await UpdateInheritedTags(cancellationToken).ConfigureAwait(false);
        }

        private async Task UpdateInheritedTags(CancellationToken cancellationToken)
        {
            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                var newValues = new List<Tuple<Guid, string>>();

                using (var cmd = connection.CreateCommand())
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

                IDbTransaction transaction = null;

                try
                {
                    transaction = connection.BeginTransaction();

                    using (var updateInheritedTagsCommand = connection.CreateCommand())
                    {
                        updateInheritedTagsCommand.CommandText = "Update TypedBaseItems set InheritedTags=@InheritedTags where Guid=@Guid";
                        updateInheritedTagsCommand.Parameters.Add(updateInheritedTagsCommand, "@Guid");
                        updateInheritedTagsCommand.Parameters.Add(updateInheritedTagsCommand, "@InheritedTags");

                        if (newValues.Count > 1)
                        {
                            updateInheritedTagsCommand.Prepare();
                        }

                        foreach (var item in newValues)
                        {
                            updateInheritedTagsCommand.GetParameter(0).Value = item.Item1;
                            updateInheritedTagsCommand.GetParameter(1).Value = item.Item2;

                            updateInheritedTagsCommand.Transaction = transaction;
                            updateInheritedTagsCommand.ExecuteNonQuery();
                        }
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
                }
            }
        }

        private async Task UpdateInheritedParentalRating(CancellationToken cancellationToken)
        {
            var newValues = new List<Tuple<Guid, int>>();

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select Guid,InheritedParentalRatingValue,(select Max(ParentalRatingValue, (select COALESCE(MAX(ParentalRatingValue),0) from TypedBaseItems where guid in (Select AncestorId from AncestorIds where ItemId=Outer.guid)))) as NewInheritedParentalRatingValue from typedbaseitems as Outer where InheritedParentalRatingValue <> NewInheritedParentalRatingValue";

                    using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                    {
                        while (reader.Read())
                        {
                            var id = reader.GetGuid(0);
                            var newValue = reader.GetInt32(2);

                            newValues.Add(new Tuple<Guid, int>(id, newValue));
                        }
                    }
                }

                Logger.Debug("UpdateInheritedParentalRatings - {0} rows", newValues.Count);
                if (newValues.Count == 0)
                {
                    return;
                }

                using (var updateInheritedRatingCommand = connection.CreateCommand())
                {
                    updateInheritedRatingCommand.CommandText = "Update TypedBaseItems set InheritedParentalRatingValue=@InheritedParentalRatingValue where Guid=@Guid";
                    updateInheritedRatingCommand.Parameters.Add(updateInheritedRatingCommand, "@Guid");
                    updateInheritedRatingCommand.Parameters.Add(updateInheritedRatingCommand, "@InheritedParentalRatingValue");
                    updateInheritedRatingCommand.Prepare();

                    IDbTransaction transaction = null;

                    try
                    {
                        transaction = connection.BeginTransaction();

                        foreach (var item in newValues)
                        {
                            updateInheritedRatingCommand.GetParameter(0).Value = item.Item1;
                            updateInheritedRatingCommand.GetParameter(1).Value = item.Item2;

                            updateInheritedRatingCommand.Transaction = transaction;
                            updateInheritedRatingCommand.ExecuteNonQuery();
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

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                IDbTransaction transaction = null;

                try
                {
                    transaction = connection.BeginTransaction();

                    // Delete people
                    DeletePeople(connection, transaction, id);
                    DeleteChapters(connection, transaction, id);
                    DeleteMediaStreams(connection, transaction, id);

                    // Delete ancestors
                    DeleteAncestors(connection, transaction, id);

                    // Delete user data keys
                    DeleteUserDataKeys(connection, transaction, id);

                    // Delete item values
                    DeleteItemValues(connection, transaction, id);

                    // Delete provider ids
                    DeleteProviderIds(connection, transaction, id);

                    DeleteImages(connection, transaction, id);

                    // Delete the item
                    using (var deleteItemCommand = connection.CreateCommand())
                    {
                        deleteItemCommand.CommandText = "delete from TypedBaseItems where guid=@Id";
                        deleteItemCommand.Parameters.Add(deleteItemCommand, "@Id");

                        deleteItemCommand.GetParameter(0).Value = id;
                        deleteItemCommand.Transaction = transaction;
                        deleteItemCommand.ExecuteNonQuery();
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
                }
            }
        }

        public List<string> GetPeopleNames(InternalPeopleQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
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
        }

        public List<PersonInfo> GetPeople(InternalPeopleQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
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

            return whereClauses;
        }

        private void DeleteAncestors(IDbConnection connection, IDbTransaction transaction, Guid id)
        {
            using (var deleteAncestorsCommand = connection.CreateCommand())
            {
                deleteAncestorsCommand.CommandText = "delete from AncestorIds where ItemId=@Id";
                deleteAncestorsCommand.Parameters.Add(deleteAncestorsCommand, "@Id");

                deleteAncestorsCommand.GetParameter(0).Value = id;
                deleteAncestorsCommand.Transaction = transaction;

                deleteAncestorsCommand.ExecuteNonQuery();
            }
        }

        private void UpdateAncestors(Guid itemId, List<Guid> ancestorIds, IDbConnection connection, IDbTransaction transaction)
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
            DeleteAncestors(connection, transaction, itemId);

            if (ancestorIds.Count > 0)
            {
                using (var saveAncestorCommand = connection.CreateCommand())
                {
                    saveAncestorCommand.CommandText = "insert into AncestorIds (ItemId, AncestorId, AncestorIdText) values (@ItemId, @AncestorId, @AncestorIdText)";
                    saveAncestorCommand.Parameters.Add(saveAncestorCommand, "@ItemId");
                    saveAncestorCommand.Parameters.Add(saveAncestorCommand, "@AncestorId");
                    saveAncestorCommand.Parameters.Add(saveAncestorCommand, "@AncestorIdText");

                    if (ancestorIds.Count > 1)
                    {
                        saveAncestorCommand.Prepare();
                    }

                    foreach (var ancestorId in ancestorIds)
                    {
                        saveAncestorCommand.GetParameter(0).Value = itemId;
                        saveAncestorCommand.GetParameter(1).Value = ancestorId;
                        saveAncestorCommand.GetParameter(2).Value = ancestorId.ToString("N");

                        saveAncestorCommand.Transaction = transaction;

                        saveAncestorCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private List<Tuple<int, string>> GetItemValues(BaseItem item)
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

        private void DeleteImages(IDbConnection connection, IDbTransaction transaction, Guid id)
        {
            using (var deleteImagesCommand = connection.CreateCommand())
            {
                deleteImagesCommand.CommandText = "delete from Images where ItemId=@Id";
                deleteImagesCommand.Parameters.Add(deleteImagesCommand, "@Id");

                deleteImagesCommand.GetParameter(0).Value = id;
                deleteImagesCommand.Transaction = transaction;
                deleteImagesCommand.ExecuteNonQuery();
            }
        }

        private void UpdateImages(Guid itemId, List<ItemImageInfo> images, IDbConnection connection, IDbTransaction transaction)
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
            DeleteImages(connection, transaction, itemId);

            if (images.Count > 0)
            {
                using (var saveImagesCommand = connection.CreateCommand())
                {
                    var index = 0;

                    saveImagesCommand.CommandText = "insert into Images (ItemId, ImageType, Path, DateModified, IsPlaceHolder, SortOrder) values (@ItemId, @ImageType, @Path, @DateModified, @IsPlaceHolder, @SortOrder)";
                    saveImagesCommand.Parameters.Add(saveImagesCommand, "@ItemId");
                    saveImagesCommand.Parameters.Add(saveImagesCommand, "@ImageType");
                    saveImagesCommand.Parameters.Add(saveImagesCommand, "@Path");
                    saveImagesCommand.Parameters.Add(saveImagesCommand, "@DateModified");
                    saveImagesCommand.Parameters.Add(saveImagesCommand, "@IsPlaceHolder");
                    saveImagesCommand.Parameters.Add(saveImagesCommand, "@SortOrder");

                    if (images.Count > 1)
                    {
                        saveImagesCommand.Prepare();
                    }

                    foreach (var image in images)
                    {
                        saveImagesCommand.GetParameter(0).Value = itemId;
                        saveImagesCommand.GetParameter(1).Value = image.Type;
                        saveImagesCommand.GetParameter(2).Value = image.Path;

                        if (image.DateModified == default(DateTime))
                        {
                            saveImagesCommand.GetParameter(3).Value = null;
                        }
                        else
                        {
                            saveImagesCommand.GetParameter(3).Value = image.DateModified;
                        }

                        saveImagesCommand.GetParameter(4).Value = image.IsPlaceholder;
                        saveImagesCommand.GetParameter(5).Value = index;

                        saveImagesCommand.Transaction = transaction;

                        saveImagesCommand.ExecuteNonQuery();
                        index++;
                    }
                }
            }
        }

        private void DeleteProviderIds(IDbConnection connection, IDbTransaction transaction, Guid itemId)
        {
            using (var deleteProviderIdsCommand = connection.CreateCommand())
            {
                // provider ids
                deleteProviderIdsCommand.CommandText = "delete from ProviderIds where ItemId=@Id";
                deleteProviderIdsCommand.Parameters.Add(deleteProviderIdsCommand, "@Id");

                deleteProviderIdsCommand.GetParameter(0).Value = itemId;
                deleteProviderIdsCommand.Transaction = transaction;

                deleteProviderIdsCommand.ExecuteNonQuery();
            }
        }

        private void UpdateProviderIds(Guid itemId, Dictionary<string, string> values, IDbConnection connection, IDbTransaction transaction)
        {
            if (itemId == Guid.Empty)
            {
                throw new ArgumentNullException("itemId");
            }

            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            CheckDisposed();

            // First delete 
            DeleteProviderIds(connection, transaction, itemId);

            if (values.Count > 0)
            {
                using (var saveProviderIdsCommand = connection.CreateCommand())
                {
                    saveProviderIdsCommand.CommandText = "insert into ProviderIds (ItemId, Name, Value) values (@ItemId, @Name, @Value)";
                    saveProviderIdsCommand.Parameters.Add(saveProviderIdsCommand, "@ItemId");
                    saveProviderIdsCommand.Parameters.Add(saveProviderIdsCommand, "@Name");
                    saveProviderIdsCommand.Parameters.Add(saveProviderIdsCommand, "@Value");

                    if (values.Count > 1)
                    {
                        saveProviderIdsCommand.Prepare();
                    }

                    foreach (var pair in values)
                    {
                        saveProviderIdsCommand.GetParameter(0).Value = itemId;
                        saveProviderIdsCommand.GetParameter(1).Value = pair.Key;
                        saveProviderIdsCommand.GetParameter(2).Value = pair.Value;
                        saveProviderIdsCommand.Transaction = transaction;

                        saveProviderIdsCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private void DeleteItemValues(IDbConnection connection, IDbTransaction transaction, Guid itemId)
        {
            using (var deleteItemValuesCommand = connection.CreateCommand())
            {
                deleteItemValuesCommand.CommandText = "delete from ItemValues where ItemId=@Id";
                deleteItemValuesCommand.Parameters.Add(deleteItemValuesCommand, "@Id");

                // First delete 
                deleteItemValuesCommand.GetParameter(0).Value = itemId;
                deleteItemValuesCommand.Transaction = transaction;

                deleteItemValuesCommand.ExecuteNonQuery();
            }
        }

        private void UpdateItemValues(Guid itemId, List<Tuple<int, string>> values, IDbConnection connection, IDbTransaction transaction)
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
            DeleteItemValues(connection, transaction, itemId);

            if (values.Count > 0)
            {
                using (var saveItemValuesCommand = connection.CreateCommand())
                {
                    saveItemValuesCommand.CommandText = "insert into ItemValues (ItemId, Type, Value) values (@ItemId, @Type, @Value)";
                    saveItemValuesCommand.Parameters.Add(saveItemValuesCommand, "@ItemId");
                    saveItemValuesCommand.Parameters.Add(saveItemValuesCommand, "@Type");
                    saveItemValuesCommand.Parameters.Add(saveItemValuesCommand, "@Value");

                    if (values.Count > 1)
                    {
                        saveItemValuesCommand.Prepare();
                    }

                    foreach (var pair in values)
                    {
                        saveItemValuesCommand.GetParameter(0).Value = itemId;
                        saveItemValuesCommand.GetParameter(1).Value = pair.Item1;
                        saveItemValuesCommand.GetParameter(2).Value = pair.Item2;
                        saveItemValuesCommand.Transaction = transaction;

                        saveItemValuesCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private void DeleteUserDataKeys(IDbConnection connection, IDbTransaction transaction, Guid itemId)
        {
            using (var deleteUserDataKeysCommand = connection.CreateCommand())
            {
                // user data
                deleteUserDataKeysCommand.CommandText = "delete from UserDataKeys where ItemId=@Id";
                deleteUserDataKeysCommand.Parameters.Add(deleteUserDataKeysCommand, "@Id");

                deleteUserDataKeysCommand.GetParameter(0).Value = itemId;
                deleteUserDataKeysCommand.Transaction = transaction;

                deleteUserDataKeysCommand.ExecuteNonQuery();
            }
        }

        private void UpdateUserDataKeys(Guid itemId, List<string> keys, IDbConnection connection, IDbTransaction transaction)
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
            DeleteUserDataKeys(connection, transaction, itemId);

            var index = 0;

            if (keys.Count > 0)
            {
                using (var saveUserDataKeysCommand = connection.CreateCommand())
                {
                    saveUserDataKeysCommand.CommandText = "insert into UserDataKeys (ItemId, UserDataKey, Priority) values (@ItemId, @UserDataKey, @Priority)";
                    saveUserDataKeysCommand.Parameters.Add(saveUserDataKeysCommand, "@ItemId");
                    saveUserDataKeysCommand.Parameters.Add(saveUserDataKeysCommand, "@UserDataKey");
                    saveUserDataKeysCommand.Parameters.Add(saveUserDataKeysCommand, "@Priority");

                    if (keys.Count > 1)
                    {
                        saveUserDataKeysCommand.Prepare();
                    }

                    foreach (var key in keys)
                    {
                        saveUserDataKeysCommand.GetParameter(0).Value = itemId;
                        saveUserDataKeysCommand.GetParameter(1).Value = key;
                        saveUserDataKeysCommand.GetParameter(2).Value = index;
                        index++;
                        saveUserDataKeysCommand.Transaction = transaction;

                        saveUserDataKeysCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private void DeletePeople(IDbConnection connection, IDbTransaction transaction, Guid id)
        {
            using (var deletePeopleCommand = connection.CreateCommand())
            {
                deletePeopleCommand.CommandText = "delete from People where ItemId=@Id";
                deletePeopleCommand.Parameters.Add(deletePeopleCommand, "@Id");


                deletePeopleCommand.GetParameter(0).Value = id;
                deletePeopleCommand.Transaction = transaction;

                deletePeopleCommand.ExecuteNonQuery();
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

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                IDbTransaction transaction = null;

                try
                {
                    transaction = connection.BeginTransaction();

                    // First delete 
                    DeletePeople(connection, transaction, itemId);

                    var listIndex = 0;

                    if (people.Count > 0)
                    {
                        using (var savePersonCommand = connection.CreateCommand())
                        {
                            savePersonCommand.CommandText = "insert into People (ItemId, Name, Role, PersonType, SortOrder, ListOrder) values (@ItemId, @Name, @Role, @PersonType, @SortOrder, @ListOrder)";
                            savePersonCommand.Parameters.Add(savePersonCommand, "@ItemId");
                            savePersonCommand.Parameters.Add(savePersonCommand, "@Name");
                            savePersonCommand.Parameters.Add(savePersonCommand, "@Role");
                            savePersonCommand.Parameters.Add(savePersonCommand, "@PersonType");
                            savePersonCommand.Parameters.Add(savePersonCommand, "@SortOrder");
                            savePersonCommand.Parameters.Add(savePersonCommand, "@ListOrder");

                            if (people.Count > 1)
                            {
                                savePersonCommand.Prepare();
                            }

                            foreach (var person in people)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                savePersonCommand.GetParameter(0).Value = itemId;
                                savePersonCommand.GetParameter(1).Value = person.Name;
                                savePersonCommand.GetParameter(2).Value = person.Role;
                                savePersonCommand.GetParameter(3).Value = person.Type;
                                savePersonCommand.GetParameter(4).Value = person.SortOrder;
                                savePersonCommand.GetParameter(5).Value = listIndex;

                                savePersonCommand.Transaction = transaction;

                                savePersonCommand.ExecuteNonQuery();
                                listIndex++;
                            }
                        }
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
                }
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

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
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
        }

        private void DeleteMediaStreams(IDbConnection connection, IDbTransaction transaction, Guid id)
        {
            using (var deleteStreamsCommand = connection.CreateCommand())
            {
                // MediaStreams
                deleteStreamsCommand.CommandText = "delete from mediastreams where ItemId=@ItemId";
                deleteStreamsCommand.Parameters.Add(deleteStreamsCommand, "@ItemId");

                deleteStreamsCommand.GetParameter(0).Value = id;

                deleteStreamsCommand.Transaction = transaction;

                deleteStreamsCommand.ExecuteNonQuery();
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

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                IDbTransaction transaction = null;

                try
                {
                    transaction = connection.BeginTransaction();

                    // First delete 
                    DeleteMediaStreams(connection, transaction, id);

                    if (streams.Count > 0)
                    {
                        using (var saveStreamCommand = connection.CreateCommand())
                        {
                            saveStreamCommand.CommandText = string.Format("replace into mediastreams ({0}) values ({1})",
                                string.Join(",", _mediaStreamSaveColumns),
                                string.Join(",", _mediaStreamSaveColumns.Select(i => "@" + i).ToArray()));

                            foreach (var col in _mediaStreamSaveColumns)
                            {
                                saveStreamCommand.Parameters.Add(saveStreamCommand, "@" + col);
                            }

                            if (streams.Count > 1)
                            {
                                saveStreamCommand.Prepare();
                            }

                            foreach (var stream in streams)
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                var index = 0;

                                saveStreamCommand.GetParameter(index++).Value = id;
                                saveStreamCommand.GetParameter(index++).Value = stream.Index;
                                saveStreamCommand.GetParameter(index++).Value = stream.Type.ToString();
                                saveStreamCommand.GetParameter(index++).Value = stream.Codec;
                                saveStreamCommand.GetParameter(index++).Value = stream.Language;
                                saveStreamCommand.GetParameter(index++).Value = stream.ChannelLayout;
                                saveStreamCommand.GetParameter(index++).Value = stream.Profile;
                                saveStreamCommand.GetParameter(index++).Value = stream.AspectRatio;
                                saveStreamCommand.GetParameter(index++).Value = stream.Path;

                                saveStreamCommand.GetParameter(index++).Value = stream.IsInterlaced;

                                saveStreamCommand.GetParameter(index++).Value = stream.BitRate;
                                saveStreamCommand.GetParameter(index++).Value = stream.Channels;
                                saveStreamCommand.GetParameter(index++).Value = stream.SampleRate;

                                saveStreamCommand.GetParameter(index++).Value = stream.IsDefault;
                                saveStreamCommand.GetParameter(index++).Value = stream.IsForced;
                                saveStreamCommand.GetParameter(index++).Value = stream.IsExternal;

                                saveStreamCommand.GetParameter(index++).Value = stream.Width;
                                saveStreamCommand.GetParameter(index++).Value = stream.Height;
                                saveStreamCommand.GetParameter(index++).Value = stream.AverageFrameRate;
                                saveStreamCommand.GetParameter(index++).Value = stream.RealFrameRate;
                                saveStreamCommand.GetParameter(index++).Value = stream.Level;
                                saveStreamCommand.GetParameter(index++).Value = stream.PixelFormat;
                                saveStreamCommand.GetParameter(index++).Value = stream.BitDepth;
                                saveStreamCommand.GetParameter(index++).Value = stream.IsAnamorphic;
                                saveStreamCommand.GetParameter(index++).Value = stream.RefFrames;

                                saveStreamCommand.GetParameter(index++).Value = stream.CodecTag;
                                saveStreamCommand.GetParameter(index++).Value = stream.Comment;
                                saveStreamCommand.GetParameter(index++).Value = stream.NalLengthSize;
                                saveStreamCommand.GetParameter(index++).Value = stream.IsAVC;
                                saveStreamCommand.GetParameter(index++).Value = stream.Title;

                                saveStreamCommand.GetParameter(index++).Value = stream.TimeBase;
                                saveStreamCommand.GetParameter(index++).Value = stream.CodecTimeBase;

                                saveStreamCommand.Transaction = transaction;
                                saveStreamCommand.ExecuteNonQuery();
                            }
                        }
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
                }
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