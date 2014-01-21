using MediaBrowser.Controller;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class SqliteFileOrganizationRepository : IFileOrganizationRepository
    {
        private IDbConnection _connection;

        private readonly ILogger _logger;

        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private SqliteShrinkMemoryTimer _shrinkMemoryTimer;
        private readonly IServerApplicationPaths _appPaths;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private IDbCommand _saveResultCommand;
        private IDbCommand _deleteResultCommand;

        public SqliteFileOrganizationRepository(ILogManager logManager, IServerApplicationPaths appPaths)
        {
            _appPaths = appPaths;

            _logger = logManager.GetLogger(GetType().Name);
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "fileorganization.db");

            _connection = await SqliteExtensions.ConnectToDb(dbFile, _logger).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists organizationresults (ResultId GUID PRIMARY KEY, OriginalPath TEXT, TargetPath TEXT, OrganizationDate datetime, Status TEXT, OrganizationType TEXT, StatusMessage TEXT, ExtractedName TEXT, ExtractedYear int null)",
                                "create index if not exists idx_organizationresults on organizationresults(ResultId)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, _logger);

            PrepareStatements();

            _shrinkMemoryTimer = new SqliteShrinkMemoryTimer(_connection, _writeLock, _logger);
        }

        private void PrepareStatements()
        {
            _saveResultCommand = _connection.CreateCommand();
            _saveResultCommand.CommandText = "replace into organizationresults (ResultId, OriginalPath, TargetPath, OrganizationDate, Status, OrganizationType, StatusMessage, ExtractedName, ExtractedYear) values (@ResultId, @OriginalPath, @TargetPath, @OrganizationDate, @Status, @OrganizationType, @StatusMessage, @ExtractedName, @ExtractedYear)";

            _saveResultCommand.Parameters.Add(_saveResultCommand, "@ResultId");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@OriginalPath");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@TargetPath");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@OrganizationDate");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@Status");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@OrganizationType");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@StatusMessage");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@ExtractedName");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@ExtractedYear");

            _deleteResultCommand = _connection.CreateCommand();
            _deleteResultCommand.CommandText = "delete from organizationresults where ResultId = @ResultId";

            _deleteResultCommand.Parameters.Add(_saveResultCommand, "@ResultId");
        }

        public async Task SaveResult(FileOrganizationResult result, CancellationToken cancellationToken)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            cancellationToken.ThrowIfCancellationRequested();

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                _saveResultCommand.GetParameter(0).Value = new Guid(result.Id);
                _saveResultCommand.GetParameter(1).Value = result.OriginalPath;
                _saveResultCommand.GetParameter(2).Value = result.TargetPath;
                _saveResultCommand.GetParameter(3).Value = result.Date;
                _saveResultCommand.GetParameter(4).Value = result.Status.ToString();
                _saveResultCommand.GetParameter(5).Value = result.Type.ToString();
                _saveResultCommand.GetParameter(6).Value = result.StatusMessage;
                _saveResultCommand.GetParameter(7).Value = result.ExtractedName;
                _saveResultCommand.GetParameter(8).Value = result.ExtractedYear;

                _saveResultCommand.Transaction = transaction;

                _saveResultCommand.ExecuteNonQuery();

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
                _logger.ErrorException("Failed to save FileOrganizationResult:", e);

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

        public async Task Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            await _writeLock.WaitAsync().ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                _deleteResultCommand.GetParameter(0).Value = new Guid(id);

                _deleteResultCommand.Transaction = transaction;

                _deleteResultCommand.ExecuteNonQuery();

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
                _logger.ErrorException("Failed to save FileOrganizationResult:", e);

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

        public QueryResult<FileOrganizationResult> GetResults(FileOrganizationResultQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT ResultId, OriginalPath, TargetPath, OrganizationDate, Status, OrganizationType, StatusMessage, ExtractedName, ExtractedYear from organizationresults";

                if (query.StartIndex.HasValue && query.StartIndex.Value > 0)
                {
                    cmd.CommandText += string.Format(" WHERE ResultId NOT IN (SELECT ResultId FROM organizationresults ORDER BY OrganizationDate desc LIMIT {0})",
                        query.StartIndex.Value.ToString(_usCulture));
                }

                cmd.CommandText += " ORDER BY OrganizationDate desc";

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(_usCulture);
                }

                cmd.CommandText += "; select count (ResultId) from organizationresults";

                var list = new List<FileOrganizationResult>();
                var count = 0;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    while (reader.Read())
                    {
                        list.Add(GetResult(reader));
                    }

                    if (reader.NextResult() && reader.Read())
                    {
                        count = reader.GetInt32(0);
                    }
                }

                return new QueryResult<FileOrganizationResult>()
                {
                    Items = list.ToArray(),
                    TotalRecordCount = count
                };
            }
        }

        public FileOrganizationResult GetResult(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var guid = new Guid(id);

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select ResultId, OriginalPath, TargetPath, OrganizationDate, Status, OrganizationType, StatusMessage, ExtractedName, ExtractedYear from organizationresults where ResultId=@Id";

                cmd.Parameters.Add(cmd, "@Id", DbType.Guid).Value = guid;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        return GetResult(reader);
                    }
                }
            }

            return null;
        }

        public FileOrganizationResult GetResult(IDataReader reader)
        {
            var result = new FileOrganizationResult
            {
                Id = reader.GetGuid(0).ToString("N")
            };

            if (!reader.IsDBNull(1))
            {
                result.OriginalPath = reader.GetString(1);
            }

            if (!reader.IsDBNull(2))
            {
                result.TargetPath = reader.GetString(2);
            }

            result.Date = reader.GetDateTime(3).ToUniversalTime();
            result.Status = (FileSortingStatus)Enum.Parse(typeof(FileSortingStatus), reader.GetString(4), true);
            result.Type = (FileOrganizerType)Enum.Parse(typeof(FileOrganizerType), reader.GetString(5), true);

            if (!reader.IsDBNull(6))
            {
                result.StatusMessage = reader.GetString(6);
            }

            result.OriginalFileName = Path.GetFileName(result.OriginalPath);

            if (!reader.IsDBNull(7))
            {
                result.ExtractedName = reader.GetString(7);
            }

            if (!reader.IsDBNull(8))
            {
                result.ExtractedYear = reader.GetInt32(8);
            }

            return result;
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
                        if (_shrinkMemoryTimer != null)
                        {
                            _shrinkMemoryTimer.Dispose();
                            _shrinkMemoryTimer = null;
                        }

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
