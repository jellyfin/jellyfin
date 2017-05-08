using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.FileOrganization;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    public class SqliteFileOrganizationRepository : BaseSqliteRepository, IFileOrganizationRepository, IDisposable
    {
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public SqliteFileOrganizationRepository(ILogger logger, IServerApplicationPaths appPaths) : base(logger)
        {
            DbFilePath = Path.Combine(appPaths.DataPath, "fileorganization.db");
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public void Initialize()
        {
            using (var connection = CreateConnection())
            {
                RunDefaultInitialization(connection);

                string[] queries = {

                                "create table if not exists FileOrganizerResults (ResultId GUID PRIMARY KEY, OriginalPath TEXT, TargetPath TEXT, FileLength INT, OrganizationDate datetime, Status TEXT, OrganizationType TEXT, StatusMessage TEXT, ExtractedName TEXT, ExtractedYear int null, ExtractedSeasonNumber int null, ExtractedEpisodeNumber int null, ExtractedEndingEpisodeNumber, DuplicatePaths TEXT int null)",
                                "create index if not exists idx_FileOrganizerResults on FileOrganizerResults(ResultId)"
                               };

                connection.RunQueries(queries);
            }
        }

        public async Task SaveResult(FileOrganizationResult result, CancellationToken cancellationToken)
        {
            if (result == null)
            {
                throw new ArgumentNullException("result");
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        var commandText = "replace into FileOrganizerResults (ResultId, OriginalPath, TargetPath, FileLength, OrganizationDate, Status, OrganizationType, StatusMessage, ExtractedName, ExtractedYear, ExtractedSeasonNumber, ExtractedEpisodeNumber, ExtractedEndingEpisodeNumber, DuplicatePaths) values (@ResultId, @OriginalPath, @TargetPath, @FileLength, @OrganizationDate, @Status, @OrganizationType, @StatusMessage, @ExtractedName, @ExtractedYear, @ExtractedSeasonNumber, @ExtractedEpisodeNumber, @ExtractedEndingEpisodeNumber, @DuplicatePaths)";

                        using (var statement = db.PrepareStatement(commandText))
                        {
                            statement.TryBind("@ResultId", result.Id.ToGuidBlob());
                            statement.TryBind("@OriginalPath", result.OriginalPath);

                            statement.TryBind("@TargetPath", result.TargetPath);
                            statement.TryBind("@FileLength", result.FileSize);
                            statement.TryBind("@OrganizationDate", result.Date.ToDateTimeParamValue());
                            statement.TryBind("@Status", result.Status.ToString());
                            statement.TryBind("@OrganizationType", result.Type.ToString());
                            statement.TryBind("@StatusMessage", result.StatusMessage);
                            statement.TryBind("@ExtractedName", result.ExtractedName);
                            statement.TryBind("@ExtractedYear", result.ExtractedYear);
                            statement.TryBind("@ExtractedSeasonNumber", result.ExtractedSeasonNumber);
                            statement.TryBind("@ExtractedEpisodeNumber", result.ExtractedEpisodeNumber);
                            statement.TryBind("@ExtractedEndingEpisodeNumber", result.ExtractedEndingEpisodeNumber);
                            statement.TryBind("@DuplicatePaths", string.Join("|", result.DuplicatePaths.ToArray()));

                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
        }

        public async Task Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        using (var statement = db.PrepareStatement("delete from FileOrganizerResults where ResultId = @ResultId"))
                        {
                            statement.TryBind("@ResultId", id.ToGuidBlob());
                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
        }

        public async Task DeleteAll()
        {
            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        var commandText = "delete from FileOrganizerResults";

                        db.Execute(commandText);
                    }, TransactionMode);
                }
            }
        }

        public QueryResult<FileOrganizationResult> GetResults(FileOrganizationResultQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var commandText = "SELECT ResultId, OriginalPath, TargetPath, FileLength, OrganizationDate, Status, OrganizationType, StatusMessage, ExtractedName, ExtractedYear, ExtractedSeasonNumber, ExtractedEpisodeNumber, ExtractedEndingEpisodeNumber, DuplicatePaths from FileOrganizerResults";

                    if (query.StartIndex.HasValue && query.StartIndex.Value > 0)
                    {
                        commandText += string.Format(" WHERE ResultId NOT IN (SELECT ResultId FROM FileOrganizerResults ORDER BY OrganizationDate desc LIMIT {0})",
                            query.StartIndex.Value.ToString(_usCulture));
                    }

                    commandText += " ORDER BY OrganizationDate desc";

                    if (query.Limit.HasValue)
                    {
                        commandText += " LIMIT " + query.Limit.Value.ToString(_usCulture);
                    }

                    var list = new List<FileOrganizationResult>();

                    using (var statement = connection.PrepareStatement(commandText))
                    {
                        foreach (var row in statement.ExecuteQuery())
                        {
                            list.Add(GetResult(row));
                        }
                    }

                    int count;
                    using (var statement = connection.PrepareStatement("select count (ResultId) from FileOrganizerResults"))
                    {
                        count = statement.ExecuteQuery().SelectScalarInt().First();
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

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement("select ResultId, OriginalPath, TargetPath, FileLength, OrganizationDate, Status, OrganizationType, StatusMessage, ExtractedName, ExtractedYear, ExtractedSeasonNumber, ExtractedEpisodeNumber, ExtractedEndingEpisodeNumber, DuplicatePaths from FileOrganizerResults where ResultId=@ResultId"))
                    {
                        statement.TryBind("@ResultId", id.ToGuidBlob());

                        foreach (var row in statement.ExecuteQuery())
                        {
                            return GetResult(row);
                        }
                    }

                    return null;
                }
            }
        }

        public FileOrganizationResult GetResult(IReadOnlyList<IResultSetValue> reader)
        {
            var index = 0;

            var result = new FileOrganizationResult
            {
                Id = reader[0].ReadGuidFromBlob().ToString("N")
            };

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                result.OriginalPath = reader[index].ToString();
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                result.TargetPath = reader[index].ToString();
            }

            index++;
            result.FileSize = reader[index].ToInt64();

            index++;
            result.Date = reader[index].ReadDateTime();

            index++;
            result.Status = (FileSortingStatus)Enum.Parse(typeof(FileSortingStatus), reader[index].ToString(), true);

            index++;
            result.Type = (FileOrganizerType)Enum.Parse(typeof(FileOrganizerType), reader[index].ToString(), true);

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                result.StatusMessage = reader[index].ToString();
            }

            result.OriginalFileName = Path.GetFileName(result.OriginalPath);

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                result.ExtractedName = reader[index].ToString();
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                result.ExtractedYear = reader[index].ToInt();
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                result.ExtractedSeasonNumber = reader[index].ToInt();
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                result.ExtractedEpisodeNumber = reader[index].ToInt();
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                result.ExtractedEndingEpisodeNumber = reader[index].ToInt();
            }

            index++;
            if (reader[index].SQLiteType != SQLiteType.Null)
            {
                result.DuplicatePaths = reader[index].ToString().Split('|').Where(i => !string.IsNullOrEmpty(i)).ToList();
            }

            return result;
        }
    }
}
