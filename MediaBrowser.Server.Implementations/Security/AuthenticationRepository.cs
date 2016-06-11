using MediaBrowser.Controller;
using MediaBrowser.Controller.Security;
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

namespace MediaBrowser.Server.Implementations.Security
{
    public class AuthenticationRepository : BaseSqliteRepository, IAuthenticationRepository
    {
        private readonly IServerApplicationPaths _appPaths;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public AuthenticationRepository(ILogManager logManager, IServerApplicationPaths appPaths, IDbConnector connector)
            : base(logManager, connector)
        {
            _appPaths = appPaths;
            DbFilePath = Path.Combine(appPaths.DataPath, "authentication.db");
        }

        public async Task Initialize()
        {
            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                string[] queries = {

                                "create table if not exists AccessTokens (Id GUID PRIMARY KEY, AccessToken TEXT NOT NULL, DeviceId TEXT, AppName TEXT, AppVersion TEXT, DeviceName TEXT, UserId TEXT, IsActive BIT, DateCreated DATETIME NOT NULL, DateRevoked DATETIME)",
                                "create index if not exists idx_AccessTokens on AccessTokens(Id)"
                               };

                connection.RunQueries(queries, Logger);

                connection.AddColumn(Logger, "AccessTokens", "AppVersion", "TEXT");
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

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                using (var saveInfoCommand = connection.CreateCommand())
                {
                    saveInfoCommand.CommandText = "replace into AccessTokens (Id, AccessToken, DeviceId, AppName, AppVersion, DeviceName, UserId, IsActive, DateCreated, DateRevoked) values (@Id, @AccessToken, @DeviceId, @AppName, @AppVersion, @DeviceName, @UserId, @IsActive, @DateCreated, @DateRevoked)";

                    saveInfoCommand.Parameters.Add(saveInfoCommand, "@Id");
                    saveInfoCommand.Parameters.Add(saveInfoCommand, "@AccessToken");
                    saveInfoCommand.Parameters.Add(saveInfoCommand, "@DeviceId");
                    saveInfoCommand.Parameters.Add(saveInfoCommand, "@AppName");
                    saveInfoCommand.Parameters.Add(saveInfoCommand, "@AppVersion");
                    saveInfoCommand.Parameters.Add(saveInfoCommand, "@DeviceName");
                    saveInfoCommand.Parameters.Add(saveInfoCommand, "@UserId");
                    saveInfoCommand.Parameters.Add(saveInfoCommand, "@IsActive");
                    saveInfoCommand.Parameters.Add(saveInfoCommand, "@DateCreated");
                    saveInfoCommand.Parameters.Add(saveInfoCommand, "@DateRevoked");

                    IDbTransaction transaction = null;

                    try
                    {
                        transaction = connection.BeginTransaction();

                        var index = 0;

                        saveInfoCommand.GetParameter(index++).Value = new Guid(info.Id);
                        saveInfoCommand.GetParameter(index++).Value = info.AccessToken;
                        saveInfoCommand.GetParameter(index++).Value = info.DeviceId;
                        saveInfoCommand.GetParameter(index++).Value = info.AppName;
                        saveInfoCommand.GetParameter(index++).Value = info.AppVersion;
                        saveInfoCommand.GetParameter(index++).Value = info.DeviceName;
                        saveInfoCommand.GetParameter(index++).Value = info.UserId;
                        saveInfoCommand.GetParameter(index++).Value = info.IsActive;
                        saveInfoCommand.GetParameter(index++).Value = info.DateCreated;
                        saveInfoCommand.GetParameter(index++).Value = info.DateRevoked;

                        saveInfoCommand.Transaction = transaction;

                        saveInfoCommand.ExecuteNonQuery();

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

        private const string BaseSelectText = "select Id, AccessToken, DeviceId, AppName, AppVersion, DeviceName, UserId, IsActive, DateCreated, DateRevoked from AccessTokens";

        public QueryResult<AuthenticationInfo> Get(AuthenticationInfoQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = BaseSelectText;

                    var whereClauses = new List<string>();

                    var startIndex = query.StartIndex ?? 0;

                    if (!string.IsNullOrWhiteSpace(query.AccessToken))
                    {
                        whereClauses.Add("AccessToken=@AccessToken");
                        cmd.Parameters.Add(cmd, "@AccessToken", DbType.String).Value = query.AccessToken;
                    }

                    if (!string.IsNullOrWhiteSpace(query.UserId))
                    {
                        whereClauses.Add("UserId=@UserId");
                        cmd.Parameters.Add(cmd, "@UserId", DbType.String).Value = query.UserId;
                    }

                    if (!string.IsNullOrWhiteSpace(query.DeviceId))
                    {
                        whereClauses.Add("DeviceId=@DeviceId");
                        cmd.Parameters.Add(cmd, "@DeviceId", DbType.String).Value = query.DeviceId;
                    }

                    if (query.IsActive.HasValue)
                    {
                        whereClauses.Add("IsActive=@IsActive");
                        cmd.Parameters.Add(cmd, "@IsActive", DbType.Boolean).Value = query.IsActive.Value;
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

                    cmd.CommandText += whereText;

                    cmd.CommandText += " ORDER BY DateCreated";

                    if (query.Limit.HasValue)
                    {
                        cmd.CommandText += " LIMIT " + query.Limit.Value.ToString(_usCulture);
                    }

                    cmd.CommandText += "; select count (Id) from AccessTokens" + whereTextWithoutPaging;

                    var list = new List<AuthenticationInfo>();
                    var count = 0;

                    using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        while (reader.Read())
                        {
                            list.Add(Get(reader));
                        }

                        if (reader.NextResult() && reader.Read())
                        {
                            count = reader.GetInt32(0);
                        }
                    }

                    return new QueryResult<AuthenticationInfo>()
                    {
                        Items = list.ToArray(),
                        TotalRecordCount = count
                    };
                }
            }
        }

        public AuthenticationInfo Get(string id)
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
                    cmd.CommandText = BaseSelectText + " where Id=@Id";

                    cmd.Parameters.Add(cmd, "@Id", DbType.Guid).Value = guid;

                    using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                    {
                        if (reader.Read())
                        {
                            return Get(reader);
                        }
                    }
                }

                return null;
            }
        }

        private AuthenticationInfo Get(IDataReader reader)
        {
            var info = new AuthenticationInfo
            {
                Id = reader.GetGuid(0).ToString("N"),
                AccessToken = reader.GetString(1)
            };

            if (!reader.IsDBNull(2))
            {
                info.DeviceId = reader.GetString(2);
            }

            if (!reader.IsDBNull(3))
            {
                info.AppName = reader.GetString(3);
            }

            if (!reader.IsDBNull(4))
            {
                info.AppVersion = reader.GetString(4);
            }

            if (!reader.IsDBNull(5))
            {
                info.DeviceName = reader.GetString(5);
            }

            if (!reader.IsDBNull(6))
            {
                info.UserId = reader.GetString(6);
            }

            info.IsActive = reader.GetBoolean(7);
            info.DateCreated = reader.GetDateTime(8).ToUniversalTime();

            if (!reader.IsDBNull(9))
            {
                info.DateRevoked = reader.GetDateTime(9).ToUniversalTime();
            }

            return info;
        }
    }
}
