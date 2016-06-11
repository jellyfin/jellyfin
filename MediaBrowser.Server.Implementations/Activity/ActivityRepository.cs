using MediaBrowser.Controller;
using MediaBrowser.Controller.Activity;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using MediaBrowser.Server.Implementations.Persistence;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Activity
{
    public class ActivityRepository : BaseSqliteRepository, IActivityRepository
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public ActivityRepository(ILogManager logManager, IServerApplicationPaths appPaths, IDbConnector connector)
            : base(logManager, connector)
        {
            DbFilePath = Path.Combine(appPaths.DataPath, "activitylog.db");
        }

        public async Task Initialize()
        {
            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                string[] queries = {

                                "create table if not exists ActivityLogEntries (Id GUID PRIMARY KEY, Name TEXT, Overview TEXT, ShortOverview TEXT, Type TEXT, ItemId TEXT, UserId TEXT, DateCreated DATETIME, LogSeverity TEXT)",
                                "create index if not exists idx_ActivityLogEntries on ActivityLogEntries(Id)"
                               };

                connection.RunQueries(queries, Logger);
            }
        }

        private const string BaseActivitySelectText = "select Id, Name, Overview, ShortOverview, Type, ItemId, UserId, DateCreated, LogSeverity from ActivityLogEntries";

        public Task Create(ActivityLogEntry entry)
        {
            return Update(entry);
        }

        public async Task Update(ActivityLogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                using (var saveActivityCommand = connection.CreateCommand())
                {
                    saveActivityCommand.CommandText = "replace into ActivityLogEntries (Id, Name, Overview, ShortOverview, Type, ItemId, UserId, DateCreated, LogSeverity) values (@Id, @Name, @Overview, @ShortOverview, @Type, @ItemId, @UserId, @DateCreated, @LogSeverity)";

                    saveActivityCommand.Parameters.Add(saveActivityCommand, "@Id");
                    saveActivityCommand.Parameters.Add(saveActivityCommand, "@Name");
                    saveActivityCommand.Parameters.Add(saveActivityCommand, "@Overview");
                    saveActivityCommand.Parameters.Add(saveActivityCommand, "@ShortOverview");
                    saveActivityCommand.Parameters.Add(saveActivityCommand, "@Type");
                    saveActivityCommand.Parameters.Add(saveActivityCommand, "@ItemId");
                    saveActivityCommand.Parameters.Add(saveActivityCommand, "@UserId");
                    saveActivityCommand.Parameters.Add(saveActivityCommand, "@DateCreated");
                    saveActivityCommand.Parameters.Add(saveActivityCommand, "@LogSeverity");

                    IDbTransaction transaction = null;

                    try
                    {
                        transaction = connection.BeginTransaction();

                        var index = 0;

                        saveActivityCommand.GetParameter(index++).Value = new Guid(entry.Id);
                        saveActivityCommand.GetParameter(index++).Value = entry.Name;
                        saveActivityCommand.GetParameter(index++).Value = entry.Overview;
                        saveActivityCommand.GetParameter(index++).Value = entry.ShortOverview;
                        saveActivityCommand.GetParameter(index++).Value = entry.Type;
                        saveActivityCommand.GetParameter(index++).Value = entry.ItemId;
                        saveActivityCommand.GetParameter(index++).Value = entry.UserId;
                        saveActivityCommand.GetParameter(index++).Value = entry.Date;
                        saveActivityCommand.GetParameter(index++).Value = entry.Severity.ToString();

                        saveActivityCommand.Transaction = transaction;

                        saveActivityCommand.ExecuteNonQuery();

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

        public QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, int? startIndex, int? limit)
        {
            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = BaseActivitySelectText;

                    var whereClauses = new List<string>();

                    if (minDate.HasValue)
                    {
                        whereClauses.Add("DateCreated>=@DateCreated");
                        cmd.Parameters.Add(cmd, "@DateCreated", DbType.Date).Value = minDate.Value;
                    }

                    var whereTextWithoutPaging = whereClauses.Count == 0 ?
                        string.Empty :
                        " where " + string.Join(" AND ", whereClauses.ToArray());

                    if (startIndex.HasValue && startIndex.Value > 0)
                    {
                        var pagingWhereText = whereClauses.Count == 0 ?
                            string.Empty :
                            " where " + string.Join(" AND ", whereClauses.ToArray());

                        whereClauses.Add(string.Format("Id NOT IN (SELECT Id FROM ActivityLogEntries {0} ORDER BY DateCreated DESC LIMIT {1})",
                            pagingWhereText,
                            startIndex.Value.ToString(_usCulture)));
                    }

                    var whereText = whereClauses.Count == 0 ?
                        string.Empty :
                        " where " + string.Join(" AND ", whereClauses.ToArray());

                    cmd.CommandText += whereText;

                    cmd.CommandText += " ORDER BY DateCreated DESC";

                    if (limit.HasValue)
                    {
                        cmd.CommandText += " LIMIT " + limit.Value.ToString(_usCulture);
                    }

                    cmd.CommandText += "; select count (Id) from ActivityLogEntries" + whereTextWithoutPaging;

                    var list = new List<ActivityLogEntry>();
                    var count = 0;

                    using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        while (reader.Read())
                        {
                            list.Add(GetEntry(reader));
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            count = reader.GetInt32(0);
                        }
                    }

                    return new QueryResult<ActivityLogEntry>()
                    {
                        Items = list.ToArray(),
                        TotalRecordCount = count
                    };
                }
            }
        }

        private ActivityLogEntry GetEntry(IDataReader reader)
        {
            var index = 0;

            var info = new ActivityLogEntry
            {
                Id = reader.GetGuid(index).ToString("N")
            };

            index++;
            if (!reader.IsDBNull(index))
            {
                info.Name = reader.GetString(index);
            }

            index++;
            if (!reader.IsDBNull(index))
            {
                info.Overview = reader.GetString(index);
            }

            index++;
            if (!reader.IsDBNull(index))
            {
                info.ShortOverview = reader.GetString(index);
            }

            index++;
            if (!reader.IsDBNull(index))
            {
                info.Type = reader.GetString(index);
            }

            index++;
            if (!reader.IsDBNull(index))
            {
                info.ItemId = reader.GetString(index);
            }

            index++;
            if (!reader.IsDBNull(index))
            {
                info.UserId = reader.GetString(index);
            }

            index++;
            info.Date = reader.GetDateTime(index).ToUniversalTime();

            index++;
            if (!reader.IsDBNull(index))
            {
                info.Severity = (LogSeverity)Enum.Parse(typeof(LogSeverity), reader.GetString(index), true);
            }

            return info;
        }
    }
}
