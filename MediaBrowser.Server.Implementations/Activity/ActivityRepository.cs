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
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Activity
{
    public class ActivityRepository : IActivityRepository, IDisposable
    {
        private IDbConnection _connection;
        private readonly ILogger _logger;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly IServerApplicationPaths _appPaths;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private IDbCommand _saveActivityCommand;

        public ActivityRepository(ILogger logger, IServerApplicationPaths appPaths)
        {
            _logger = logger;
            _appPaths = appPaths;
        }

        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "activitylog.db");

            _connection = await SqliteExtensions.ConnectToDb(dbFile, _logger).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists ActivityLogEntries (Id GUID PRIMARY KEY, Name TEXT, Overview TEXT, ShortOverview TEXT, Type TEXT, ItemId TEXT, UserId TEXT, DateCreated DATETIME, LogSeverity TEXT)",
                                "create index if not exists idx_ActivityLogEntries on ActivityLogEntries(Id)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, _logger);

            PrepareStatements();
        }

        private void PrepareStatements()
        {
            _saveActivityCommand = _connection.CreateCommand();
            _saveActivityCommand.CommandText = "replace into ActivityLogEntries (Id, Name, Overview, ShortOverview, Type, ItemId, UserId, DateCreated, LogSeverity) values (@Id, @Name, @Overview, @ShortOverview, @Type, @ItemId, @UserId, @DateCreated, @LogSeverity)";

            _saveActivityCommand.Parameters.Add(_saveActivityCommand, "@Id");
            _saveActivityCommand.Parameters.Add(_saveActivityCommand, "@Name");
            _saveActivityCommand.Parameters.Add(_saveActivityCommand, "@Overview");
            _saveActivityCommand.Parameters.Add(_saveActivityCommand, "@ShortOverview");
            _saveActivityCommand.Parameters.Add(_saveActivityCommand, "@Type");
            _saveActivityCommand.Parameters.Add(_saveActivityCommand, "@ItemId");
            _saveActivityCommand.Parameters.Add(_saveActivityCommand, "@UserId");
            _saveActivityCommand.Parameters.Add(_saveActivityCommand, "@DateCreated");
            _saveActivityCommand.Parameters.Add(_saveActivityCommand, "@LogSeverity");
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

            await _writeLock.WaitAsync().ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                var index = 0;

                _saveActivityCommand.GetParameter(index++).Value = new Guid(entry.Id);
                _saveActivityCommand.GetParameter(index++).Value = entry.Name;
                _saveActivityCommand.GetParameter(index++).Value = entry.Overview;
                _saveActivityCommand.GetParameter(index++).Value = entry.ShortOverview;
                _saveActivityCommand.GetParameter(index++).Value = entry.Type;
                _saveActivityCommand.GetParameter(index++).Value = entry.ItemId;
                _saveActivityCommand.GetParameter(index++).Value = entry.UserId;
                _saveActivityCommand.GetParameter(index++).Value = entry.Date;
                _saveActivityCommand.GetParameter(index++).Value = entry.Severity.ToString();

                _saveActivityCommand.Transaction = transaction;

                _saveActivityCommand.ExecuteNonQuery();

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

        public QueryResult<ActivityLogEntry> GetActivityLogEntries(int? startIndex, int? limit)
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = BaseActivitySelectText;

                var whereClauses = new List<string>();

                if (startIndex.HasValue && startIndex.Value > 0)
                {
                    whereClauses.Add(string.Format("Id NOT IN (SELECT Id FROM ActivityLogEntries ORDER BY DateCreated DESC LIMIT {0})",
                        startIndex.Value.ToString(_usCulture)));
                }

                if (whereClauses.Count > 0)
                {
                    cmd.CommandText += " where " + string.Join(" AND ", whereClauses.ToArray());
                }

                cmd.CommandText += " ORDER BY DateCreated DESC";

                if (limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + limit.Value.ToString(_usCulture);
                }

                cmd.CommandText += "; select count (Id) from ActivityLogEntries";

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
