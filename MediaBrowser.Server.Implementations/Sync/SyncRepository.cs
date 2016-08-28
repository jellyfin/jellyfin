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
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly IJsonSerializer _json;

        public SyncRepository(ILogManager logManager, IJsonSerializer json, IServerApplicationPaths appPaths, IDbConnector connector)
            : base(logManager, connector)
        {
            _json = json;
            DbFilePath = Path.Combine(appPaths.DataPath, "sync14.db");
        }

        private class SyncSummary
        {
            public Dictionary<string, int> Items { get; set; }

            public SyncSummary()
            {
                Items = new Dictionary<string, int>();
            }
        }


        public async Task Initialize()
        {
            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                string[] queries = {

                                "create table if not exists SyncJobs (Id GUID PRIMARY KEY, TargetId TEXT NOT NULL, Name TEXT NOT NULL, Profile TEXT, Quality TEXT, Bitrate INT, Status TEXT NOT NULL, Progress FLOAT, UserId TEXT NOT NULL, ItemIds TEXT NOT NULL, Category TEXT, ParentId TEXT, UnwatchedOnly BIT, ItemLimit INT, SyncNewContent BIT, DateCreated DateTime, DateLastModified DateTime, ItemCount int)",

                                "create table if not exists SyncJobItems (Id GUID PRIMARY KEY, ItemId TEXT, ItemName TEXT, MediaSourceId TEXT, JobId TEXT, TemporaryPath TEXT, OutputPath TEXT, Status TEXT, TargetId TEXT, DateCreated DateTime, Progress FLOAT, AdditionalFiles TEXT, MediaSource TEXT, IsMarkedForRemoval BIT, JobItemIndex INT, ItemDateModifiedTicks BIGINT)",

                                "drop index if exists idx_SyncJobItems2",
                                "drop index if exists idx_SyncJobItems3",
                                "drop index if exists idx_SyncJobs1",
                                "drop index if exists idx_SyncJobs",
                                "drop index if exists idx_SyncJobItems1",
                                "create index if not exists idx_SyncJobItems4 on SyncJobItems(TargetId,ItemId,Status,Progress,DateCreated)",
                                "create index if not exists idx_SyncJobItems5 on SyncJobItems(TargetId,Status,ItemId,Progress)",

                                "create index if not exists idx_SyncJobs2 on SyncJobs(TargetId,Status,ItemIds,Progress)",

                                "pragma shrink_memory"
                               };

                connection.RunQueries(queries, Logger);

                connection.AddColumn(Logger, "SyncJobs", "Profile", "TEXT");
                connection.AddColumn(Logger, "SyncJobs", "Bitrate", "INT");
                connection.AddColumn(Logger, "SyncJobItems", "ItemDateModifiedTicks", "BIGINT");
            }
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

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
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
            return InsertOrUpdate(job, true);
        }

        public Task Update(SyncJob job)
        {
            return InsertOrUpdate(job, false);
        }

        private async Task InsertOrUpdate(SyncJob job, bool insert)
        {
            if (job == null)
            {
                throw new ArgumentNullException("job");
            }

            CheckDisposed();

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                using (var cmd = connection.CreateCommand())
                {
                    if (insert)
                    {
                        cmd.CommandText = "insert into SyncJobs (Id, TargetId, Name, Profile, Quality, Bitrate, Status, Progress, UserId, ItemIds, Category, ParentId, UnwatchedOnly, ItemLimit, SyncNewContent, DateCreated, DateLastModified, ItemCount) values (@Id, @TargetId, @Name, @Profile, @Quality, @Bitrate, @Status, @Progress, @UserId, @ItemIds, @Category, @ParentId, @UnwatchedOnly, @ItemLimit, @SyncNewContent, @DateCreated, @DateLastModified, @ItemCount)";

                        cmd.Parameters.Add(cmd, "@Id");
                        cmd.Parameters.Add(cmd, "@TargetId");
                        cmd.Parameters.Add(cmd, "@Name");
                        cmd.Parameters.Add(cmd, "@Profile");
                        cmd.Parameters.Add(cmd, "@Quality");
                        cmd.Parameters.Add(cmd, "@Bitrate");
                        cmd.Parameters.Add(cmd, "@Status");
                        cmd.Parameters.Add(cmd, "@Progress");
                        cmd.Parameters.Add(cmd, "@UserId");
                        cmd.Parameters.Add(cmd, "@ItemIds");
                        cmd.Parameters.Add(cmd, "@Category");
                        cmd.Parameters.Add(cmd, "@ParentId");
                        cmd.Parameters.Add(cmd, "@UnwatchedOnly");
                        cmd.Parameters.Add(cmd, "@ItemLimit");
                        cmd.Parameters.Add(cmd, "@SyncNewContent");
                        cmd.Parameters.Add(cmd, "@DateCreated");
                        cmd.Parameters.Add(cmd, "@DateLastModified");
                        cmd.Parameters.Add(cmd, "@ItemCount");
                    }
                    else
                    {
                        cmd.CommandText = "update SyncJobs set TargetId=@TargetId,Name=@Name,Profile=@Profile,Quality=@Quality,Bitrate=@Bitrate,Status=@Status,Progress=@Progress,UserId=@UserId,ItemIds=@ItemIds,Category=@Category,ParentId=@ParentId,UnwatchedOnly=@UnwatchedOnly,ItemLimit=@ItemLimit,SyncNewContent=@SyncNewContent,DateCreated=@DateCreated,DateLastModified=@DateLastModified,ItemCount=@ItemCount where Id=@Id";

                        cmd.Parameters.Add(cmd, "@Id");
                        cmd.Parameters.Add(cmd, "@TargetId");
                        cmd.Parameters.Add(cmd, "@Name");
                        cmd.Parameters.Add(cmd, "@Profile");
                        cmd.Parameters.Add(cmd, "@Quality");
                        cmd.Parameters.Add(cmd, "@Bitrate");
                        cmd.Parameters.Add(cmd, "@Status");
                        cmd.Parameters.Add(cmd, "@Progress");
                        cmd.Parameters.Add(cmd, "@UserId");
                        cmd.Parameters.Add(cmd, "@ItemIds");
                        cmd.Parameters.Add(cmd, "@Category");
                        cmd.Parameters.Add(cmd, "@ParentId");
                        cmd.Parameters.Add(cmd, "@UnwatchedOnly");
                        cmd.Parameters.Add(cmd, "@ItemLimit");
                        cmd.Parameters.Add(cmd, "@SyncNewContent");
                        cmd.Parameters.Add(cmd, "@DateCreated");
                        cmd.Parameters.Add(cmd, "@DateLastModified");
                        cmd.Parameters.Add(cmd, "@ItemCount");
                    }

                    IDbTransaction transaction = null;

                    try
                    {
                        transaction = connection.BeginTransaction();

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
                    }
                }
            }
        }

        public async Task DeleteJob(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException("id");
            }

            CheckDisposed();

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                using (var deleteJobCommand = connection.CreateCommand())
                {
                    using (var deleteJobItemsCommand = connection.CreateCommand())
                    {
                        IDbTransaction transaction = null;

                        try
                        {
                            // _deleteJobCommand
                            deleteJobCommand.CommandText = "delete from SyncJobs where Id=@Id";
                            deleteJobCommand.Parameters.Add(deleteJobCommand, "@Id");

                            transaction = connection.BeginTransaction();

                            deleteJobCommand.GetParameter(0).Value = new Guid(id);
                            deleteJobCommand.Transaction = transaction;
                            deleteJobCommand.ExecuteNonQuery();

                            // _deleteJobItemsCommand
                            deleteJobItemsCommand.CommandText = "delete from SyncJobItems where JobId=@JobId";
                            deleteJobItemsCommand.Parameters.Add(deleteJobItemsCommand, "@JobId");

                            deleteJobItemsCommand.GetParameter(0).Value = id;
                            deleteJobItemsCommand.Transaction = transaction;
                            deleteJobItemsCommand.ExecuteNonQuery();

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
                        }
                    }
                }
            }
        }

        public QueryResult<SyncJob> GetJobs(SyncJobQuery query)
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
                    if (!string.IsNullOrWhiteSpace(query.ExcludeTargetIds))
                    {
                        var excludeIds = (query.ExcludeTargetIds ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (excludeIds.Length == 1)
                        {
                            whereClauses.Add("TargetId<>@ExcludeTargetId");
                            cmd.Parameters.Add(cmd, "@ExcludeTargetId", DbType.String).Value = excludeIds[0];
                        }
                        else if (excludeIds.Length > 1)
                        {
                            whereClauses.Add("TargetId<>@ExcludeTargetId");
                            cmd.Parameters.Add(cmd, "@ExcludeTargetId", DbType.String).Value = excludeIds[0];
                        }
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
        }

        public SyncJobItem GetJobItem(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            CheckDisposed();

            var guid = new Guid(id);

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
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
        }

        private QueryResult<T> GetJobItemReader<T>(SyncJobItemQuery query, string baseSelectText, Func<IDataReader, T> itemFactory)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
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
        }

        public Dictionary<string, SyncedItemProgress> GetSyncedItemProgresses(SyncJobItemQuery query)
        {
            var result = new Dictionary<string, SyncedItemProgress>();

            var now = DateTime.UtcNow;

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select ItemId,Status,Progress from SyncJobItems";

                    var whereClauses = new List<string>();

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

                    if (whereClauses.Count > 0)
                    {
                        cmd.CommandText += " where " + string.Join(" AND ", whereClauses.ToArray());
                    }

                    cmd.CommandText += ";" + cmd.CommandText
                        .Replace("select ItemId,Status,Progress from SyncJobItems", "select ItemIds,Status,Progress from SyncJobs")
                        .Replace("'Synced'", "'Completed','CompletedWithError'");

                    //Logger.Debug(cmd.CommandText);

                    using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        LogQueryTime("GetSyncedItemProgresses", cmd, now);

                        while (reader.Read())
                        {
                            AddStatusResult(reader, result, false);
                        }

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                AddStatusResult(reader, result, true);
                            }
                        }
                    }
                }
            }

            return result;
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

        private void AddStatusResult(IDataReader reader, Dictionary<string, SyncedItemProgress> result, bool multipleIds)
        {
            if (reader.IsDBNull(0))
            {
                return;
            }

            var itemIds = new List<string>();

            var ids = reader.GetString(0);

            if (multipleIds)
            {
                itemIds = ids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                itemIds.Add(ids);
            }

            if (!reader.IsDBNull(1))
            {
                SyncJobItemStatus status;
                var statusString = reader.GetString(1);
                if (string.Equals(statusString, "Completed", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(statusString, "CompletedWithError", StringComparison.OrdinalIgnoreCase))
                {
                    status = SyncJobItemStatus.Synced;
                }
                else
                {
                    status = (SyncJobItemStatus)Enum.Parse(typeof(SyncJobItemStatus), statusString, true);
                }

                if (status == SyncJobItemStatus.Synced)
                {
                    foreach (var itemId in itemIds)
                    {
                        result[itemId] = new SyncedItemProgress
                        {
                            Status = SyncJobItemStatus.Synced
                        };
                    }
                }
                else
                {
                    double progress = reader.IsDBNull(2) ? 0.0 : reader.GetDouble(2);

                    foreach (var itemId in itemIds)
                    {
                        SyncedItemProgress currentStatus;
                        if (!result.TryGetValue(itemId, out currentStatus) || (currentStatus.Status != SyncJobItemStatus.Synced && progress >= currentStatus.Progress))
                        {
                            result[itemId] = new SyncedItemProgress
                            {
                                Status = status,
                                Progress = progress
                            };
                        }
                    }
                }
            }
        }

        public QueryResult<SyncJobItem> GetJobItems(SyncJobItemQuery query)
        {
            return GetJobItemReader(query, BaseJobItemSelectText, GetJobItem);
        }

        public Task Create(SyncJobItem jobItem)
        {
            return InsertOrUpdate(jobItem, true);
        }

        public Task Update(SyncJobItem jobItem)
        {
            return InsertOrUpdate(jobItem, false);
        }

        private async Task InsertOrUpdate(SyncJobItem jobItem, bool insert)
        {
            if (jobItem == null)
            {
                throw new ArgumentNullException("jobItem");
            }

            CheckDisposed();

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                using (var cmd = connection.CreateCommand())
                {
                    if (insert)
                    {
                        cmd.CommandText = "insert into SyncJobItems (Id, ItemId, ItemName, MediaSourceId, JobId, TemporaryPath, OutputPath, Status, TargetId, DateCreated, Progress, AdditionalFiles, MediaSource, IsMarkedForRemoval, JobItemIndex, ItemDateModifiedTicks) values (@Id, @ItemId, @ItemName, @MediaSourceId, @JobId, @TemporaryPath, @OutputPath, @Status, @TargetId, @DateCreated, @Progress, @AdditionalFiles, @MediaSource, @IsMarkedForRemoval, @JobItemIndex, @ItemDateModifiedTicks)";

                        cmd.Parameters.Add(cmd, "@Id");
                        cmd.Parameters.Add(cmd, "@ItemId");
                        cmd.Parameters.Add(cmd, "@ItemName");
                        cmd.Parameters.Add(cmd, "@MediaSourceId");
                        cmd.Parameters.Add(cmd, "@JobId");
                        cmd.Parameters.Add(cmd, "@TemporaryPath");
                        cmd.Parameters.Add(cmd, "@OutputPath");
                        cmd.Parameters.Add(cmd, "@Status");
                        cmd.Parameters.Add(cmd, "@TargetId");
                        cmd.Parameters.Add(cmd, "@DateCreated");
                        cmd.Parameters.Add(cmd, "@Progress");
                        cmd.Parameters.Add(cmd, "@AdditionalFiles");
                        cmd.Parameters.Add(cmd, "@MediaSource");
                        cmd.Parameters.Add(cmd, "@IsMarkedForRemoval");
                        cmd.Parameters.Add(cmd, "@JobItemIndex");
                        cmd.Parameters.Add(cmd, "@ItemDateModifiedTicks");
                    }
                    else
                    {
                        // cmd
                        cmd.CommandText = "update SyncJobItems set ItemId=@ItemId,ItemName=@ItemName,MediaSourceId=@MediaSourceId,JobId=@JobId,TemporaryPath=@TemporaryPath,OutputPath=@OutputPath,Status=@Status,TargetId=@TargetId,DateCreated=@DateCreated,Progress=@Progress,AdditionalFiles=@AdditionalFiles,MediaSource=@MediaSource,IsMarkedForRemoval=@IsMarkedForRemoval,JobItemIndex=@JobItemIndex,ItemDateModifiedTicks=@ItemDateModifiedTicks where Id=@Id";

                        cmd.Parameters.Add(cmd, "@Id");
                        cmd.Parameters.Add(cmd, "@ItemId");
                        cmd.Parameters.Add(cmd, "@ItemName");
                        cmd.Parameters.Add(cmd, "@MediaSourceId");
                        cmd.Parameters.Add(cmd, "@JobId");
                        cmd.Parameters.Add(cmd, "@TemporaryPath");
                        cmd.Parameters.Add(cmd, "@OutputPath");
                        cmd.Parameters.Add(cmd, "@Status");
                        cmd.Parameters.Add(cmd, "@TargetId");
                        cmd.Parameters.Add(cmd, "@DateCreated");
                        cmd.Parameters.Add(cmd, "@Progress");
                        cmd.Parameters.Add(cmd, "@AdditionalFiles");
                        cmd.Parameters.Add(cmd, "@MediaSource");
                        cmd.Parameters.Add(cmd, "@IsMarkedForRemoval");
                        cmd.Parameters.Add(cmd, "@JobItemIndex");
                        cmd.Parameters.Add(cmd, "@ItemDateModifiedTicks");
                    }

                    IDbTransaction transaction = null;

                    try
                    {
                        transaction = connection.BeginTransaction();

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
                    }
                }
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
    }
}
