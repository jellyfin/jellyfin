#pragma warning disable CS1591
#pragma warning disable SA1600

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Data;
using MediaBrowser.Controller;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Activity
{
    public class ActivityRepository : BaseSqliteRepository, IActivityRepository
    {
        private static readonly CultureInfo _usCulture = CultureInfo.ReadOnly(new CultureInfo("en-US"));
        private readonly IFileSystem _fileSystem;

        public ActivityRepository(ILoggerFactory loggerFactory, IServerApplicationPaths appPaths, IFileSystem fileSystem)
            : base(loggerFactory.CreateLogger(nameof(ActivityRepository)))
        {
            DbFilePath = Path.Combine(appPaths.DataPath, "activitylog.db");
            _fileSystem = fileSystem;
        }

        public void Initialize()
        {
            try
            {
                InitializeInternal();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error loading database file. Will reset and retry.");

                _fileSystem.DeleteFile(DbFilePath);

                InitializeInternal();
            }
        }

        private void InitializeInternal()
        {
            using (var connection = GetConnection())
            {
                connection.RunQueries(new[]
                {
                    "create table if not exists ActivityLog (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Overview TEXT, ShortOverview TEXT, Type TEXT NOT NULL, ItemId TEXT, UserId TEXT, DateCreated DATETIME NOT NULL, LogSeverity TEXT NOT NULL)",
                    "drop index if exists idx_ActivityLogEntries"
                });

                TryMigrate(connection);
            }
        }

        private void TryMigrate(ManagedConnection connection)
        {
            try
            {
                if (TableExists(connection, "ActivityLogEntries"))
                {
                    connection.RunQueries(new[]
                    {
                        "INSERT INTO ActivityLog (Name, Overview, ShortOverview, Type, ItemId, UserId, DateCreated, LogSeverity) SELECT Name, Overview, ShortOverview, Type, ItemId, UserId, DateCreated, LogSeverity FROM ActivityLogEntries",
                        "drop table if exists ActivityLogEntries"
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error migrating activity log database");
            }
        }

        private const string BaseActivitySelectText = "select Id, Name, Overview, ShortOverview, Type, ItemId, UserId, DateCreated, LogSeverity from ActivityLog";

        public void Create(ActivityLogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    using (var statement = db.PrepareStatement("insert into ActivityLog (Name, Overview, ShortOverview, Type, ItemId, UserId, DateCreated, LogSeverity) values (@Name, @Overview, @ShortOverview, @Type, @ItemId, @UserId, @DateCreated, @LogSeverity)"))
                    {
                        statement.TryBind("@Name", entry.Name);

                        statement.TryBind("@Overview", entry.Overview);
                        statement.TryBind("@ShortOverview", entry.ShortOverview);
                        statement.TryBind("@Type", entry.Type);
                        statement.TryBind("@ItemId", entry.ItemId);

                        if (entry.UserId.Equals(Guid.Empty))
                        {
                            statement.TryBindNull("@UserId");
                        }
                        else
                        {
                            statement.TryBind("@UserId", entry.UserId.ToString("N", CultureInfo.InvariantCulture));
                        }

                        statement.TryBind("@DateCreated", entry.Date.ToDateTimeParamValue());
                        statement.TryBind("@LogSeverity", entry.Severity.ToString());

                        statement.MoveNext();
                    }
                }, TransactionMode);
            }
        }

        public void Update(ActivityLogEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    using (var statement = db.PrepareStatement("Update ActivityLog set Name=@Name,Overview=@Overview,ShortOverview=@ShortOverview,Type=@Type,ItemId=@ItemId,UserId=@UserId,DateCreated=@DateCreated,LogSeverity=@LogSeverity where Id=@Id"))
                    {
                        statement.TryBind("@Id", entry.Id);

                        statement.TryBind("@Name", entry.Name);
                        statement.TryBind("@Overview", entry.Overview);
                        statement.TryBind("@ShortOverview", entry.ShortOverview);
                        statement.TryBind("@Type", entry.Type);
                        statement.TryBind("@ItemId", entry.ItemId);

                        if (entry.UserId.Equals(Guid.Empty))
                        {
                            statement.TryBindNull("@UserId");
                        }
                        else
                        {
                            statement.TryBind("@UserId", entry.UserId.ToString("N", CultureInfo.InvariantCulture));
                        }

                        statement.TryBind("@DateCreated", entry.Date.ToDateTimeParamValue());
                        statement.TryBind("@LogSeverity", entry.Severity.ToString());

                        statement.MoveNext();
                    }
                }, TransactionMode);
            }
        }

        public QueryResult<ActivityLogEntry> GetActivityLogEntries(DateTime? minDate, bool? hasUserId, int? startIndex, int? limit)
        {
            var commandText = BaseActivitySelectText;
            var whereClauses = new List<string>();

            if (minDate.HasValue)
            {
                whereClauses.Add("DateCreated>=@DateCreated");
            }
            if (hasUserId.HasValue)
            {
                if (hasUserId.Value)
                {
                    whereClauses.Add("UserId not null");
                }
                else
                {
                    whereClauses.Add("UserId is null");
                }
            }

            var whereTextWithoutPaging = whereClauses.Count == 0 ?
              string.Empty :
              " where " + string.Join(" AND ", whereClauses.ToArray());

            if (startIndex.HasValue && startIndex.Value > 0)
            {
                var pagingWhereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                whereClauses.Add(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Id NOT IN (SELECT Id FROM ActivityLog {0} ORDER BY DateCreated DESC LIMIT {1})",
                        pagingWhereText,
                        startIndex.Value));
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

            var statementTexts = new[]
            {
                commandText,
                "select count (Id) from ActivityLog" + whereTextWithoutPaging
            };

            var list = new List<ActivityLogEntry>();
            var result = new QueryResult<ActivityLogEntry>();

            using (var connection = GetConnection(true))
            {
                connection.RunInTransaction(
                    db =>
                    {
                        var statements = PrepareAll(db, statementTexts).ToList();

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

                            result.TotalRecordCount = statement.ExecuteQuery().SelectScalarInt().First();
                        }
                    },
                    ReadTransactionMode);
            }

            result.Items = list;
            return result;
        }

        private static ActivityLogEntry GetEntry(IReadOnlyList<IResultSetValue> reader)
        {
            var index = 0;

            var info = new ActivityLogEntry
            {
                Id = reader[index].ToInt64()
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
                info.UserId = new Guid(reader[index].ToString());
            }

            index++;
            info.Date = reader[index].ReadDateTime();

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                info.Severity = (LogLevel)Enum.Parse(typeof(LogLevel), reader[index].ToString(), true);
            }

            return info;
        }
    }
}
