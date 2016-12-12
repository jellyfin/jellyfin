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
                RunDefaultInitialization(connection);

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

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        using (var statement = db.PrepareStatement("replace into ActivityLogEntries (Id, Name, Overview, ShortOverview, Type, ItemId, UserId, DateCreated, LogSeverity) values (@Id, @Name, @Overview, @ShortOverview, @Type, @ItemId, @UserId, @DateCreated, @LogSeverity)"))
                        {
                            statement.TryBind("@Id", entry.Id.ToGuidParamValue());
                            statement.TryBind("@Name", entry.Name);

                            statement.TryBind("@Overview", entry.Overview);
                            statement.TryBind("@ShortOverview", entry.ShortOverview);
                            statement.TryBind("@Type", entry.Type);
                            statement.TryBind("@ItemId", entry.ItemId);
                            statement.TryBind("@UserId", entry.UserId);
                            statement.TryBind("@DateCreated", entry.Date.ToDateTimeParamValue());
                            statement.TryBind("@LogSeverity", entry.Severity.ToString());

                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
        }

        public QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, int? startIndex, int? limit)
        {
            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var list = new List<ActivityLogEntry>();
                    int totalRecordCount = 0;

                    var commandText = BaseActivitySelectText;
                    var whereClauses = new List<string>();

                    if (minDate.HasValue)
                    {
                        whereClauses.Add("DateCreated>=@DateCreated");
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

                    var statementTexts = new List<string>();
                    statementTexts.Add(commandText);
                    statementTexts.Add("select count (Id) from ActivityLogEntries" + whereTextWithoutPaging);

                    connection.RunInTransaction(db =>
                    {
                        var statements = PrepareAllSafe(db, string.Join(";", statementTexts.ToArray())).ToList();

                        using (var statement = statements[0])
                        {
                            if (minDate.HasValue)
                            {
                                statement.TryBind("@DateCreated", minDate.Value.ToDateTimeParamValue());
                            }

                            foreach (var row in statement.ExecuteQuery())
                            {
                                list.Add(GetEntry(row));
                            }
                        }

                        using (var statement = statements[1])
                        {
                            if (minDate.HasValue)
                            {
                                statement.TryBind("@DateCreated", minDate.Value.ToDateTimeParamValue());
                            }

                            totalRecordCount = statement.ExecuteQuery().SelectScalarInt().First();
                        }
                    }, ReadTransactionMode);

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
