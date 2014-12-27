using MediaBrowser.Controller;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using MediaBrowser.Server.Implementations.Persistence;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class SyncRepository : ISyncRepository, IDisposable
    {
        private IDbConnection _connection;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly IServerApplicationPaths _appPaths;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private IDbCommand _deleteJobCommand;
        private IDbCommand _deleteJobItemsCommand;
        private IDbCommand _saveJobCommand;
        private IDbCommand _saveJobItemCommand;

        public SyncRepository(ILogger logger, IServerApplicationPaths appPaths)
        {
            _logger = logger;
            _appPaths = appPaths;
        }

        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "sync8.db");

            _connection = await SqliteExtensions.ConnectToDb(dbFile, _logger).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists SyncJobs (Id GUID PRIMARY KEY, TargetId TEXT NOT NULL, Name TEXT NOT NULL, Quality TEXT NOT NULL, Status TEXT NOT NULL, Progress FLOAT, UserId TEXT NOT NULL, ItemIds TEXT NOT NULL, Category TEXT, ParentId TEXT, UnwatchedOnly BIT, ItemLimit INT, SyncNewContent BIT, DateCreated DateTime, DateLastModified DateTime, ItemCount int)",
                                "create index if not exists idx_SyncJobs on SyncJobs(Id)",

                                "create table if not exists SyncJobItems (Id GUID PRIMARY KEY, ItemId TEXT, MediaSourceId TEXT, JobId TEXT, OutputPath TEXT, Status TEXT, TargetId TEXT, DateCreated DateTime, Progress FLOAT)",
                                "create index if not exists idx_SyncJobItems on SyncJobs(Id)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, _logger);

            PrepareStatements();
        }

        private void PrepareStatements()
        {
            _deleteJobCommand = _connection.CreateCommand();
            _deleteJobCommand.CommandText = "delete from SyncJobs where Id=@Id";
            _deleteJobCommand.Parameters.Add(_deleteJobCommand, "@Id");

            _deleteJobItemsCommand = _connection.CreateCommand();
            _deleteJobItemsCommand.CommandText = "delete from SyncJobItems where JobId=@JobId";
            _deleteJobItemsCommand.Parameters.Add(_deleteJobItemsCommand, "@JobId");
            
            _saveJobCommand = _connection.CreateCommand();
            _saveJobCommand.CommandText = "replace into SyncJobs (Id, TargetId, Name, Quality, Status, Progress, UserId, ItemIds, Category, ParentId, UnwatchedOnly, ItemLimit, SyncNewContent, DateCreated, DateLastModified, ItemCount) values (@Id, @TargetId, @Name, @Quality, @Status, @Progress, @UserId, @ItemIds, @Category, @ParentId, @UnwatchedOnly, @ItemLimit, @SyncNewContent, @DateCreated, @DateLastModified, @ItemCount)";

            _saveJobCommand.Parameters.Add(_saveJobCommand, "@Id");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@TargetId");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@Name");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@Quality");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@Status");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@Progress");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@UserId");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@ItemIds");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@Category");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@ParentId");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@UnwatchedOnly");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@ItemLimit");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@SyncNewContent");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@DateCreated");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@DateLastModified");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@ItemCount");

            _saveJobItemCommand = _connection.CreateCommand();
            _saveJobItemCommand.CommandText = "replace into SyncJobItems (Id, ItemId, MediaSourceId, JobId, OutputPath, Status, TargetId, DateCreated, Progress) values (@Id, @ItemId, @MediaSourceId, @JobId, @OutputPath, @Status, @TargetId, @DateCreated, @Progress)";

            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@Id");
            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@ItemId");
            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@MediaSourceId");
            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@JobId");
            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@OutputPath");
            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@Status");
            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@TargetId");
            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@DateCreated");
            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@Progress");
        }

        private const string BaseJobSelectText = "select Id, TargetId, Name, Quality, Status, Progress, UserId, ItemIds, Category, ParentId, UnwatchedOnly, ItemLimit, SyncNewContent, DateCreated, DateLastModified, ItemCount from SyncJobs";
        private const string BaseJobItemSelectText = "select Id, ItemId, MediaSourceId, JobId, OutputPath, Status, TargetId, DateCreated, Progress from SyncJobItems";

        public SyncJob GetJob(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var guid = new Guid(id);

            if (guid == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }
            
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = BaseJobSelectText + " where Id=@Id";

                cmd.Parameters.Add(cmd, "@Id", DbType.Guid).Value = guid;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        return GetJob(reader);
                    }
                }
            }

            return null;
        }

        private SyncJob GetJob(IDataReader reader)
        {
            var info = new SyncJob
            {
                Id = reader.GetGuid(0).ToString("N"),
                TargetId = reader.GetString(1),
                Name = reader.GetString(2)
            };

            if (!reader.IsDBNull(3))
            {
                info.Quality = (SyncQuality)Enum.Parse(typeof(SyncQuality), reader.GetString(3), true);
            }

            if (!reader.IsDBNull(4))
            {
                info.Status = (SyncJobStatus)Enum.Parse(typeof(SyncJobStatus), reader.GetString(4), true);
            }

            if (!reader.IsDBNull(5))
            {
                info.Progress = reader.GetDouble(5);
            }

            if (!reader.IsDBNull(6))
            {
                info.UserId = reader.GetString(6);
            }

            if (!reader.IsDBNull(7))
            {
                info.RequestedItemIds = reader.GetString(7).Split(',').ToList();
            }

            if (!reader.IsDBNull(8))
            {
                info.Category = (SyncCategory)Enum.Parse(typeof(SyncCategory), reader.GetString(8), true);
            }

            if (!reader.IsDBNull(9))
            {
                info.ParentId = reader.GetString(9);
            }

            if (!reader.IsDBNull(10))
            {
                info.UnwatchedOnly = reader.GetBoolean(10);
            }

            if (!reader.IsDBNull(11))
            {
                info.ItemLimit = reader.GetInt32(11);
            }

            info.SyncNewContent = reader.GetBoolean(12);

            info.DateCreated = reader.GetDateTime(13).ToUniversalTime();
            info.DateLastModified = reader.GetDateTime(14).ToUniversalTime();
            info.ItemCount = reader.GetInt32(15);

            return info;
        }

        public Task Create(SyncJob job)
        {
            return Update(job);
        }

        public async Task Update(SyncJob job)
        {
            if (job == null)
            {
                throw new ArgumentNullException("job");
            }

            await _writeLock.WaitAsync().ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                var index = 0;

                _saveJobCommand.GetParameter(index++).Value = new Guid(job.Id);
                _saveJobCommand.GetParameter(index++).Value = job.TargetId;
                _saveJobCommand.GetParameter(index++).Value = job.Name;
                _saveJobCommand.GetParameter(index++).Value = job.Quality;
                _saveJobCommand.GetParameter(index++).Value = job.Status;
                _saveJobCommand.GetParameter(index++).Value = job.Progress;
                _saveJobCommand.GetParameter(index++).Value = job.UserId;
                _saveJobCommand.GetParameter(index++).Value = string.Join(",", job.RequestedItemIds.ToArray());
                _saveJobCommand.GetParameter(index++).Value = job.Category;
                _saveJobCommand.GetParameter(index++).Value = job.ParentId;
                _saveJobCommand.GetParameter(index++).Value = job.UnwatchedOnly;
                _saveJobCommand.GetParameter(index++).Value = job.ItemLimit;
                _saveJobCommand.GetParameter(index++).Value = job.SyncNewContent;
                _saveJobCommand.GetParameter(index++).Value = job.DateCreated;
                _saveJobCommand.GetParameter(index++).Value = job.DateLastModified;
                _saveJobCommand.GetParameter(index++).Value = job.ItemCount;

                _saveJobCommand.Transaction = transaction;

                _saveJobCommand.ExecuteNonQuery();

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
                _logger.ErrorException("Failed to save record:", e);

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

        public async Task DeleteJob(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            await _writeLock.WaitAsync().ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                var index = 0;

                _deleteJobCommand.GetParameter(index++).Value = new Guid(id);
                _deleteJobCommand.Transaction = transaction;
                _deleteJobCommand.ExecuteNonQuery();

                index = 0;
                _deleteJobItemsCommand.GetParameter(index++).Value = id;
                _deleteJobItemsCommand.Transaction = transaction;
                _deleteJobItemsCommand.ExecuteNonQuery();
                
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
                _logger.ErrorException("Failed to save record:", e);

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

        public QueryResult<SyncJob> GetJobs(SyncJobQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = BaseJobSelectText;

                var whereClauses = new List<string>();

                if (query.IsCompleted.HasValue)
                {
                    if (query.IsCompleted.Value)
                    {
                        whereClauses.Add("Status=@Status");
                    }
                    else
                    {
                        whereClauses.Add("Status<>@Status");
                    }
                    cmd.Parameters.Add(cmd, "@Status", DbType.String).Value = SyncJobStatus.Completed.ToString();
                }
                if (!string.IsNullOrWhiteSpace(query.TargetId))
                {
                    whereClauses.Add("TargetId=@TargetId");
                    cmd.Parameters.Add(cmd, "@TargetId", DbType.String).Value = query.TargetId;
                }

                var whereTextWithoutPaging = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                var startIndex = query.StartIndex ?? 0;
                if (startIndex > 0)
                {
                    whereClauses.Add(string.Format("Id NOT IN (SELECT Id FROM SyncJobs ORDER BY DateLastModified DESC LIMIT {0})",
                        startIndex.ToString(_usCulture)));
                }

                if (whereClauses.Count > 0)
                {
                    cmd.CommandText += " where " + string.Join(" AND ", whereClauses.ToArray());
                }

                cmd.CommandText += " ORDER BY DateLastModified DESC";

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(_usCulture);
                }

                cmd.CommandText += "; select count (Id) from SyncJobs" + whereTextWithoutPaging;

                var list = new List<SyncJob>();
                var count = 0;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        list.Add(GetJob(reader));
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        count = reader.GetInt32(0);
                    }
                }

                return new QueryResult<SyncJob>()
                {
                    Items = list.ToArray(),
                    TotalRecordCount = count
                };
            }
        }

        public SyncJobItem GetJobItem(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var guid = new Guid(id);

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = BaseJobItemSelectText + " where Id=@Id";

                cmd.Parameters.Add(cmd, "@Id", DbType.Guid).Value = guid;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        return GetJobItem(reader);
                    }
                }
            }

            return null;
        }

        public QueryResult<SyncJobItem> GetJobItems(SyncJobItemQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = BaseJobItemSelectText;

                var whereClauses = new List<string>();

                if (!string.IsNullOrWhiteSpace(query.JobId))
                {
                    whereClauses.Add("JobId=@JobId");
                    cmd.Parameters.Add(cmd, "@JobId", DbType.String).Value = query.JobId;
                }
                if (!string.IsNullOrWhiteSpace(query.TargetId))
                {
                    whereClauses.Add("TargetId=@TargetId");
                    cmd.Parameters.Add(cmd, "@TargetId", DbType.String).Value = query.TargetId;
                }
                if (query.Status.HasValue)
                {
                    whereClauses.Add("Status=@Status");
                    cmd.Parameters.Add(cmd, "@Status", DbType.String).Value = query.Status.Value.ToString();
                }

                if (query.IsCompleted.HasValue)
                {
                    if (query.IsCompleted.Value)
                    {
                        whereClauses.Add("Status=@Status");
                    }
                    else
                    {
                        whereClauses.Add("Status<>@Status");
                    }
                    cmd.Parameters.Add(cmd, "@Status", DbType.String).Value = SyncJobStatus.Completed.ToString();
                }

                var whereTextWithoutPaging = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                var startIndex = query.StartIndex ?? 0;
                if (startIndex > 0)
                {
                    whereClauses.Add(string.Format("Id NOT IN (SELECT Id FROM SyncJobItems ORDER BY DateCreated LIMIT {0})",
                        startIndex.ToString(_usCulture)));
                }

                if (whereClauses.Count > 0)
                {
                    cmd.CommandText += " where " + string.Join(" AND ", whereClauses.ToArray());
                }

                cmd.CommandText += " ORDER BY DateCreated";

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(_usCulture);
                }

                cmd.CommandText += "; select count (Id) from SyncJobItems" + whereTextWithoutPaging;

                var list = new List<SyncJobItem>();
                var count = 0;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        list.Add(GetJobItem(reader));
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        count = reader.GetInt32(0);
                    }
                }

                return new QueryResult<SyncJobItem>()
                {
                    Items = list.ToArray(),
                    TotalRecordCount = count
                };
            }
        }

        public Task Create(SyncJobItem jobItem)
        {
            return Update(jobItem);
        }

        public async Task Update(SyncJobItem jobItem)
        {
            if (jobItem == null)
            {
                throw new ArgumentNullException("jobItem");
            }

            await _writeLock.WaitAsync().ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                var index = 0;

                _saveJobItemCommand.GetParameter(index++).Value = new Guid(jobItem.Id);
                _saveJobItemCommand.GetParameter(index++).Value = jobItem.ItemId;
                _saveJobItemCommand.GetParameter(index++).Value = jobItem.MediaSourceId;
                _saveJobItemCommand.GetParameter(index++).Value = jobItem.JobId;
                _saveJobItemCommand.GetParameter(index++).Value = jobItem.OutputPath;
                _saveJobItemCommand.GetParameter(index++).Value = jobItem.Status;
                _saveJobItemCommand.GetParameter(index++).Value = jobItem.TargetId;
                _saveJobItemCommand.GetParameter(index++).Value = jobItem.DateCreated;
                _saveJobItemCommand.GetParameter(index++).Value = jobItem.Progress;

                _saveJobItemCommand.Transaction = transaction;

                _saveJobItemCommand.ExecuteNonQuery();

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
                _logger.ErrorException("Failed to save record:", e);

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

        private SyncJobItem GetJobItem(IDataReader reader)
        {
            var info = new SyncJobItem
            {
                Id = reader.GetGuid(0).ToString("N"),
                ItemId = reader.GetString(1)
            };

            if (!reader.IsDBNull(2))
            {
                info.MediaSourceId = reader.GetString(2);
            }
            
            info.JobId = reader.GetString(3);
            
            if (!reader.IsDBNull(4))
            {
                info.OutputPath = reader.GetString(4);
            }

            if (!reader.IsDBNull(5))
            {
                info.Status = (SyncJobItemStatus)Enum.Parse(typeof(SyncJobItemStatus), reader.GetString(5), true);
            }

            info.TargetId = reader.GetString(6);

            info.DateCreated = reader.GetDateTime(7);

            if (!reader.IsDBNull(8))
            {
                info.Progress = reader.GetDouble(8);
            }
            
            return info;
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

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                try
                {
                    lock (_disposeLock)
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
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error disposing database", ex);
                }
            }
        }
    }
}
