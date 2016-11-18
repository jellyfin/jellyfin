using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using MediaBrowser.Controller;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Activity
{
    public class ActivityRepository : BaseSqliteRepository, IActivityRepository
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public ActivityRepository(ILogger logger, IServerApplicationPaths appPaths)
            : base(logger)
        {
            DbFilePath = Path.Combine(appPaths.DataPath, "activitylog.db");
        }

        public void Initialize()
        {
            using (var connection = CreateConnection())
            {
                string[] queries = {

                                "create table if not exists ActivityLogEntries (Id GUID PRIMARY KEY, Name TEXT, Overview TEXT, ShortOverview TEXT, Type TEXT, ItemId TEXT, UserId TEXT, DateCreated DATETIME, LogSeverity TEXT)",
                                "create index if not exists idx_ActivityLogEntries on ActivityLogEntries(Id)"
                               };

                connection.RunQueries(queries);
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

            lock (WriteLock)
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        var commandText = "replace into ActivityLogEntries (Id, Name, Overview, ShortOverview, Type, ItemId, UserId, DateCreated, LogSeverity) values (?, ?, ?, ?, ?, ?, ?, ?, ?)";

                        db.Execute(commandText,
                            entry.Id.ToGuidParamValue(),
                            entry.Name,
                            entry.Overview,
                            entry.ShortOverview,
                            entry.Type,
                            entry.ItemId,
                            entry.UserId,
                            entry.Date.ToDateTimeParamValue(),
                            entry.Severity.ToString());
                    });
                }
            }
        }

        public QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, int? startIndex, int? limit)
        {
            lock (WriteLock)
            {
                using (var connection = CreateConnection(true))
                {
                    var commandText = BaseActivitySelectText;

                    var whereClauses = new List<string>();
                    var paramList = new List<object>();

                    if (minDate.HasValue)
                    {
                        whereClauses.Add("DateCreated>=@DateCreated");
                        paramList.Add(minDate.Value.ToDateTimeParamValue());
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

                    commandText += whereText;

                    commandText += " ORDER BY DateCreated DESC";

                    if (limit.HasValue)
                    {
                        commandText += " LIMIT " + limit.Value.ToString(_usCulture);
                    }

                    var totalRecordCount = connection.Query("select count (Id) from ActivityLogEntries" + whereTextWithoutPaging, paramList.ToArray()).SelectScalarInt().First();

                    var list = new List<ActivityLogEntry>();

                    foreach (var row in connection.Query(commandText, paramList.ToArray()))
                    {
                        list.Add(GetEntry(row));
                    }

                    return new QueryResult<ActivityLogEntry>()
                    {
                        Items = list.ToArray(),
                        TotalRecordCount = totalRecordCount
                    };
                }
            }
        }

        private ActivityLogEntry GetEntry(IReadOnlyList<IResultSetValue> reader)
        {
            var index = 0;

            var info = new ActivityLogEntry
            {
                Id = reader[index].ReadGuid().ToString("N")
            };

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.Name = reader[index].ToString();
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.Overview = reader[index].ToString();
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.ShortOverview = reader[index].ToString();
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.Type = reader[index].ToString();
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.ItemId = reader[index].ToString();
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.UserId = reader[index].ToString();
            }

            index++;
            info.Date = reader[index].ReadDateTime();

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.Severity = (LogSeverity)Enum.Parse(typeof(LogSeverity), reader[index].ToString(), true);
            }

            return info;
        }
    }
}
