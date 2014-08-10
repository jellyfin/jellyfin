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

        private IDbCommand _saveJobCommand;
        private IDbCommand _saveJobItemCommand;

        public SyncRepository(ILogger logger, IServerApplicationPaths appPaths)
        {
            _logger = logger;
            _appPaths = appPaths;
        }

        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "sync.db");

            _connection = await SqliteExtensions.ConnectToDb(dbFile, _logger).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists SyncJobs (Id GUID PRIMARY KEY, TargetId TEXT NOT NULL, Name TEXT NOT NULL, Quality TEXT NOT NULL, Status TEXT NOT NULL, Progress FLOAT, UserId TEXT NOT NULL, ItemIds TEXT NOT NULL, UnwatchedOnly BIT, SyncLimit BigInt, LimitType TEXT, IsDynamic BIT, DateCreated DateTime, DateLastModified DateTime, ItemCount int)",
                                "create index if not exists idx_SyncJobs on SyncJobs(Id)",

                                "create table if not exists SyncJobItems (Id GUID PRIMARY KEY, ItemId TEXT, JobId TEXT, OutputPath TEXT, Status TEXT, TargetId TEXT)",
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
            _saveJobCommand = _connection.CreateCommand();
            _saveJobCommand.CommandText = "replace into SyncJobs (Id, TargetId, Name, Quality, Status, Progress, UserId, ItemIds, UnwatchedOnly, SyncLimit, LimitType, IsDynamic, DateCreated, DateLastModified, ItemCount) values (@Id, @TargetId, @Name, @Quality, @Status, @Progress, @UserId, @ItemIds, @UnwatchedOnly, @SyncLimit, @LimitType, @IsDynamic, @DateCreated, @DateLastModified, @ItemCount)";

            _saveJobCommand.Parameters.Add(_saveJobCommand, "@Id");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@TargetId");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@Name");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@Quality");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@Status");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@Progress");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@UserId");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@ItemIds");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@UnwatchedOnly");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@SyncLimit");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@LimitType");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@IsDynamic");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@DateCreated");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@DateLastModified");
            _saveJobCommand.Parameters.Add(_saveJobCommand, "@ItemCount");

            _saveJobItemCommand = _connection.CreateCommand();
            _saveJobItemCommand.CommandText = "replace into SyncJobItems (Id, ItemId, JobId, OutputPath, Status, TargetId) values (@Id, @ItemId, @JobId, @OutputPath, @Status, @TargetId)";

            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@Id");
            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@ItemId");
            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@JobId");
            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@OutputPath");
            _saveJobItemCommand.Parameters.Add(_saveJobCommand, "@Status");
        }

        private const string BaseJobSelectText = "select Id, TargetId, Name, Quality, Status, Progress, UserId, ItemIds, UnwatchedOnly, SyncLimit, LimitType, IsDynamic, DateCreated, DateLastModified, ItemCount from SyncJobs";
        private const string BaseJobItemSelectText = "select Id, ItemId, JobId, OutputPath, Status, TargetId from SyncJobItems";

        public SyncJob GetJob(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var guid = new Guid(id);

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
                info.UnwatchedOnly = reader.GetBoolean(8);
            }

            if (!reader.IsDBNull(9))
            {
                info.Limit = reader.GetInt64(9);
            }

            if (!reader.IsDBNull(10))
            {
                info.LimitType = (SyncLimitType)Enum.Parse(typeof(SyncLimitType), reader.GetString(10), true);
            }

            info.IsDynamic = reader.GetBoolean(11);
            info.DateCreated = reader.GetDateTime(12).ToUniversalTime();
            info.DateLastModified = reader.GetDateTime(13).ToUniversalTime();
            info.ItemCount = reader.GetInt32(14);

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
                _saveJobCommand.GetParameter(index++).Value = job.UnwatchedOnly;
                _saveJobCommand.GetParameter(index++).Value = job.Limit;
                _saveJobCommand.GetParameter(index++).Value = job.LimitType;
                _saveJobCommand.GetParameter(index++).Value = job.IsDynamic;
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

                cmd.CommandText += "; select count (Id) from SyncJobs";

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
                        return GetSyncJobItem(reader);
                    }
                }
            }

            return null;
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
                _saveJobItemCommand.GetParameter(index++).Value = jobItem.JobId;
                _saveJobItemCommand.GetParameter(index++).Value = jobItem.OutputPath;
                _saveJobItemCommand.GetParameter(index++).Value = jobItem.Status;
                _saveJobItemCommand.GetParameter(index++).Value = jobItem.TargetId;

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

        private SyncJobItem GetSyncJobItem(IDataReader reader)
        {
            var info = new SyncJobItem
            {
                Id = reader.GetGuid(0).ToString("N"),
                ItemId = reader.GetString(1),
                JobId = reader.GetString(2)
            };

            if (!reader.IsDBNull(3))
            {
                info.OutputPath = reader.GetString(3);
            }

            if (!reader.IsDBNull(4))
            {
                info.Status = (SyncJobStatus)Enum.Parse(typeof(SyncJobStatus), reader.GetString(4), true);
            }

            info.TargetId = reader.GetString(5);

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
