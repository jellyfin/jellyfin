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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class SqliteFileOrganizationRepository : BaseSqliteRepository, IFileOrganizationRepository, IDisposable
    {
        private IDbConnection _connection;

        private readonly IServerApplicationPaths _appPaths;

        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private IDbCommand _saveResultCommand;
        private IDbCommand _deleteResultCommand;
        private IDbCommand _deleteAllCommand;

        public SqliteFileOrganizationRepository(ILogManager logManager, IServerApplicationPaths appPaths) : base(logManager)
        {
            _appPaths = appPaths;
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize(IDbConnector dbConnector)
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "fileorganization.db");

            _connection = await dbConnector.Connect(dbFile).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists FileOrganizerResults (ResultId GUID PRIMARY KEY, OriginalPath TEXT, TargetPath TEXT, FileLength INT, OrganizationDate datetime, Status TEXT, OrganizationType TEXT, StatusMessage TEXT, ExtractedName TEXT, ExtractedYear int null, ExtractedSeasonNumber int null, ExtractedEpisodeNumber int null, ExtractedEndingEpisodeNumber, DuplicatePaths TEXT int null)",
                                "create index if not exists idx_FileOrganizerResults on FileOrganizerResults(ResultId)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, Logger);

            PrepareStatements();
        }

        private void PrepareStatements()
        {
            _saveResultCommand = _connection.CreateCommand();
            _saveResultCommand.CommandText = "replace into FileOrganizerResults (ResultId, OriginalPath, TargetPath, FileLength, OrganizationDate, Status, OrganizationType, StatusMessage, ExtractedName, ExtractedYear, ExtractedSeasonNumber, ExtractedEpisodeNumber, ExtractedEndingEpisodeNumber, DuplicatePaths) values (@ResultId, @OriginalPath, @TargetPath, @FileLength, @OrganizationDate, @Status, @OrganizationType, @StatusMessage, @ExtractedName, @ExtractedYear, @ExtractedSeasonNumber, @ExtractedEpisodeNumber, @ExtractedEndingEpisodeNumber, @DuplicatePaths)";

            _saveResultCommand.Parameters.Add(_saveResultCommand, "@ResultId");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@OriginalPath");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@TargetPath");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@FileLength");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@OrganizationDate");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@Status");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@OrganizationType");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@StatusMessage");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@ExtractedName");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@ExtractedYear");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@ExtractedSeasonNumber");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@ExtractedEpisodeNumber");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@ExtractedEndingEpisodeNumber");
            _saveResultCommand.Parameters.Add(_saveResultCommand, "@DuplicatePaths");

            _deleteResultCommand = _connection.CreateCommand();
            _deleteResultCommand.CommandText = "delete from FileOrganizerResults where ResultId = @ResultId";

            _deleteResultCommand.Parameters.Add(_saveResultCommand, "@ResultId");

            _deleteAllCommand = _connection.CreateCommand();
            _deleteAllCommand.CommandText = "delete from FileOrganizerResults";
        }

        public async Task SaveResult(FileOrganizationResult result, CancellationToken cancellationToken)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            cancellationToken.ThrowIfCancellationRequested();

            await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                var index = 0;

                _saveResultCommand.GetParameter(index++).Value = new Guid(result.Id);
                _saveResultCommand.GetParameter(index++).Value = result.OriginalPath;
                _saveResultCommand.GetParameter(index++).Value = result.TargetPath;
                _saveResultCommand.GetParameter(index++).Value = result.FileSize;
                _saveResultCommand.GetParameter(index++).Value = result.Date;
                _saveResultCommand.GetParameter(index++).Value = result.Status.ToString();
                _saveResultCommand.GetParameter(index++).Value = result.Type.ToString();
                _saveResultCommand.GetParameter(index++).Value = result.StatusMessage;
                _saveResultCommand.GetParameter(index++).Value = result.ExtractedName;
                _saveResultCommand.GetParameter(index++).Value = result.ExtractedYear;
                _saveResultCommand.GetParameter(index++).Value = result.ExtractedSeasonNumber;
                _saveResultCommand.GetParameter(index++).Value = result.ExtractedEpisodeNumber;
                _saveResultCommand.GetParameter(index++).Value = result.ExtractedEndingEpisodeNumber;
                _saveResultCommand.GetParameter(index).Value = string.Join("|", result.DuplicatePaths.ToArray());

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
                Logger.ErrorException("Failed to save FileOrganizationResult:", e);

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

        public async Task Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            await WriteLock.WaitAsync().ConfigureAwait(false);

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
                Logger.ErrorException("Failed to delete FileOrganizationResult:", e);

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

        public async Task DeleteAll()
        {
            await WriteLock.WaitAsync().ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();
                
                _deleteAllCommand.Transaction = transaction;

                _deleteAllCommand.ExecuteNonQuery();

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
                Logger.ErrorException("Failed to delete results", e);

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
        
        public QueryResult<FileOrganizationResult> GetResults(FileOrganizationResultQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "SELECT ResultId, OriginalPath, TargetPath, FileLength, OrganizationDate, Status, OrganizationType, StatusMessage, ExtractedName, ExtractedYear, ExtractedSeasonNumber, ExtractedEpisodeNumber, ExtractedEndingEpisodeNumber, DuplicatePaths from FileOrganizerResults";

                if (query.StartIndex.HasValue && query.StartIndex.Value > 0)
                {
                    cmd.CommandText += string.Format(" WHERE ResultId NOT IN (SELECT ResultId FROM FileOrganizerResults ORDER BY OrganizationDate desc LIMIT {0})",
                        query.StartIndex.Value.ToString(_usCulture));
                }

                cmd.CommandText += " ORDER BY OrganizationDate desc";

                if (query.Limit.HasValue)
                {
                    cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(_usCulture);
                }

                cmd.CommandText += "; select count (ResultId) from FileOrganizerResults";

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
                cmd.CommandText = "select ResultId, OriginalPath, TargetPath, FileLength, OrganizationDate, Status, OrganizationType, StatusMessage, ExtractedName, ExtractedYear, ExtractedSeasonNumber, ExtractedEpisodeNumber, ExtractedEndingEpisodeNumber, DuplicatePaths from FileOrganizerResults where ResultId=@Id";

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
            var index = 0;

            var result = new FileOrganizationResult
            {
                Id = reader.GetGuid(0).ToString("N")
            };

            index++;
            if (!reader.IsDBNull(index))
            {
                result.OriginalPath = reader.GetString(index);
            }

            index++;
            if (!reader.IsDBNull(index))
            {
                result.TargetPath = reader.GetString(index);
            }

            index++;
            result.FileSize = reader.GetInt64(index);

            index++;
            result.Date = reader.GetDateTime(index).ToUniversalTime();

            index++;
            result.Status = (FileSortingStatus)Enum.Parse(typeof(FileSortingStatus), reader.GetString(index), true);

            index++;
            result.Type = (FileOrganizerType)Enum.Parse(typeof(FileOrganizerType), reader.GetString(index), true);

            index++;
            if (!reader.IsDBNull(index))
            {
                result.StatusMessage = reader.GetString(index);
            }

            result.OriginalFileName = Path.GetFileName(result.OriginalPath);

            index++;
            if (!reader.IsDBNull(index))
            {
                result.ExtractedName = reader.GetString(index);
            }

            index++;
            if (!reader.IsDBNull(index))
            {
                result.ExtractedYear = reader.GetInt32(index);
            }

            index++;
            if (!reader.IsDBNull(index))
            {
                result.ExtractedSeasonNumber = reader.GetInt32(index);
            }

            index++;
            if (!reader.IsDBNull(index))
            {
                result.ExtractedEpisodeNumber = reader.GetInt32(index);
            }

            index++;
            if (!reader.IsDBNull(index))
            {
                result.ExtractedEndingEpisodeNumber = reader.GetInt32(index);
            }

            index++;
            if (!reader.IsDBNull(index))
            {
                result.DuplicatePaths = reader.GetString(index).Split('|').Where(i => !string.IsNullOrEmpty(i)).ToList();
            }

            return result;
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
