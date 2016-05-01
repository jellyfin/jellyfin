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
        private IDbConnection _connection;
        private readonly IServerApplicationPaths _appPaths;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        private IDbCommand _saveInfoCommand;

        public AuthenticationRepository(ILogManager logManager, IServerApplicationPaths appPaths)
            : base(logManager)
        {
            _appPaths = appPaths;
        }

        public async Task Initialize(IDbConnector dbConnector)
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "authentication.db");

            _connection = await dbConnector.Connect(dbFile).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists AccessTokens (Id GUID PRIMARY KEY, AccessToken TEXT NOT NULL, DeviceId TEXT, AppName TEXT, AppVersion TEXT, DeviceName TEXT, UserId TEXT, IsActive BIT, DateCreated DATETIME NOT NULL, DateRevoked DATETIME)",
                                "create index if not exists idx_AccessTokens on AccessTokens(Id)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, Logger);

            _connection.AddColumn(Logger, "AccessTokens", "AppVersion", "TEXT");

            PrepareStatements();
        }

        private void PrepareStatements()
        {
            _saveInfoCommand = _connection.CreateCommand();
            _saveInfoCommand.CommandText = "replace into AccessTokens (Id, AccessToken, DeviceId, AppName, AppVersion, DeviceName, UserId, IsActive, DateCreated, DateRevoked) values (@Id, @AccessToken, @DeviceId, @AppName, @AppVersion, @DeviceName, @UserId, @IsActive, @DateCreated, @DateRevoked)";

            _saveInfoCommand.Parameters.Add(_saveInfoCommand, "@Id");
            _saveInfoCommand.Parameters.Add(_saveInfoCommand, "@AccessToken");
            _saveInfoCommand.Parameters.Add(_saveInfoCommand, "@DeviceId");
            _saveInfoCommand.Parameters.Add(_saveInfoCommand, "@AppName");
            _saveInfoCommand.Parameters.Add(_saveInfoCommand, "@AppVersion");
            _saveInfoCommand.Parameters.Add(_saveInfoCommand, "@DeviceName");
            _saveInfoCommand.Parameters.Add(_saveInfoCommand, "@UserId");
            _saveInfoCommand.Parameters.Add(_saveInfoCommand, "@IsActive");
            _saveInfoCommand.Parameters.Add(_saveInfoCommand, "@DateCreated");
            _saveInfoCommand.Parameters.Add(_saveInfoCommand, "@DateRevoked");
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

            await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                var index = 0;

                _saveInfoCommand.GetParameter(index++).Value = new Guid(info.Id);
                _saveInfoCommand.GetParameter(index++).Value = info.AccessToken;
                _saveInfoCommand.GetParameter(index++).Value = info.DeviceId;
                _saveInfoCommand.GetParameter(index++).Value = info.AppName;
                _saveInfoCommand.GetParameter(index++).Value = info.AppVersion;
                _saveInfoCommand.GetParameter(index++).Value = info.DeviceName;
                _saveInfoCommand.GetParameter(index++).Value = info.UserId;
                _saveInfoCommand.GetParameter(index++).Value = info.IsActive;
                _saveInfoCommand.GetParameter(index++).Value = info.DateCreated;
                _saveInfoCommand.GetParameter(index++).Value = info.DateRevoked;

                _saveInfoCommand.Transaction = transaction;

                _saveInfoCommand.ExecuteNonQuery();

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

                WriteLock.Release();
            }
        }

        private const string BaseSelectText = "select Id, AccessToken, DeviceId, AppName, AppVersion, DeviceName, UserId, IsActive, DateCreated, DateRevoked from AccessTokens";

        public QueryResult<AuthenticationInfo> Get(AuthenticationInfoQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException("query");
            }

            using (var cmd = _connection.CreateCommand())
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

        public AuthenticationInfo Get(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }

            var guid = new Guid(id);

            using (var cmd = _connection.CreateCommand())
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
