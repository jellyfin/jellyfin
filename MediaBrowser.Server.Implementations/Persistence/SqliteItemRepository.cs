using MediaBrowser.Common.Configuration;
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
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Playlists;
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
        private readonly IApplicationPaths _appPaths;

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

        private IDbCommand _updateInheritedRatingCommand;

        private const int LatestSchemaVersion = 55;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteItemRepository"/> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logManager">The log manager.</param>
        /// <exception cref="System.ArgumentNullException">
        /// appPaths
        /// or
        /// jsonSerializer
        /// </exception>
        public SqliteItemRepository(IApplicationPaths appPaths, IJsonSerializer jsonSerializer, ILogManager logManager)
            : base(logManager)
        {
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }

            _appPaths = appPaths;
            _jsonSerializer = jsonSerializer;

            _criticReviewsPath = Path.Combine(_appPaths.DataPath, "critic-reviews");
        }

        private const string ChaptersTableName = "Chapters2";

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "library.db");

            _connection = await SqliteExtensions.ConnectToDb(dbFile, Logger).ConfigureAwait(false);

            var createMediaStreamsTableCommand
               = "create table if not exists mediastreams (ItemId GUID, StreamIndex INT, StreamType TEXT, Codec TEXT, Language TEXT, ChannelLayout TEXT, Profile TEXT, AspectRatio TEXT, Path TEXT, IsInterlaced BIT, BitRate INT NULL, Channels INT NULL, SampleRate INT NULL, IsDefault BIT, IsForced BIT, IsExternal BIT, Height INT NULL, Width INT NULL, AverageFrameRate FLOAT NULL, RealFrameRate FLOAT NULL, Level FLOAT NULL, PixelFormat TEXT, BitDepth INT NULL, IsAnamorphic BIT NULL, RefFrames INT NULL, IsCabac BIT NULL, CodecTag TEXT NULL, Comment TEXT NULL, PRIMARY KEY (ItemId, StreamIndex))";

            string[] queries = {

                                "create table if not exists TypedBaseItems (guid GUID primary key, type TEXT, data BLOB, ParentId GUID, Path TEXT)",
                                "create index if not exists idx_TypedBaseItems on TypedBaseItems(guid)",
                                "create index if not exists idx_PathTypedBaseItems on TypedBaseItems(Path)",
                                "create index if not exists idx_ParentIdTypedBaseItems on TypedBaseItems(ParentId)",

                                "create table if not exists AncestorIds (ItemId GUID, AncestorId GUID, AncestorIdText TEXT, PRIMARY KEY (ItemId, AncestorId))",
                                "create index if not exists idx_AncestorIds1 on AncestorIds(AncestorId)",
                                "create index if not exists idx_AncestorIds2 on AncestorIds(AncestorIdText)",
                                
                                "create table if not exists People (ItemId GUID, Name TEXT NOT NULL, Role TEXT, PersonType TEXT, SortOrder int, ListOrder int)",
                                "create index if not exists idxPeopleItemId on People(ItemId)",
                                "create index if not exists idxPeopleName on People(Name)",

                                "create table if not exists "+ChaptersTableName+" (ItemId GUID, ChapterIndex INT, StartPositionTicks BIGINT, Name TEXT, ImagePath TEXT, PRIMARY KEY (ItemId, ChapterIndex))",
                                "create index if not exists idx_"+ChaptersTableName+" on "+ChaptersTableName+"(ItemId, ChapterIndex)",

                                createMediaStreamsTableCommand,
                                "create index if not exists idx_mediastreams on mediastreams(ItemId, StreamIndex)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
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
            _connection.AddColumn(Logger, "TypedBaseItems", "ParentalRatingValue", "INT");
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

            PrepareStatements();

            new MediaStreamColumns(_connection, Logger).AddColumns();

            var chapterDbFile = Path.Combine(_appPaths.DataPath, "chapters.db");
            if (File.Exists(chapterDbFile))
            {
                MigrateChapters(chapterDbFile);
            }

            var mediaStreamsDbFile = Path.Combine(_appPaths.DataPath, "mediainfo.db");
            if (File.Exists(mediaStreamsDbFile))
            {
                MigrateMediaStreams(mediaStreamsDbFile);
            }
        }

        private void MigrateMediaStreams(string file)
        {
            try
            {
                var backupFile = file + ".bak";
                File.Copy(file, backupFile, true);
                SqliteExtensions.Attach(_connection, backupFile, "MediaInfoOld");

                var columns = string.Join(",", _mediaStreamSaveColumns);

                string[] queries = {
                                "REPLACE INTO mediastreams("+columns+") SELECT "+columns+" FROM MediaInfoOld.mediastreams;"
                               };

                _connection.RunQueries(queries, Logger);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error migrating media info database", ex);
            }
            finally
            {
                TryDeleteFile(file);
            }
        }

        private void MigrateChapters(string file)
        {
            try
            {
                var backupFile = file + ".bak";
                File.Copy(file, backupFile, true);
                SqliteExtensions.Attach(_connection, backupFile, "ChaptersOld");

                string[] queries = {
                                "REPLACE INTO "+ChaptersTableName+"(ItemId, ChapterIndex, StartPositionTicks, Name, ImagePath) SELECT ItemId, ChapterIndex, StartPositionTicks, Name, ImagePath FROM ChaptersOld.Chapters;"
                               };

                _connection.RunQueries(queries, Logger);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error migrating chapter database", ex);
            }
            finally
            {
                TryDeleteFile(file);
            }
        }

        private void TryDeleteFile(string file)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error deleting file {0}", ex, file);
            }
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
            "TrailerTypes"
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
            "IsCabac",
            "CodecTag",
            "Comment"
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
                "CriticRatingSummary"
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

            _updateInheritedRatingCommand = _connection.CreateCommand();
            _updateInheritedRatingCommand.CommandText = "Update TypedBaseItems set InheritedParentalRatingValue=@InheritedParentalRatingValue where Guid=@Guid";
            _updateInheritedRatingCommand.Parameters.Add(_updateInheritedRatingCommand, "@InheritedParentalRatingValue");
            _updateInheritedRatingCommand.Parameters.Add(_updateInheritedRatingCommand, "@Guid");
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
                    _saveItemCommand.GetParameter(index++).Value = item.GetParentalRatingValue() ?? 0;
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

                    _saveItemCommand.GetParameter(index++).Value = item.DateLastSaved;
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

                    _saveItemCommand.GetParameter(index++).Value = string.Join("|", item.Tags.ToArray());
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
                    if (trailer != null)
                    {
                        _saveItemCommand.GetParameter(index++).Value = string.Join("|", trailer.TrailerTypes.Select(i => i.ToString()).ToArray());
                    }
                    else
                    {
                        _saveItemCommand.GetParameter(index++).Value = null;
                    }

                    _saveItemCommand.GetParameter(index++).Value = item.CriticRating;
                    _saveItemCommand.GetParameter(index++).Value = item.CriticRatingSummary;
                    
                    _saveItemCommand.Transaction = transaction;

                    _saveItemCommand.ExecuteNonQuery();

                    if (item.SupportsAncestors)
                    {
                        UpdateAncestors(item.Id, item.GetAncestorIds().Distinct().ToList(), transaction);
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
                Logger.Debug("Unknown type {0}", typeString);

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

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select StartPositionTicks,Name,ImagePath from " + ChaptersTableName + " where ItemId = @ItemId order by ChapterIndex asc";

                cmd.Parameters.Add(cmd, "@ItemId", DbType.Guid).Value = id;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        yield return GetChapter(reader);
                    }
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

            using (var cmd = _connection.CreateCommand())
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
        public async Task SaveChapters(Guid id, IEnumerable<ChapterInfo> chapters, CancellationToken cancellationToken)
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

        public IEnumerable<BaseItem> GetItemsOfType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            CheckDisposed();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select " + string.Join(",", _retriveItemColumns) + " from TypedBaseItems where type = @type";

                cmd.Parameters.Add(cmd, "@type", DbType.String).Value = type.FullName;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
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

        public IEnumerable<BaseItem> GetItemList(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select " + string.Join(",", _retriveItemColumns) + " from TypedBaseItems";

                var whereClauses = GetWhereClauses(query, cmd, true);

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                cmd.CommandText += whereText;

                cmd.CommandText += GetOrderByText(query);

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(CultureInfo.InvariantCulture);
                }

                //Logger.Debug(cmd.CommandText);

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
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

        public QueryResult<BaseItem> GetItems(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select " + string.Join(",", _retriveItemColumns) + " from TypedBaseItems";

                var whereClauses = GetWhereClauses(query, cmd, false);

                var whereTextWithoutPaging = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                whereClauses = GetWhereClauses(query, cmd, true);

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                cmd.CommandText += whereText;

                cmd.CommandText += GetOrderByText(query);

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(CultureInfo.InvariantCulture);
                }

                cmd.CommandText += "; select count (guid) from TypedBaseItems" + whereTextWithoutPaging;

                //Logger.Debug(cmd.CommandText);

                var list = new List<BaseItem>();
                var count = 0;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
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

                return new QueryResult<BaseItem>()
                {
                    Items = list.ToArray(),
                    TotalRecordCount = count
                };
            }
        }

        private string GetOrderByText(InternalItemsQuery query)
        {
            if (query.SortBy == null || query.SortBy.Length == 0)
            {
                return string.Empty;
            }

            var sortOrder = query.SortOrder == SortOrder.Descending ? "DESC" : "ASC";

            return " ORDER BY " + string.Join(",", query.SortBy.Select(i => MapOrderByField(i) + " " + sortOrder).ToArray());
        }

        private string MapOrderByField(string name)
        {
            if (string.Equals(name, ItemSortBy.AirTime, StringComparison.OrdinalIgnoreCase))
            {
                // TODO
                return "SortName";
            }
            if (string.Equals(name, ItemSortBy.Runtime, StringComparison.OrdinalIgnoreCase))
            {
                return "RuntimeTicks";
            }
            if (string.Equals(name, ItemSortBy.Random, StringComparison.OrdinalIgnoreCase))
            {
                return "RANDOM()";
            }

            return name;
        }

        public List<Guid> GetItemIdsList(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select guid from TypedBaseItems";

                var whereClauses = GetWhereClauses(query, cmd, true);

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                cmd.CommandText += whereText;

                cmd.CommandText += GetOrderByText(query);

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(CultureInfo.InvariantCulture);
                }

                var list = new List<Guid>();

                //Logger.Debug(cmd.CommandText);

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
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

                var whereClauses = GetWhereClauses(query, cmd, false);

                var whereTextWithoutPaging = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                whereClauses = GetWhereClauses(query, cmd, true);

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                cmd.CommandText += whereText;

                cmd.CommandText += GetOrderByText(query);

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(CultureInfo.InvariantCulture);
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

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select guid from TypedBaseItems";

                var whereClauses = GetWhereClauses(query, cmd, false);

                var whereTextWithoutPaging = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                whereClauses = GetWhereClauses(query, cmd, true);

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                cmd.CommandText += whereText;

                cmd.CommandText += GetOrderByText(query);

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(CultureInfo.InvariantCulture);
                }

                cmd.CommandText += "; select count (guid) from TypedBaseItems" + whereTextWithoutPaging;

                var list = new List<Guid>();
                var count = 0;

                //Logger.Debug(cmd.CommandText);

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
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

        private List<string> GetWhereClauses(InternalItemsQuery query, IDbCommand cmd, bool addPaging)
        {
            var whereClauses = new List<string>();

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
                whereClauses.Add("IsMovie=@IsMovie");
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
                whereClauses.Add("type=@type");
                cmd.Parameters.Add(cmd, "@type", DbType.String).Value = includeTypes[0];
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

            if (query.ParentIndexNumber.HasValue)
            {
                whereClauses.Add("ParentIndexNumber=@MinEndDate");
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

            if (query.ExcludeTrailerTypes.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var type in query.ExcludeTrailerTypes)
                {
                    clauses.Add("TrailerTypes not like @TrailerTypes" + index);
                    cmd.Parameters.Add(cmd, "@TrailerTypes" + index, DbType.String).Value = "%" + type + "%";
                    index++;
                }
                var clause = "(" + string.Join(" AND ", clauses.ToArray()) + ")";
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

            if (!string.IsNullOrWhiteSpace(query.NameContains))
            {
                whereClauses.Add("Name like @NameContains");
                cmd.Parameters.Add(cmd, "@NameContains", DbType.String).Value = "%" + query.NameContains + "%";
            }

            if (query.Genres.Length > 0)
            {
                var clauses = new List<string>();
                var index = 0;
                foreach (var item in query.Genres)
                {
                    clauses.Add("Genres like @Genres" + index);
                    cmd.Parameters.Add(cmd, "@Genres" + index, DbType.String).Value = "%" + item + "%";
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
                    clauses.Add("Tags like @Tags" + index);
                    cmd.Parameters.Add(cmd, "@Tags" + index, DbType.String).Value = "%" + item + "%";
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
                    clauses.Add("Studios like @Studios" + index);
                    cmd.Parameters.Add(cmd, "@Studios" + index, DbType.String).Value = "%" + item + "%";
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
                whereClauses.Add("LocationType=@LocationType");
                cmd.Parameters.Add(cmd, "@LocationType", DbType.String).Value = query.LocationTypes[0].ToString();
            }
            else if (query.LocationTypes.Length > 1)
            {
                var val = string.Join(",", query.LocationTypes.Select(i => "'" + i + "'").ToArray());

                whereClauses.Add("LocationType in (" + val + ")");
            }
            if (query.ExcludeLocationTypes.Length == 1)
            {
                whereClauses.Add("LocationType<>@ExcludeLocationTypes");
                cmd.Parameters.Add(cmd, "@ExcludeLocationTypes", DbType.String).Value = query.ExcludeLocationTypes[0].ToString();
            }
            else if (query.ExcludeLocationTypes.Length > 1)
            {
                var val = string.Join(",", query.ExcludeLocationTypes.Select(i => "'" + i + "'").ToArray());

                whereClauses.Add("LocationType not in (" + val + ")");
            }
            if (query.MediaTypes.Length == 1)
            {
                whereClauses.Add("MediaType=@MediaTypes");
                cmd.Parameters.Add(cmd, "@MediaTypes", DbType.String).Value = query.MediaTypes[0].ToString();
            }
            if (query.MediaTypes.Length > 1)
            {
                var val = string.Join(",", query.MediaTypes.Select(i => "'" + i + "'").ToArray());

                whereClauses.Add("MediaType in (" + val + ")");
            }

            var enableItemsByName = query.IncludeItemsByName ?? query.IncludeItemTypes.Length > 0;

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
                whereClauses.Add("Tags not like @excludeTag" + excludeTagIndex);
                cmd.Parameters.Add(cmd, "@excludeTag" + excludeTagIndex, DbType.String).Value = "%" + excludeTag + "%";
                excludeTagIndex++;
            }

            if (addPaging)
            {
                if (query.StartIndex.HasValue && query.StartIndex.Value > 0)
                {
                    var pagingWhereText = whereClauses.Count == 0 ?
                        string.Empty :
                        " where " + string.Join(" AND ", whereClauses.ToArray());

                    var orderBy = GetOrderByText(query);

                    whereClauses.Add(string.Format("guid NOT IN (SELECT guid FROM TypedBaseItems {0}" + orderBy + " LIMIT {1})",
                        pagingWhereText,
                        query.StartIndex.Value.ToString(CultureInfo.InvariantCulture)));
                }
            }

            return whereClauses;
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
            var newValues = new List<Tuple<Guid, int>>();

            using (var cmd = _connection.CreateCommand())
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
                    _updateInheritedRatingCommand.GetParameter(0).Value = item.Item1;
                    _updateInheritedRatingCommand.GetParameter(1).Value = item.Item2;

                    _updateInheritedRatingCommand.Transaction = transaction;
                    _updateInheritedRatingCommand.ExecuteNonQuery();
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
            var dict = new Dictionary<string, string[]>();

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
                        yield return GetMediaStream(reader);
                    }
                }
            }
        }

        public async Task SaveMediaStreams(Guid id, IEnumerable<MediaStream> streams, CancellationToken cancellationToken)
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
                    _saveStreamCommand.GetParameter(index++).Value = stream.IsCabac;

                    _saveStreamCommand.GetParameter(index++).Value = stream.CodecTag;
                    _saveStreamCommand.GetParameter(index++).Value = stream.Comment;

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
                item.IsCabac = reader.GetBoolean(25);
            }

            if (!reader.IsDBNull(26))
            {
                item.CodecTag = reader.GetString(26);
            }

            if (!reader.IsDBNull(27))
            {
                item.Comment = reader.GetString(27);
            }

            return item;
        }

    }
}