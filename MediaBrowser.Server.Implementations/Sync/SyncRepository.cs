using MediaBrowser.Controller;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Sync;
using MediaBrowser.Server.Implementations.Persistence;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class SyncRepository : BaseSqliteRepository, ISyncRepository
    {
        private IDbConnection _connection;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private IDbCommand _insertJobCommand;
        private IDbCommand _updateJobCommand;
        private IDbCommand _deleteJobCommand;

        private IDbCommand _deleteJobItemsCommand;
        private IDbCommand _insertJobItemCommand;
        private IDbCommand _updateJobItemCommand;

        private readonly IJsonSerializer _json;
        private readonly IServerApplicationPaths _appPaths;

        public SyncRepository(ILogManager logManager, IJsonSerializer json, IServerApplicationPaths appPaths)
            : base(logManager)
        {
            _json = json;
            _appPaths = appPaths;
        }

        public async Task Initialize(IDbConnector dbConnector)
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "sync14.db");

            _connection = await dbConnector.Connect(dbFile).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists SyncJobs (Id GUID PRIMARY KEY, TargetId TEXT NOT NULL, Name TEXT NOT NULL, Profile TEXT, Quality TEXT, Bitrate INT, Status TEXT NOT NULL, Progress FLOAT, UserId TEXT NOT NULL, ItemIds TEXT NOT NULL, Category TEXT, ParentId TEXT, UnwatchedOnly BIT, ItemLimit INT, SyncNewContent BIT, DateCreated DateTime, DateLastModified DateTime, ItemCount int)",
                                "create index if not exists idx_SyncJobs on SyncJobs(Id)",
                                "create index if not exists idx_SyncJobs1 on SyncJobs(TargetId)",

                                "create table if not exists SyncJobItems (Id GUID PRIMARY KEY, ItemId TEXT, ItemName TEXT, MediaSourceId TEXT, JobId TEXT, TemporaryPath TEXT, OutputPath TEXT, Status TEXT, TargetId TEXT, DateCreated DateTime, Progress FLOAT, AdditionalFiles TEXT, MediaSource TEXT, IsMarkedForRemoval BIT, JobItemIndex INT, ItemDateModifiedTicks BIGINT)",
                                "create index if not exists idx_SyncJobItems1 on SyncJobItems(Id)",
                                "create index if not exists idx_SyncJobItems2 on SyncJobItems(TargetId)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, Logger);

            _connection.AddColumn(Logger, "SyncJobs", "Profile", "TEXT");
            _connection.AddColumn(Logger, "SyncJobs", "Bitrate", "INT");
            _connection.AddColumn(Logger, "SyncJobItems", "ItemDateModifiedTicks", "BIGINT");

            PrepareStatements();
        }

        private void PrepareStatements()
        {
            // _deleteJobCommand
            _deleteJobCommand = _connection.CreateCommand();
            _deleteJobCommand.CommandText = "delete from SyncJobs where Id=@Id";
            _deleteJobCommand.Parameters.Add(_deleteJobCommand, "@Id");

            // _deleteJobItemsCommand
            _deleteJobItemsCommand = _connection.CreateCommand();
            _deleteJobItemsCommand.CommandText = "delete from SyncJobItems where JobId=@JobId";
            _deleteJobItemsCommand.Parameters.Add(_deleteJobItemsCommand, "@JobId");

            // _insertJobCommand
            _insertJobCommand = _connection.CreateCommand();
            _insertJobCommand.CommandText = "insert into SyncJobs (Id, TargetId, Name, Profile, Quality, Bitrate, Status, Progress, UserId, ItemIds, Category, ParentId, UnwatchedOnly, ItemLimit, SyncNewContent, DateCreated, DateLastModified, ItemCount) values (@Id, @TargetId, @Name, @Profile, @Quality, @Bitrate, @Status, @Progress, @UserId, @ItemIds, @Category, @ParentId, @UnwatchedOnly, @ItemLimit, @SyncNewContent, @DateCreated, @DateLastModified, @ItemCount)";

            _insertJobCommand.Parameters.Add(_insertJobCommand, "@Id");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@TargetId");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@Name");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@Profile");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@Quality");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@Bitrate");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@Status");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@Progress");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@UserId");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@ItemIds");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@Category");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@ParentId");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@UnwatchedOnly");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@ItemLimit");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@SyncNewContent");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@DateCreated");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@DateLastModified");
            _insertJobCommand.Parameters.Add(_insertJobCommand, "@ItemCount");

            // _updateJobCommand
            _updateJobCommand = _connection.CreateCommand();
            _updateJobCommand.CommandText = "update SyncJobs set TargetId=@TargetId,Name=@Name,Profile=@Profile,Quality=@Quality,Bitrate=@Bitrate,Status=@Status,Progress=@Progress,UserId=@UserId,ItemIds=@ItemIds,Category=@Category,ParentId=@ParentId,UnwatchedOnly=@UnwatchedOnly,ItemLimit=@ItemLimit,SyncNewContent=@SyncNewContent,DateCreated=@DateCreated,DateLastModified=@DateLastModified,ItemCount=@ItemCount where Id=@Id";

            _updateJobCommand.Parameters.Add(_updateJobCommand, "@Id");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@TargetId");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@Name");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@Profile");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@Quality");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@Bitrate");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@Status");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@Progress");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@UserId");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@ItemIds");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@Category");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@ParentId");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@UnwatchedOnly");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@ItemLimit");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@SyncNewContent");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@DateCreated");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@DateLastModified");
            _updateJobCommand.Parameters.Add(_updateJobCommand, "@ItemCount");

            // _insertJobItemCommand
            _insertJobItemCommand = _connection.CreateCommand();
            _insertJobItemCommand.CommandText = "insert into SyncJobItems (Id, ItemId, ItemName, MediaSourceId, JobId, TemporaryPath, OutputPath, Status, TargetId, DateCreated, Progress, AdditionalFiles, MediaSource, IsMarkedForRemoval, JobItemIndex, ItemDateModifiedTicks) values (@Id, @ItemId, @ItemName, @MediaSourceId, @JobId, @TemporaryPath, @OutputPath, @Status, @TargetId, @DateCreated, @Progress, @AdditionalFiles, @MediaSource, @IsMarkedForRemoval, @JobItemIndex, @ItemDateModifiedTicks)";

            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@Id");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@ItemId");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@ItemName");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@MediaSourceId");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@JobId");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@TemporaryPath");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@OutputPath");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@Status");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@TargetId");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@DateCreated");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@Progress");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@AdditionalFiles");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@MediaSource");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@IsMarkedForRemoval");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@JobItemIndex");
            _insertJobItemCommand.Parameters.Add(_insertJobItemCommand, "@ItemDateModifiedTicks");

            // _updateJobItemCommand
            _updateJobItemCommand = _connection.CreateCommand();
            _updateJobItemCommand.CommandText = "update SyncJobItems set ItemId=@ItemId,ItemName=@ItemName,MediaSourceId=@MediaSourceId,JobId=@JobId,TemporaryPath=@TemporaryPath,OutputPath=@OutputPath,Status=@Status,TargetId=@TargetId,DateCreated=@DateCreated,Progress=@Progress,AdditionalFiles=@AdditionalFiles,MediaSource=@MediaSource,IsMarkedForRemoval=@IsMarkedForRemoval,JobItemIndex=@JobItemIndex,ItemDateModifiedTicks=@ItemDateModifiedTicks where Id=@Id";

            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@Id");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@ItemId");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@ItemName");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@MediaSourceId");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@JobId");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@TemporaryPath");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@OutputPath");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@Status");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@TargetId");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@DateCreated");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@Progress");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@AdditionalFiles");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@MediaSource");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@IsMarkedForRemoval");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@JobItemIndex");
            _updateJobItemCommand.Parameters.Add(_updateJobItemCommand, "@ItemDateModifiedTicks");
        }

        private const string BaseJobSelectText = "select Id, TargetId, Name, Profile, Quality, Bitrate, Status, Progress, UserId, ItemIds, Category, ParentId, UnwatchedOnly, ItemLimit, SyncNewContent, DateCreated, DateLastModified, ItemCount from SyncJobs";
        private const string BaseJobItemSelectText = "select Id, ItemId, ItemName, MediaSourceId, JobId, TemporaryPath, OutputPath, Status, TargetId, DateCreated, Progress, AdditionalFiles, MediaSource, IsMarkedForRemoval, JobItemIndex, ItemDateModifiedTicks from SyncJobItems";

        public SyncJob GetJob(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            CheckDisposed();
            
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
                info.Profile = reader.GetString(3);
            }

            if (!reader.IsDBNull(4))
            {
                info.Quality = reader.GetString(4);
            }

            if (!reader.IsDBNull(5))
            {
                info.Bitrate = reader.GetInt32(5);
            }

            if (!reader.IsDBNull(6))
            {
                info.Status = (SyncJobStatus)Enum.Parse(typeof(SyncJobStatus), reader.GetString(6), true);
            }

            if (!reader.IsDBNull(7))
            {
                info.Progress = reader.GetDouble(7);
            }

            if (!reader.IsDBNull(8))
            {
                info.UserId = reader.GetString(8);
            }

            if (!reader.IsDBNull(9))
            {
                info.RequestedItemIds = reader.GetString(9).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (!reader.IsDBNull(10))
            {
                info.Category = (SyncCategory)Enum.Parse(typeof(SyncCategory), reader.GetString(10), true);
            }

            if (!reader.IsDBNull(11))
            {
                info.ParentId = reader.GetString(11);
            }

            if (!reader.IsDBNull(12))
            {
                info.UnwatchedOnly = reader.GetBoolean(12);
            }

            if (!reader.IsDBNull(13))
            {
                info.ItemLimit = reader.GetInt32(13);
            }

            info.SyncNewContent = reader.GetBoolean(14);

            info.DateCreated = reader.GetDateTime(15).ToUniversalTime();
            info.DateLastModified = reader.GetDateTime(16).ToUniversalTime();
            info.ItemCount = reader.GetInt32(17);

            return info;
        }

        public Task Create(SyncJob job)
        {
            return InsertOrUpdate(job, _insertJobCommand);
        }

        public Task Update(SyncJob job)
        {
            return InsertOrUpdate(job, _updateJobCommand);
        }

        private async Task InsertOrUpdate(SyncJob job, IDbCommand cmd)
        {
            if (job == null)
            {
                throw new ArgumentNullException("job");
            }

            CheckDisposed();
            
            await WriteLock.WaitAsync().ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                var index = 0;

                cmd.GetParameter(index++).Value = new Guid(job.Id);
                cmd.GetParameter(index++).Value = job.TargetId;
                cmd.GetParameter(index++).Value = job.Name;
                cmd.GetParameter(index++).Value = job.Profile;
                cmd.GetParameter(index++).Value = job.Quality;
                cmd.GetParameter(index++).Value = job.Bitrate;
                cmd.GetParameter(index++).Value = job.Status.ToString();
                cmd.GetParameter(index++).Value = job.Progress;
                cmd.GetParameter(index++).Value = job.UserId;
                cmd.GetParameter(index++).Value = string.Join(",", job.RequestedItemIds.ToArray());
                cmd.GetParameter(index++).Value = job.Category;
                cmd.GetParameter(index++).Value = job.ParentId;
                cmd.GetParameter(index++).Value = job.UnwatchedOnly;
                cmd.GetParameter(index++).Value = job.ItemLimit;
                cmd.GetParameter(index++).Value = job.SyncNewContent;
                cmd.GetParameter(index++).Value = job.DateCreated;
                cmd.GetParameter(index++).Value = job.DateLastModified;
                cmd.GetParameter(index++).Value = job.ItemCount;

                cmd.Transaction = transaction;

                cmd.ExecuteNonQuery();

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
                Logger.ErrorException("Failed to save record:", e);

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

        public async Task DeleteJob(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            CheckDisposed();
            
            await WriteLock.WaitAsync().ConfigureAwait(false);

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
                Logger.ErrorException("Failed to save record:", e);

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

        public QueryResult<SyncJob> GetJobs(SyncJobQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            CheckDisposed();
            
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = BaseJobSelectText;

                var whereClauses = new List<string>();

                if (query.Statuses.Length > 0)
                {
                    var statuses = string.Join(",", query.Statuses.Select(i => "'" + i.ToString() + "'").ToArray());

                    whereClauses.Add(string.Format("Status in ({0})", statuses));
                }
                if (!string.IsNullOrWhiteSpace(query.TargetId))
                {
                    whereClauses.Add("TargetId=@TargetId");
                    cmd.Parameters.Add(cmd, "@TargetId", DbType.String).Value = query.TargetId;
                }
                if (!string.IsNullOrWhiteSpace(query.UserId))
                {
                    whereClauses.Add("UserId=@UserId");
                    cmd.Parameters.Add(cmd, "@UserId", DbType.String).Value = query.UserId;
                }
                if (query.SyncNewContent.HasValue)
                {
                    whereClauses.Add("SyncNewContent=@SyncNewContent");
                    cmd.Parameters.Add(cmd, "@SyncNewContent", DbType.Boolean).Value = query.SyncNewContent.Value;
                }

                cmd.CommandText += " mainTable";

                var whereTextWithoutPaging = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                var startIndex = query.StartIndex ?? 0;
                if (startIndex > 0)
                {
                    whereClauses.Add(string.Format("Id NOT IN (SELECT Id FROM SyncJobs ORDER BY (Select Max(DateLastModified) from SyncJobs where TargetId=mainTable.TargetId) DESC, DateLastModified DESC LIMIT {0})",
                        startIndex.ToString(_usCulture)));
                }

                if (whereClauses.Count > 0)
                {
                    cmd.CommandText += " where " + string.Join(" AND ", whereClauses.ToArray());
                }

                cmd.CommandText += " ORDER BY (Select Max(DateLastModified) from SyncJobs where TargetId=mainTable.TargetId) DESC, DateLastModified DESC";

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

            CheckDisposed();
            
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

        private QueryResult<T> GetJobItemReader<T>(SyncJobItemQuery query, string baseSelectText, Func<IDataReader, T> itemFactory)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = baseSelectText;

                var whereClauses = new List<string>();

                if (!string.IsNullOrWhiteSpace(query.JobId))
                {
                    whereClauses.Add("JobId=@JobId");
                    cmd.Parameters.Add(cmd, "@JobId", DbType.String).Value = query.JobId;
                }
                if (!string.IsNullOrWhiteSpace(query.ItemId))
                {
                    whereClauses.Add("ItemId=@ItemId");
                    cmd.Parameters.Add(cmd, "@ItemId", DbType.String).Value = query.ItemId;
                }
                if (!string.IsNullOrWhiteSpace(query.TargetId))
                {
                    whereClauses.Add("TargetId=@TargetId");
                    cmd.Parameters.Add(cmd, "@TargetId", DbType.String).Value = query.TargetId;
                }

                if (query.Statuses.Length > 0)
                {
                    var statuses = string.Join(",", query.Statuses.Select(i => "'" + i.ToString() + "'").ToArray());

                    whereClauses.Add(string.Format("Status in ({0})", statuses));
                }

                var whereTextWithoutPaging = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                var startIndex = query.StartIndex ?? 0;
                if (startIndex > 0)
                {
                    whereClauses.Add(string.Format("Id NOT IN (SELECT Id FROM SyncJobItems ORDER BY JobItemIndex, DateCreated LIMIT {0})",
                        startIndex.ToString(_usCulture)));
                }

                if (whereClauses.Count > 0)
                {
                    cmd.CommandText += " where " + string.Join(" AND ", whereClauses.ToArray());
                }

                cmd.CommandText += " ORDER BY JobItemIndex, DateCreated";

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(_usCulture);
                }

                cmd.CommandText += "; select count (Id) from SyncJobItems" + whereTextWithoutPaging;

                var list = new List<T>();
                var count = 0;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        list.Add(itemFactory(reader));
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        count = reader.GetInt32(0);
                    }
                }

                return new QueryResult<T>()
                {
                    Items = list.ToArray(),
                    TotalRecordCount = count
                };
            }
        }

        public QueryResult<SyncedItemProgress> GetSyncedItemProgresses(SyncJobItemQuery query)
        {
            return GetJobItemReader(query, "select ItemId,Status from SyncJobItems", GetSyncedItemProgress);
        }

        public QueryResult<SyncJobItem> GetJobItems(SyncJobItemQuery query)
        {
            return GetJobItemReader(query, BaseJobItemSelectText, GetJobItem);
        }

        public Task Create(SyncJobItem jobItem)
        {
            return InsertOrUpdate(jobItem, _insertJobItemCommand);
        }

        public Task Update(SyncJobItem jobItem)
        {
            return InsertOrUpdate(jobItem, _updateJobItemCommand);
        }

        private async Task InsertOrUpdate(SyncJobItem jobItem, IDbCommand cmd)
        {
            if (jobItem == null)
            {
                throw new ArgumentNullException("jobItem");
            }

            CheckDisposed();
            
            await WriteLock.WaitAsync().ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                var index = 0;

                cmd.GetParameter(index++).Value = new Guid(jobItem.Id);
                cmd.GetParameter(index++).Value = jobItem.ItemId;
                cmd.GetParameter(index++).Value = jobItem.ItemName;
                cmd.GetParameter(index++).Value = jobItem.MediaSourceId;
                cmd.GetParameter(index++).Value = jobItem.JobId;
                cmd.GetParameter(index++).Value = jobItem.TemporaryPath;
                cmd.GetParameter(index++).Value = jobItem.OutputPath;
                cmd.GetParameter(index++).Value = jobItem.Status.ToString();
                cmd.GetParameter(index++).Value = jobItem.TargetId;
                cmd.GetParameter(index++).Value = jobItem.DateCreated;
                cmd.GetParameter(index++).Value = jobItem.Progress;
                cmd.GetParameter(index++).Value = _json.SerializeToString(jobItem.AdditionalFiles);
                cmd.GetParameter(index++).Value = jobItem.MediaSource == null ? null : _json.SerializeToString(jobItem.MediaSource);
                cmd.GetParameter(index++).Value = jobItem.IsMarkedForRemoval;
                cmd.GetParameter(index++).Value = jobItem.JobItemIndex;
                cmd.GetParameter(index++).Value = jobItem.ItemDateModifiedTicks;

                cmd.Transaction = transaction;

                cmd.ExecuteNonQuery();

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
                Logger.ErrorException("Failed to save record:", e);

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

        private SyncJobItem GetJobItem(IDataReader reader)
        {
            var info = new SyncJobItem
            {
                Id = reader.GetGuid(0).ToString("N"),
                ItemId = reader.GetString(1)
            };

            if (!reader.IsDBNull(2))
            {
                info.ItemName = reader.GetString(2);
            }

            if (!reader.IsDBNull(3))
            {
                info.MediaSourceId = reader.GetString(3);
            }

            info.JobId = reader.GetString(4);

            if (!reader.IsDBNull(5))
            {
                info.TemporaryPath = reader.GetString(5);
            }
            if (!reader.IsDBNull(6))
            {
                info.OutputPath = reader.GetString(6);
            }

            if (!reader.IsDBNull(7))
            {
                info.Status = (SyncJobItemStatus)Enum.Parse(typeof(SyncJobItemStatus), reader.GetString(7), true);
            }

            info.TargetId = reader.GetString(8);

            info.DateCreated = reader.GetDateTime(9).ToUniversalTime();

            if (!reader.IsDBNull(10))
            {
                info.Progress = reader.GetDouble(10);
            }

            if (!reader.IsDBNull(11))
            {
                var json = reader.GetString(11);

                if (!string.IsNullOrWhiteSpace(json))
                {
                    info.AdditionalFiles = _json.DeserializeFromString<List<ItemFileInfo>>(json);
                }
            }

            if (!reader.IsDBNull(12))
            {
                var json = reader.GetString(12);

                if (!string.IsNullOrWhiteSpace(json))
                {
                    info.MediaSource = _json.DeserializeFromString<MediaSourceInfo>(json);
                }
            }

            info.IsMarkedForRemoval = reader.GetBoolean(13);
            info.JobItemIndex = reader.GetInt32(14);

            if (!reader.IsDBNull(15))
            {
                info.ItemDateModifiedTicks = reader.GetInt64(15);
            }

            return info;
        }

        private SyncedItemProgress GetSyncedItemProgress(IDataReader reader)
        {
            var item = new SyncedItemProgress();

            item.ItemId = reader.GetString(0);

            if (!reader.IsDBNull(1))
            {
                item.Status = (SyncJobItemStatus)Enum.Parse(typeof(SyncJobItemStatus), reader.GetString(1), true);
            }

            return item;
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
    }
}
