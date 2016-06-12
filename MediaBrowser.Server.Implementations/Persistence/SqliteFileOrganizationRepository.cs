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
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public SqliteFileOrganizationRepository(ILogManager logManager, IServerApplicationPaths appPaths, IDbConnector connector) : base(logManager, connector)
        {
            DbFilePath = Path.Combine(appPaths.DataPath, "fileorganization.db");
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                string[] queries = {

                                "create table if not exists FileOrganizerResults (ResultId GUID PRIMARY KEY, OriginalPath TEXT, TargetPath TEXT, FileLength INT, OrganizationDate datetime, Status TEXT, OrganizationType TEXT, StatusMessage TEXT, ExtractedName TEXT, ExtractedYear int null, ExtractedSeasonNumber int null, ExtractedEpisodeNumber int null, ExtractedEndingEpisodeNumber, DuplicatePaths TEXT int null)",
                                "create index if not exists idx_FileOrganizerResults on FileOrganizerResults(ResultId)"
                               };

                connection.RunQueries(queries, Logger);
            }
        }

        public async Task SaveResult(FileOrganizationResult result, CancellationToken cancellationToken)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                using (var saveResultCommand = connection.CreateCommand())
                {
                    saveResultCommand.CommandText = "replace into FileOrganizerResults (ResultId, OriginalPath, TargetPath, FileLength, OrganizationDate, Status, OrganizationType, StatusMessage, ExtractedName, ExtractedYear, ExtractedSeasonNumber, ExtractedEpisodeNumber, ExtractedEndingEpisodeNumber, DuplicatePaths) values (@ResultId, @OriginalPath, @TargetPath, @FileLength, @OrganizationDate, @Status, @OrganizationType, @StatusMessage, @ExtractedName, @ExtractedYear, @ExtractedSeasonNumber, @ExtractedEpisodeNumber, @ExtractedEndingEpisodeNumber, @DuplicatePaths)";

                    saveResultCommand.Parameters.Add(saveResultCommand, "@ResultId");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@OriginalPath");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@TargetPath");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@FileLength");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@OrganizationDate");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@Status");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@OrganizationType");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@StatusMessage");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@ExtractedName");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@ExtractedYear");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@ExtractedSeasonNumber");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@ExtractedEpisodeNumber");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@ExtractedEndingEpisodeNumber");
                    saveResultCommand.Parameters.Add(saveResultCommand, "@DuplicatePaths");

                    IDbTransaction transaction = null;

                    try
                    {
                        transaction = connection.BeginTransaction();

                        var index = 0;

                        saveResultCommand.GetParameter(index++).Value = new Guid(result.Id);
                        saveResultCommand.GetParameter(index++).Value = result.OriginalPath;
                        saveResultCommand.GetParameter(index++).Value = result.TargetPath;
                        saveResultCommand.GetParameter(index++).Value = result.FileSize;
                        saveResultCommand.GetParameter(index++).Value = result.Date;
                        saveResultCommand.GetParameter(index++).Value = result.Status.ToString();
                        saveResultCommand.GetParameter(index++).Value = result.Type.ToString();
                        saveResultCommand.GetParameter(index++).Value = result.StatusMessage;
                        saveResultCommand.GetParameter(index++).Value = result.ExtractedName;
                        saveResultCommand.GetParameter(index++).Value = result.ExtractedYear;
                        saveResultCommand.GetParameter(index++).Value = result.ExtractedSeasonNumber;
                        saveResultCommand.GetParameter(index++).Value = result.ExtractedEpisodeNumber;
                        saveResultCommand.GetParameter(index++).Value = result.ExtractedEndingEpisodeNumber;
                        saveResultCommand.GetParameter(index).Value = string.Join("|", result.DuplicatePaths.ToArray());

                        saveResultCommand.Transaction = transaction;

                        saveResultCommand.ExecuteNonQuery();

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
                    }
                }
            }
        }

        public async Task Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                using (var deleteResultCommand = connection.CreateCommand())
                {
                    deleteResultCommand.CommandText = "delete from FileOrganizerResults where ResultId = @ResultId";

                    deleteResultCommand.Parameters.Add(deleteResultCommand, "@ResultId");

                    IDbTransaction transaction = null;

                    try
                    {
                        transaction = connection.BeginTransaction();

                        deleteResultCommand.GetParameter(0).Value = new Guid(id);

                        deleteResultCommand.Transaction = transaction;

                        deleteResultCommand.ExecuteNonQuery();

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
                    }
                }
            }
        }

        public async Task DeleteAll()
        {
            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "delete from FileOrganizerResults";

                    IDbTransaction transaction = null;

                    try
                    {
                        transaction = connection.BeginTransaction();

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
                    }
                }
            }
        }

        public QueryResult<FileOrganizationResult> GetResults(FileOrganizationResultQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
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
        }

        public FileOrganizationResult GetResult(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            using (var connection = CreateConnection(true).Result)
            {
                var guid = new Guid(id);

                using (var cmd = connection.CreateCommand())
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
    }
}
