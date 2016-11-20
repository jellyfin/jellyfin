using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Querying;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Security
{
    public class AuthenticationRepository : BaseSqliteRepository, IAuthenticationRepository
    {
        private readonly IServerApplicationPaths _appPaths;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public AuthenticationRepository(ILogger logger, IServerApplicationPaths appPaths)
            : base(logger)
        {
            _appPaths = appPaths;
            DbFilePath = Path.Combine(appPaths.DataPath, "authentication.db");
        }

        public void Initialize()
        {
            using (var connection = CreateConnection())
            {
                string[] queries = {

                                "create table if not exists AccessTokens (Id GUID PRIMARY KEY, AccessToken TEXT NOT NULL, DeviceId TEXT, AppName TEXT, AppVersion TEXT, DeviceName TEXT, UserId TEXT, IsActive BIT, DateCreated DATETIME NOT NULL, DateRevoked DATETIME)",
                                "create index if not exists idx_AccessTokens on AccessTokens(Id)"
                               };

                connection.RunQueries(queries);

                connection.RunInTransaction(db =>
                {
                    var existingColumnNames = GetColumnNames(db, "AccessTokens");

                    AddColumn(db, "AccessTokens", "AppVersion", "TEXT", existingColumnNames);
                });
            }
        }

        public Task Create(AuthenticationInfo info, CancellationToken cancellationToken)
        {
            info.Id = Guid.NewGuid().ToString("N");

            return Update(info, cancellationToken);
        }

        public async Task Update(AuthenticationInfo info, CancellationToken cancellationToken)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        using (var statement = db.PrepareStatement("replace into AccessTokens (Id, AccessToken, DeviceId, AppName, AppVersion, DeviceName, UserId, IsActive, DateCreated, DateRevoked) values (@Id, @AccessToken, @DeviceId, @AppName, @AppVersion, @DeviceName, @UserId, @IsActive, @DateCreated, @DateRevoked)"))
                        {
                            statement.TryBind("@Id", info.Id.ToGuidParamValue());
                            statement.TryBind("@AccessToken", info.AccessToken);

                            statement.TryBind("@DeviceId", info.DeviceId);
                            statement.TryBind("@AppName", info.AppName);
                            statement.TryBind("@AppVersion", info.AppVersion);
                            statement.TryBind("@DeviceName", info.DeviceName);
                            statement.TryBind("@UserId", info.UserId);
                            statement.TryBind("@IsActive", info.IsActive);
                            statement.TryBind("@DateCreated", info.DateCreated.ToDateTimeParamValue());

                            if (info.DateRevoked.HasValue)
                            {
                                statement.TryBind("@DateRevoked", info.DateRevoked.Value.ToDateTimeParamValue());
                            }
                            else
                            {
                                statement.TryBindNull("@DateRevoked");
                            }

                            statement.MoveNext();
                        }
                    });
                }
            }
        }

        private const string BaseSelectText = "select Id, AccessToken, DeviceId, AppName, AppVersion, DeviceName, UserId, IsActive, DateCreated, DateRevoked from AccessTokens";

        private void BindAuthenticationQueryParams(AuthenticationInfoQuery query, IStatement statement)
        {
            if (!string.IsNullOrWhiteSpace(query.AccessToken))
            {
                statement.TryBind("@AccessToken", query.AccessToken);
            }

            if (!string.IsNullOrWhiteSpace(query.UserId))
            {
                statement.TryBind("@UserId", query.UserId);
            }

            if (!string.IsNullOrWhiteSpace(query.DeviceId))
            {
                statement.TryBind("@DeviceId", query.DeviceId);
            }

            if (query.IsActive.HasValue)
            {
                statement.TryBind("@IsActive", query.IsActive.Value);
            }
        }

        public QueryResult<AuthenticationInfo> Get(AuthenticationInfoQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            using (var connection = CreateConnection(true))
            {
                var commandText = BaseSelectText;

                var whereClauses = new List<string>();

                var startIndex = query.StartIndex ?? 0;

                if (!string.IsNullOrWhiteSpace(query.AccessToken))
                {
                    whereClauses.Add("AccessToken=@AccessToken");
                }

                if (!string.IsNullOrWhiteSpace(query.UserId))
                {
                    whereClauses.Add("UserId=@UserId");
                }

                if (!string.IsNullOrWhiteSpace(query.DeviceId))
                {
                    whereClauses.Add("DeviceId=@DeviceId");
                }

                if (query.IsActive.HasValue)
                {
                    whereClauses.Add("IsActive=@IsActive");
                }

                if (query.HasUser.HasValue)
                {
                    if (query.HasUser.Value)
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

                if (startIndex > 0)
                {
                    var pagingWhereText = whereClauses.Count == 0 ?
                        string.Empty :
                        " where " + string.Join(" AND ", whereClauses.ToArray());

                    whereClauses.Add(string.Format("Id NOT IN (SELECT Id FROM AccessTokens {0} ORDER BY DateCreated LIMIT {1})",
                        pagingWhereText,
                        startIndex.ToString(_usCulture)));
                }

                var whereText = whereClauses.Count == 0 ?
                    string.Empty :
                    " where " + string.Join(" AND ", whereClauses.ToArray());

                commandText += whereText;

                commandText += " ORDER BY DateCreated";

                if (query.Limit.HasValue)
                {
                    commandText += " LIMIT " + query.Limit.Value.ToString(_usCulture);
                }

                var list = new List<AuthenticationInfo>();

                using (var statement = connection.PrepareStatement(commandText))
                {
                    BindAuthenticationQueryParams(query, statement);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        list.Add(Get(row));
                    }

                    using (var totalCountStatement = connection.PrepareStatement("select count (Id) from AccessTokens" + whereTextWithoutPaging))
                    {
                        BindAuthenticationQueryParams(query, totalCountStatement);

                        var count = totalCountStatement.ExecuteQuery()
                            .SelectScalarInt()
                            .First();

                        return new QueryResult<AuthenticationInfo>()
                        {
                            Items = list.ToArray(),
                            TotalRecordCount = count
                        };
                    }
                }
            }
        }

        public AuthenticationInfo Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    var commandText = BaseSelectText + " where Id=@Id";

                    using (var statement = connection.PrepareStatement(commandText))
                    {
                        statement.BindParameters["@Id"].Bind(id.ToGuidParamValue());

                        foreach (var row in statement.ExecuteQuery())
                        {
                            return Get(row);
                        }
                        return null;
                    }
                }
            }
        }

        private AuthenticationInfo Get(IReadOnlyList<IResultSetValue> reader)
        {
            var info = new AuthenticationInfo
            {
                Id = reader[0].ReadGuid().ToString("N"),
                AccessToken = reader[1].ToString()
            };

            if (reader[2].SQLiteType != SQLiteType.Null)
            {
                info.DeviceId = reader[2].ToString();
            }

            if (reader[3].SQLiteType != SQLiteType.Null)
            {
                info.AppName = reader[3].ToString();
            }

            if (reader[4].SQLiteType != SQLiteType.Null)
            {
                info.AppVersion = reader[4].ToString();
            }

            if (reader[5].SQLiteType != SQLiteType.Null)
            {
                info.DeviceName = reader[5].ToString();
            }

            if (reader[6].SQLiteType != SQLiteType.Null)
            {
                info.UserId = reader[6].ToString();
            }

            info.IsActive = reader[7].ToBool();
            info.DateCreated = reader[8].ReadDateTime();

            if (reader[9].SQLiteType != SQLiteType.Null)
            {
                info.DateRevoked = reader[9].ReadDateTime();
            }

            return info;
        }
    }
}
