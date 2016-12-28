using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Sync;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Sync
{
    public class SyncRepository : BaseSqliteRepository, ISyncRepository
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private readonly IJsonSerializer _json;

        public SyncRepository(ILogger logger, IJsonSerializer json, IServerApplicationPaths appPaths)
            : base(logger)
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

        public void Initialize()
        {
            using (var connection = CreateConnection())
            {
                RunDefaultInitialization(connection);

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

                connection.RunQueries(queries);

                connection.RunInTransaction(db =>
                {
                    var existingColumnNames = GetColumnNames(db, "SyncJobs");
                    AddColumn(db, "SyncJobs", "Profile", "TEXT", existingColumnNames);
                    AddColumn(db, "SyncJobs", "Bitrate", "INT", existingColumnNames);

                    existingColumnNames = GetColumnNames(db, "SyncJobItems");
                    AddColumn(db, "SyncJobItems", "ItemDateModifiedTicks", "BIGINT", existingColumnNames);
                }, TransactionMode);
            }
        }

        protected override bool EnableTempStoreMemory
        {
            get
            {
                return true;
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

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var commandText = BaseJobSelectText + " where Id=?";
                    var paramList = new List<object>();

                    paramList.Add(guid.ToGuidParamValue());

                    foreach (var row in connection.Query(commandText, paramList.ToArray()))
                    {
                        return GetJob(row);
                    }

                    return null;
                }
            }
        }

        private SyncJob GetJob(IReadOnlyList<IResultSetValue> reader)
        {
            var info = new SyncJob
            {
                Id = reader[0].ReadGuid().ToString("N"),
                TargetId = reader[1].ToString(),
                Name = reader[2].ToString()
            };

            if (reader[3].SQLiteType != SQLiteType.Null)
            {
                info.Profile = reader[3].ToString();
            }

            if (reader[4].SQLiteType != SQLiteType.Null)
            {
                info.Quality = reader[4].ToString();
            }

            if (reader[5].SQLiteType != SQLiteType.Null)
            {
                info.Bitrate = reader[5].ToInt();
            }

            if (reader[6].SQLiteType != SQLiteType.Null)
            {
                info.Status = (SyncJobStatus)Enum.Parse(typeof(SyncJobStatus), reader[6].ToString(), true);
            }

            if (reader[7].SQLiteType != SQLiteType.Null)
            {
                info.Progress = reader[7].ToDouble();
            }

            if (reader[8].SQLiteType != SQLiteType.Null)
            {
                info.UserId = reader[8].ToString();
            }

            if (reader[9].SQLiteType != SQLiteType.Null)
            {
                info.RequestedItemIds = reader[9].ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (reader[10].SQLiteType != SQLiteType.Null)
            {
                info.Category = (SyncCategory)Enum.Parse(typeof(SyncCategory), reader[10].ToString(), true);
            }

            if (reader[11].SQLiteType != SQLiteType.Null)
            {
                info.ParentId = reader[11].ToString();
            }

            if (reader[12].SQLiteType != SQLiteType.Null)
            {
                info.UnwatchedOnly = reader[12].ToBool();
            }

            if (reader[13].SQLiteType != SQLiteType.Null)
            {
                info.ItemLimit = reader[13].ToInt();
            }

            info.SyncNewContent = reader[14].ToBool();

            info.DateCreated = reader[15].ReadDateTime();
            info.DateLastModified = reader[16].ReadDateTime();
            info.ItemCount = reader[17].ToInt();

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

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    string commandText;

                    if (insert)
                    {
                        commandText = "insert into SyncJobs (Id, TargetId, Name, Profile, Quality, Bitrate, Status, Progress, UserId, ItemIds, Category, ParentId, UnwatchedOnly, ItemLimit, SyncNewContent, DateCreated, DateLastModified, ItemCount) values (@Id, @TargetId, @Name, @Profile, @Quality, @Bitrate, @Status, @Progress, @UserId, @ItemIds, @Category, @ParentId, @UnwatchedOnly, @ItemLimit, @SyncNewContent, @DateCreated, @DateLastModified, @ItemCount)";
                    }
                    else
                    {
                        commandText = "update SyncJobs set TargetId=@TargetId,Name=@Name,Profile=@Profile,Quality=@Quality,Bitrate=@Bitrate,Status=@Status,Progress=@Progress,UserId=@UserId,ItemIds=@ItemIds,Category=@Category,ParentId=@ParentId,UnwatchedOnly=@UnwatchedOnly,ItemLimit=@ItemLimit,SyncNewContent=@SyncNewContent,DateCreated=@DateCreated,DateLastModified=@DateLastModified,ItemCount=@ItemCount where Id=@Id";
                    }

                    connection.RunInTransaction(conn =>
                    {
                        using (var statement = PrepareStatementSafe(connection, commandText))
                        {
                            statement.TryBind("@TargetId", job.TargetId);
                            statement.TryBind("@Name", job.Name);
                            statement.TryBind("@Profile", job.Profile);
                            statement.TryBind("@Quality", job.Quality);
                            statement.TryBind("@Bitrate", job.Bitrate);
                            statement.TryBind("@Status", job.Status.ToString());
                            statement.TryBind("@Progress", job.Progress);
                            statement.TryBind("@UserId", job.UserId);

                            statement.TryBind("@ItemIds", string.Join(",", job.RequestedItemIds.ToArray()));

                            if (job.Category.HasValue)
                            {
                                statement.TryBind("@Category", job.Category.Value.ToString());
                            }
                            else
                            {
                                statement.TryBindNull("@Category");
                            }

                            if (!string.IsNullOrWhiteSpace(job.ParentId))
                            {
                                statement.TryBind("@ParentId", job.ParentId);
                            }
                            else
                            {
                                statement.TryBindNull("@ParentId");
                            }

                            statement.TryBind("@UnwatchedOnly", job.UnwatchedOnly);

                            if (job.ItemLimit.HasValue)
                            {
                                statement.TryBind("@ItemLimit", job.ItemLimit);
                            }
                            else
                            {
                                statement.TryBindNull("@ItemLimit");
                            }

                            statement.TryBind("@SyncNewContent", job.SyncNewContent);

                            statement.TryBind("@DateCreated", job.DateCreated.ToDateTimeParamValue());
                            statement.TryBind("@DateLastModified", job.DateLastModified.ToDateTimeParamValue());

                            statement.TryBind("@ItemCount", job.ItemCount);
                            statement.TryBind("@Id", job.Id.ToGuidParamValue());

                            statement.MoveNext();
                        }
                    }, TransactionMode);
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

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(conn =>
                    {
                        conn.Execute("delete from SyncJobs where Id=?", id.ToGuidParamValue());
                        conn.Execute("delete from SyncJobItems where JobId=?", id);
                    }, TransactionMode);
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

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var commandText = BaseJobSelectText;
                    var paramList = new List<object>();

                    var whereClauses = new List<string>();

                    if (query.Statuses.Length > 0)
                    {
                        var statuses = string.Join(",", query.Statuses.Select(i => "'" + i.ToString() + "'").ToArray());

                        whereClauses.Add(string.Format("Status in ({0})", statuses));
                    }
                    if (!string.IsNullOrWhiteSpace(query.TargetId))
                    {
                        whereClauses.Add("TargetId=?");
                        paramList.Add(query.TargetId);
                    }
                    if (!string.IsNullOrWhiteSpace(query.ExcludeTargetIds))
                    {
                        var excludeIds = (query.ExcludeTargetIds ?? string.Empty).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (excludeIds.Length == 1)
                        {
                            whereClauses.Add("TargetId<>?");
                            paramList.Add(excludeIds[0]);
                        }
                        else if (excludeIds.Length > 1)
                        {
                            whereClauses.Add("TargetId<>?");
                            paramList.Add(excludeIds[0]);
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(query.UserId))
                    {
                        whereClauses.Add("UserId=?");
                        paramList.Add(query.UserId);
                    }
                    if (!string.IsNullOrWhiteSpace(query.ItemId))
                    {
                        whereClauses.Add("ItemIds like ?");
                        paramList.Add("%" + query.ItemId + "%");
                    }
                    if (query.SyncNewContent.HasValue)
                    {
                        whereClauses.Add("SyncNewContent=?");
                        paramList.Add(query.SyncNewContent.Value);
                    }

                    commandText += " mainTable";

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
                        commandText += " where " + string.Join(" AND ", whereClauses.ToArray());
                    }

                    commandText += " ORDER BY (Select Max(DateLastModified) from SyncJobs where TargetId=mainTable.TargetId) DESC, DateLastModified DESC";

                    if (query.Limit.HasValue)
                    {
                        commandText += " LIMIT " + query.Limit.Value.ToString(_usCulture);
                    }

                    var list = new List<SyncJob>();
                    var count = connection.Query("select count (Id) from SyncJobs" + whereTextWithoutPaging, paramList.ToArray())
                            .SelectScalarInt()
                            .First();

                    foreach (var row in connection.Query(commandText, paramList.ToArray()))
                    {
                        list.Add(GetJob(row));
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

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var commandText = BaseJobItemSelectText + " where Id=?";
                    var paramList = new List<object>();

                    paramList.Add(guid.ToGuidParamValue());

                    foreach (var row in connection.Query(commandText, paramList.ToArray()))
                    {
                        return GetJobItem(row);
                    }

                    return null;
                }
            }
        }

        private QueryResult<T> GetJobItemReader<T>(SyncJobItemQuery query, string baseSelectText, Func<IReadOnlyList<IResultSetValue>, T> itemFactory)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var commandText = baseSelectText;
                    var paramList = new List<object>();

                    var whereClauses = new List<string>();

                    if (!string.IsNullOrWhiteSpace(query.JobId))
                    {
                        whereClauses.Add("JobId=?");
                        paramList.Add(query.JobId);
                    }
                    if (!string.IsNullOrWhiteSpace(query.ItemId))
                    {
                        whereClauses.Add("ItemId=?");
                        paramList.Add(query.ItemId);
                    }
                    if (!string.IsNullOrWhiteSpace(query.TargetId))
                    {
                        whereClauses.Add("TargetId=?");
                        paramList.Add(query.TargetId);
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
                        commandText += " where " + string.Join(" AND ", whereClauses.ToArray());
                    }

                    commandText += " ORDER BY JobItemIndex, DateCreated";

                    if (query.Limit.HasValue)
                    {
                        commandText += " LIMIT " + query.Limit.Value.ToString(_usCulture);
                    }

                    var list = new List<T>();
                    var count = connection.Query("select count (Id) from SyncJobItems" + whereTextWithoutPaging, paramList.ToArray())
                            .SelectScalarInt()
                            .First();

                    foreach (var row in connection.Query(commandText, paramList.ToArray()))
                    {
                        list.Add(itemFactory(row));
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

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var commandText = "select ItemId,Status,Progress from SyncJobItems";
                    var whereClauses = new List<string>();

                    if (!string.IsNullOrWhiteSpace(query.TargetId))
                    {
                        whereClauses.Add("TargetId=@TargetId");
                    }

                    if (query.Statuses.Length > 0)
                    {
                        var statuses = string.Join(",", query.Statuses.Select(i => "'" + i.ToString() + "'").ToArray());

                        whereClauses.Add(string.Format("Status in ({0})", statuses));
                    }

                    if (whereClauses.Count > 0)
                    {
                        commandText += " where " + string.Join(" AND ", whereClauses.ToArray());
                    }

                    var statementTexts = new List<string>
                {
                    commandText
                };

                    commandText = commandText
                        .Replace("select ItemId,Status,Progress from SyncJobItems", "select ItemIds,Status,Progress from SyncJobs")
                        .Replace("'Synced'", "'Completed','CompletedWithError'");

                    statementTexts.Add(commandText);

                    var statements = connection.PrepareAll(string.Join(";", statementTexts.ToArray()))
                        .ToList();

                    using (var statement = statements[0])
                    {
                        if (!string.IsNullOrWhiteSpace(query.TargetId))
                        {
                            statement.TryBind("@TargetId", query.TargetId);
                        }

                        foreach (var row in statement.ExecuteQuery())
                        {
                            AddStatusResult(row, result, false);
                        }
                        LogQueryTime("GetSyncedItemProgresses", commandText, now);
                    }

                    now = DateTime.UtcNow;

                    using (var statement = statements[1])
                    {
                        if (!string.IsNullOrWhiteSpace(query.TargetId))
                        {
                            statement.TryBind("@TargetId", query.TargetId);
                        }

                        foreach (var row in statement.ExecuteQuery())
                        {
                            AddStatusResult(row, result, true);
                        }
                        LogQueryTime("GetSyncedItemProgresses", commandText, now);
                    }
                }
            }

            return result;
        }

        private void LogQueryTime(string methodName, string commandText, DateTime startDate)
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
                    commandText,
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

        private void AddStatusResult(IReadOnlyList<IResultSetValue> reader, Dictionary<string, SyncedItemProgress> result, bool multipleIds)
        {
            if (reader[0].SQLiteType == SQLiteType.Null)
            {
                return;
            }

            var itemIds = new List<string>();

            var ids = reader[0].ToString();

            if (multipleIds)
            {
                itemIds = ids.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }
            else
            {
                itemIds.Add(ids);
            }

            if (reader[1].SQLiteType != SQLiteType.Null)
            {
                SyncJobItemStatus status;
                var statusString = reader[1].ToString();
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
                    double progress = reader[2].SQLiteType == SQLiteType.Null ? 0.0 : reader[2].ToDouble();

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

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    string commandText;

                    if (insert)
                    {
                        commandText = "insert into SyncJobItems (Id, ItemId, ItemName, MediaSourceId, JobId, TemporaryPath, OutputPath, Status, TargetId, DateCreated, Progress, AdditionalFiles, MediaSource, IsMarkedForRemoval, JobItemIndex, ItemDateModifiedTicks) values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    }
                    else
                    {
                        // cmd
                        commandText = "update SyncJobItems set ItemId=?,ItemName=?,MediaSourceId=?,JobId=?,TemporaryPath=?,OutputPath=?,Status=?,TargetId=?,DateCreated=?,Progress=?,AdditionalFiles=?,MediaSource=?,IsMarkedForRemoval=?,JobItemIndex=?,ItemDateModifiedTicks=? where Id=?";
                    }

                    var paramList = new List<object>();
                    paramList.Add(jobItem.ItemId);
                    paramList.Add(jobItem.ItemName);
                    paramList.Add(jobItem.MediaSourceId);
                    paramList.Add(jobItem.JobId);
                    paramList.Add(jobItem.TemporaryPath);
                    paramList.Add(jobItem.OutputPath);
                    paramList.Add(jobItem.Status.ToString());

                    paramList.Add(jobItem.TargetId);
                    paramList.Add(jobItem.DateCreated.ToDateTimeParamValue());
                    paramList.Add(jobItem.Progress);
                    paramList.Add(_json.SerializeToString(jobItem.AdditionalFiles));
                    paramList.Add(jobItem.MediaSource == null ? null : _json.SerializeToString(jobItem.MediaSource));
                    paramList.Add(jobItem.IsMarkedForRemoval);
                    paramList.Add(jobItem.JobItemIndex);
                    paramList.Add(jobItem.ItemDateModifiedTicks);

                    if (insert)
                    {
                        paramList.Insert(0, jobItem.Id.ToGuidParamValue());
                    }
                    else
                    {
                        paramList.Add(jobItem.Id.ToGuidParamValue());
                    }

                    connection.RunInTransaction(conn =>
                    {
                        conn.Execute(commandText, paramList.ToArray());
                    }, TransactionMode);
                }
            }
        }

        private SyncJobItem GetJobItem(IReadOnlyList<IResultSetValue> reader)
        {
            var info = new SyncJobItem
            {
                Id = reader[0].ReadGuid().ToString("N"),
                ItemId = reader[1].ToString()
            };

            if (reader[2].SQLiteType != SQLiteType.Null)
            {
                info.ItemName = reader[2].ToString();
            }

            if (reader[3].SQLiteType != SQLiteType.Null)
            {
                info.MediaSourceId = reader[3].ToString();
            }

            info.JobId = reader[4].ToString();

            if (reader[5].SQLiteType != SQLiteType.Null)
            {
                info.TemporaryPath = reader[5].ToString();
            }
            if (reader[6].SQLiteType != SQLiteType.Null)
            {
                info.OutputPath = reader[6].ToString();
            }

            if (reader[7].SQLiteType != SQLiteType.Null)
            {
                info.Status = (SyncJobItemStatus)Enum.Parse(typeof(SyncJobItemStatus), reader[7].ToString(), true);
            }

            info.TargetId = reader[8].ToString();

            info.DateCreated = reader[9].ReadDateTime();

            if (reader[10].SQLiteType != SQLiteType.Null)
            {
                info.Progress = reader[10].ToDouble();
            }

            if (reader[11].SQLiteType != SQLiteType.Null)
            {
                var json = reader[11].ToString();

                if (!string.IsNullOrWhiteSpace(json))
                {
                    info.AdditionalFiles = _json.DeserializeFromString<List<ItemFileInfo>>(json);
                }
            }

            if (reader[12].SQLiteType != SQLiteType.Null)
            {
                var json = reader[12].ToString();

                if (!string.IsNullOrWhiteSpace(json))
                {
                    info.MediaSource = _json.DeserializeFromString<MediaSourceInfo>(json);
                }
            }

            info.IsMarkedForRemoval = reader[13].ToBool();
            info.JobItemIndex = reader[14].ToInt();

            if (reader[15].SQLiteType != SQLiteType.Null)
            {
                info.ItemDateModifiedTicks = reader[15].ToInt64();
            }

            return info;
        }
    }
}
