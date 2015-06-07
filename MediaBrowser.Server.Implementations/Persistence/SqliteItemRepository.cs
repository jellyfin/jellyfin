using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
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
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    /// <summary>
    /// Class SQLiteItemRepository
    /// </summary>
    public class SqliteItemRepository : IItemRepository
    {
        private IDbConnection _connection;

        private readonly ILogger _logger;

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

        private SqliteChapterRepository _chapterRepository;
        private SqliteMediaStreamsRepository _mediaStreamsRepository;

        private IDbCommand _deleteChildrenCommand;
        private IDbCommand _saveChildrenCommand;
        private IDbCommand _deleteItemCommand;

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

            _logger = logManager.GetLogger(GetType().Name);

            var chapterDbFile = Path.Combine(_appPaths.DataPath, "chapters.db");
            var chapterConnection = SqliteExtensions.ConnectToDb(chapterDbFile, _logger).Result;
            _chapterRepository = new SqliteChapterRepository(chapterConnection, logManager);

            var mediaStreamsDbFile = Path.Combine(_appPaths.DataPath, "mediainfo.db");
            var mediaStreamsConnection = SqliteExtensions.ConnectToDb(mediaStreamsDbFile, _logger).Result;
            _mediaStreamsRepository = new SqliteMediaStreamsRepository(mediaStreamsConnection, logManager);
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "library.db");

            _connection = await SqliteExtensions.ConnectToDb(dbFile, _logger).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists TypedBaseItems (guid GUID primary key, type TEXT, data BLOB)",
                                "create index if not exists idx_TypedBaseItems on TypedBaseItems(guid)",

                                "create table if not exists ChildrenIds (ParentId GUID, ItemId GUID, PRIMARY KEY (ParentId, ItemId))",
                                "create index if not exists idx_ChildrenIds on ChildrenIds(ParentId,ItemId)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, _logger);

			_connection.AddColumn(_logger, "TypedBaseItems", "Path", "Text");
			_connection.AddColumn(_logger, "TypedBaseItems", "StartDate", "DATETIME");
            _connection.AddColumn(_logger, "TypedBaseItems", "EndDate", "DATETIME");
            _connection.AddColumn(_logger, "TypedBaseItems", "ChannelId", "Text");
            _connection.AddColumn(_logger, "TypedBaseItems", "IsMovie", "BIT");
            _connection.AddColumn(_logger, "TypedBaseItems", "IsSports", "BIT");
            _connection.AddColumn(_logger, "TypedBaseItems", "IsKids", "BIT");
            _connection.AddColumn(_logger, "TypedBaseItems", "CommunityRating", "Float");
            _connection.AddColumn(_logger, "TypedBaseItems", "CustomRating", "Text");
            _connection.AddColumn(_logger, "TypedBaseItems", "IndexNumber", "INT");
            _connection.AddColumn(_logger, "TypedBaseItems", "IsLocked", "BIT");
            _connection.AddColumn(_logger, "TypedBaseItems", "Name", "Text");
            _connection.AddColumn(_logger, "TypedBaseItems", "OfficialRating", "Text");

            PrepareStatements();

            _mediaStreamsRepository.Initialize();
            _chapterRepository.Initialize();
        }

        /// <summary>
        /// The _write lock
        /// </summary>
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

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
                "CommunityRating",
                "CustomRating",
                "IndexNumber",
                "IsLocked",
                "Name",
                "OfficialRating"
            };
            _saveItemCommand = _connection.CreateCommand();
			_saveItemCommand.CommandText = "replace into TypedBaseItems (" + string.Join(",", saveColumns.ToArray()) + ") values (@1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13, @14, @15, @16)";
            for (var i = 1; i <= saveColumns.Count; i++)
            {
                _saveItemCommand.Parameters.Add(_saveItemCommand, "@" + i.ToString(CultureInfo.InvariantCulture));
            }

            _deleteChildrenCommand = _connection.CreateCommand();
            _deleteChildrenCommand.CommandText = "delete from ChildrenIds where ParentId=@ParentId";
            _deleteChildrenCommand.Parameters.Add(_deleteChildrenCommand, "@ParentId");

            _deleteItemCommand = _connection.CreateCommand();
            _deleteItemCommand.CommandText = "delete from TypedBaseItems where guid=@Id";
            _deleteItemCommand.Parameters.Add(_deleteItemCommand, "@Id");

            _saveChildrenCommand = _connection.CreateCommand();
            _saveChildrenCommand.CommandText = "replace into ChildrenIds (ParentId, ItemId) values (@ParentId, @ItemId)";
            _saveChildrenCommand.Parameters.Add(_saveChildrenCommand, "@ParentId");
            _saveChildrenCommand.Parameters.Add(_saveChildrenCommand, "@ItemId");
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

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

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
                    }
                    else
                    {
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
                    
                    _saveItemCommand.Transaction = transaction;

                    _saveItemCommand.ExecuteNonQuery();
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
                _logger.ErrorException("Failed to save items:", e);

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

                _writeLock.Release();
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
                cmd.CommandText = "select type,data from TypedBaseItems where guid = @guid";
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
                _logger.Debug("Unknown type {0}", typeString);

                return null;
            }

            using (var stream = reader.GetMemoryStream(1))
            {
                return _jsonSerializer.DeserializeFromStream(stream, type) as BaseItem;
            }
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
            return _chapterRepository.GetChapters(id);
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
            return _chapterRepository.GetChapter(id, index);
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
        public Task SaveChapters(Guid id, IEnumerable<ChapterInfo> chapters, CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _chapterRepository.SaveChapters(id, chapters, cancellationToken);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private readonly object _disposeLock = new object();

        private bool _disposed;
        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name + " has been disposed and cannot be accessed.");
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                _disposed = true;

                try
                {
                    lock (_disposeLock)
                    {
                        _writeLock.Wait();

                        if (_connection != null)
                        {
                            if (_connection.IsOpen())
                            {
                                _connection.Close();
                            }

                            _connection.Dispose();
                            _connection = null;
                        }

                        if (_chapterRepository != null)
                        {
                            _chapterRepository.Dispose();
                            _chapterRepository = null;
                        }

                        if (_mediaStreamsRepository != null)
                        {
                            _mediaStreamsRepository.Dispose();
                            _mediaStreamsRepository = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error disposing database", ex);
                }
            }
        }

        public IEnumerable<Guid> GetChildren(Guid parentId)
        {
            if (parentId == Guid.Empty)
            {
                throw new ArgumentNullException("parentId");
            }

            CheckDisposed();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select ItemId from ChildrenIds where ParentId = @ParentId";

                cmd.Parameters.Add(cmd, "@ParentId", DbType.Guid).Value = parentId;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        yield return reader.GetGuid(0);
                    }
                }
            }
        }

        public IEnumerable<BaseItem> GetChildrenItems(Guid parentId)
        {
            if (parentId == Guid.Empty)
            {
                throw new ArgumentNullException("parentId");
            }

            CheckDisposed();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select type,data from TypedBaseItems where guid in (select ItemId from ChildrenIds where ParentId = @ParentId)";

                cmd.Parameters.Add(cmd, "@ParentId", DbType.Guid).Value = parentId;

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

        public IEnumerable<BaseItem> GetItemsOfType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            CheckDisposed();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select type,data from TypedBaseItems where type = @type";

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

        public QueryResult<BaseItem> GetItems(InternalItemsQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select type,data from TypedBaseItems";

                var whereClauses = GetWhereClauses(query, cmd, false);

                var whereTextWithoutPaging = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                whereClauses = GetWhereClauses(query, cmd, true);

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                cmd.CommandText += whereText;

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(CultureInfo.InvariantCulture);
                }

                cmd.CommandText += "; select count (guid) from TypedBaseItems" + whereTextWithoutPaging;

                var list = new List<BaseItem>();
                var count = 0;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        list.Add(GetItem(reader));
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

                var whereClauses = GetWhereClauses(query, cmd, false);

                whereClauses = GetWhereClauses(query, cmd, true);

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                cmd.CommandText += whereText;

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(CultureInfo.InvariantCulture);
                }

                var list = new List<Guid>();

                _logger.Debug(cmd.CommandText);

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        list.Add(reader.GetGuid(0));
                    }
                }

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

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(CultureInfo.InvariantCulture);
                }

                cmd.CommandText += "; select count (guid) from TypedBaseItems" + whereTextWithoutPaging;

                var list = new List<Guid>();
                var count = 0;

                _logger.Debug(cmd.CommandText);

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

            var includeTypes = query.IncludeItemTypes.SelectMany(MapIncludeItemTypes).ToArray();

            if (includeTypes.Length == 1)
            {
                whereClauses.Add("type=@type");
                cmd.Parameters.Add(cmd, "@type", DbType.String).Value = includeTypes[0];
            }
            if (includeTypes.Length > 1)
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

            if (addPaging)
            {
                if (query.StartIndex.HasValue && query.StartIndex.Value > 0)
                {
                    var pagingWhereText = whereClauses.Count == 0 ?
                        string.Empty :
                        " where " + string.Join(" AND ", whereClauses.ToArray());

                    whereClauses.Add(string.Format("Id NOT IN (SELECT Id FROM TypedBaseItems {0} ORDER BY DateCreated DESC LIMIT {1})",
                        pagingWhereText,
                        query.StartIndex.Value.ToString(CultureInfo.InvariantCulture)));
                }
            }

            return whereClauses;
        }

        // Not crazy about having this all the way down here, but at least it's in one place
        readonly Dictionary<string, string[]> _types = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                {typeof(LiveTvProgram).Name, new []{typeof(LiveTvProgram).FullName}},
                {typeof(LiveTvChannel).Name, new []{typeof(LiveTvChannel).FullName}},
                {typeof(LiveTvVideoRecording).Name, new []{typeof(LiveTvVideoRecording).FullName}},
                {typeof(LiveTvAudioRecording).Name, new []{typeof(LiveTvAudioRecording).FullName}},
                {"Recording", new []{typeof(LiveTvAudioRecording).FullName, typeof(LiveTvVideoRecording).FullName}}
            };

        private IEnumerable<string> MapIncludeItemTypes(string value)
        {
            string[] result;
            if (_types.TryGetValue(value, out result))
            {
                return result;
            }

            return new[] { value };
        }

        public IEnumerable<Guid> GetItemIdsOfType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            CheckDisposed();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select guid from TypedBaseItems where type = @type";

                cmd.Parameters.Add(cmd, "@type", DbType.String).Value = type.FullName;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        yield return reader.GetGuid(0);
                    }
                }
            }
        }

        public async Task DeleteItem(Guid id, CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            CheckDisposed();

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                // First delete children
                _deleteChildrenCommand.GetParameter(0).Value = id;
                _deleteChildrenCommand.Transaction = transaction;
                _deleteChildrenCommand.ExecuteNonQuery();

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
                _logger.ErrorException("Failed to save children:", e);

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

                _writeLock.Release();
            }
        }

        public async Task SaveChildren(Guid parentId, IEnumerable<Guid> children, CancellationToken cancellationToken)
        {
            if (parentId == Guid.Empty)
            {
                throw new ArgumentNullException("parentId");
            }

            if (children == null)
            {
                throw new ArgumentNullException("children");
            }

            CheckDisposed();

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                // First delete 
                _deleteChildrenCommand.GetParameter(0).Value = parentId;
                _deleteChildrenCommand.Transaction = transaction;

                _deleteChildrenCommand.ExecuteNonQuery();

                foreach (var id in children)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    _saveChildrenCommand.GetParameter(0).Value = parentId;
                    _saveChildrenCommand.GetParameter(1).Value = id;

                    _saveChildrenCommand.Transaction = transaction;

                    _saveChildrenCommand.ExecuteNonQuery();
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
                _logger.ErrorException("Failed to save children:", e);

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

                _writeLock.Release();
            }
        }

        public IEnumerable<MediaStream> GetMediaStreams(MediaStreamQuery query)
        {
            CheckDisposed();
            return _mediaStreamsRepository.GetMediaStreams(query);
        }

        public Task SaveMediaStreams(Guid id, IEnumerable<MediaStream> streams, CancellationToken cancellationToken)
        {
            CheckDisposed();
            return _mediaStreamsRepository.SaveMediaStreams(id, streams, cancellationToken);
        }
    }
}