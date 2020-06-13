#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Data;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Security
{
    public class AuthenticationRepository : BaseSqliteRepository, IAuthenticationRepository
    {
        public AuthenticationRepository(ILogger<AuthenticationRepository> logger, IServerConfigurationManager config)
            : base(logger)
        {
            DbFilePath = Path.Combine(config.ApplicationPaths.DataPath, "authentication.db");
        }

        public void Initialize()
        {
            string[] queries =
            {
                "create table if not exists Tokens (Id INTEGER PRIMARY KEY, AccessToken TEXT NOT NULL, DeviceId TEXT NOT NULL, AppName TEXT NOT NULL, AppVersion TEXT NOT NULL, DeviceName TEXT NOT NULL, UserId TEXT, UserName TEXT, IsActive BIT NOT NULL, DateCreated DATETIME NOT NULL, DateLastActivity DATETIME NOT NULL)",
                "create table if not exists Devices (Id TEXT NOT NULL PRIMARY KEY, CustomName TEXT, Capabilities TEXT)",
                "drop index if exists idx_AccessTokens",
                "drop index if exists Tokens1",
                "drop index if exists Tokens2",

                "create index if not exists Tokens3 on Tokens (AccessToken, DateLastActivity)",
                "create index if not exists Tokens4 on Tokens (Id, DateLastActivity)",
                "create index if not exists Devices1 on Devices (Id)"
            };

            using (var connection = GetConnection())
            {
                var tableNewlyCreated = !TableExists(connection, "Tokens");

                connection.RunQueries(queries);

                TryMigrate(connection, tableNewlyCreated);
            }
        }

        private void TryMigrate(ManagedConnection connection, bool tableNewlyCreated)
        {
            try
            {
                if (tableNewlyCreated && TableExists(connection, "AccessTokens"))
                {
                    connection.RunInTransaction(db =>
                    {
                        var existingColumnNames = GetColumnNames(db, "AccessTokens");

                        AddColumn(db, "AccessTokens", "UserName", "TEXT", existingColumnNames);
                        AddColumn(db, "AccessTokens", "DateLastActivity", "DATETIME", existingColumnNames);
                        AddColumn(db, "AccessTokens", "AppVersion", "TEXT", existingColumnNames);

                    }, TransactionMode);

                    connection.RunQueries(new[]
                    {
                        "update accesstokens set DateLastActivity=DateCreated where DateLastActivity is null",
                        "update accesstokens set DeviceName='Unknown' where DeviceName is null",
                        "update accesstokens set AppName='Unknown' where AppName is null",
                        "update accesstokens set AppVersion='1' where AppVersion is null",
                        "INSERT INTO Tokens (AccessToken, DeviceId, AppName, AppVersion, DeviceName, UserId, UserName, IsActive, DateCreated, DateLastActivity) SELECT AccessToken, DeviceId, AppName, AppVersion, DeviceName, UserId, UserName, IsActive, DateCreated, DateLastActivity FROM AccessTokens where deviceid not null and devicename not null and appname not null and isactive=1"
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error migrating authentication database");
            }
        }

        public void Create(AuthenticationInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    using (var statement = db.PrepareStatement("insert into Tokens (AccessToken, DeviceId, AppName, AppVersion, DeviceName, UserId, UserName, IsActive, DateCreated, DateLastActivity) values (@AccessToken, @DeviceId, @AppName, @AppVersion, @DeviceName, @UserId, @UserName, @IsActive, @DateCreated, @DateLastActivity)"))
                    {
                        statement.TryBind("@AccessToken", info.AccessToken);

                        statement.TryBind("@DeviceId", info.DeviceId);
                        statement.TryBind("@AppName", info.AppName);
                        statement.TryBind("@AppVersion", info.AppVersion);
                        statement.TryBind("@DeviceName", info.DeviceName);
                        statement.TryBind("@UserId", (info.UserId.Equals(Guid.Empty) ? null : info.UserId.ToString("N", CultureInfo.InvariantCulture)));
                        statement.TryBind("@UserName", info.UserName);
                        statement.TryBind("@IsActive", true);
                        statement.TryBind("@DateCreated", info.DateCreated.ToDateTimeParamValue());
                        statement.TryBind("@DateLastActivity", info.DateLastActivity.ToDateTimeParamValue());

                        statement.MoveNext();
                    }

                }, TransactionMode);
            }
        }

        public void Update(AuthenticationInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    using (var statement = db.PrepareStatement("Update Tokens set AccessToken=@AccessToken, DeviceId=@DeviceId, AppName=@AppName, AppVersion=@AppVersion, DeviceName=@DeviceName, UserId=@UserId, UserName=@UserName, DateCreated=@DateCreated, DateLastActivity=@DateLastActivity where Id=@Id"))
                    {
                        statement.TryBind("@Id", info.Id);

                        statement.TryBind("@AccessToken", info.AccessToken);

                        statement.TryBind("@DeviceId", info.DeviceId);
                        statement.TryBind("@AppName", info.AppName);
                        statement.TryBind("@AppVersion", info.AppVersion);
                        statement.TryBind("@DeviceName", info.DeviceName);
                        statement.TryBind("@UserId", (info.UserId.Equals(Guid.Empty) ? null : info.UserId.ToString("N", CultureInfo.InvariantCulture)));
                        statement.TryBind("@UserName", info.UserName);
                        statement.TryBind("@DateCreated", info.DateCreated.ToDateTimeParamValue());
                        statement.TryBind("@DateLastActivity", info.DateLastActivity.ToDateTimeParamValue());

                        statement.MoveNext();
                    }
                }, TransactionMode);
            }
        }

        public void Delete(AuthenticationInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    using (var statement = db.PrepareStatement("Delete from Tokens where Id=@Id"))
                    {
                        statement.TryBind("@Id", info.Id);

                        statement.MoveNext();
                    }
                }, TransactionMode);
            }
        }

        private const string BaseSelectText = "select Tokens.Id, AccessToken, DeviceId, AppName, AppVersion, DeviceName, UserId, UserName, DateCreated, DateLastActivity, Devices.CustomName from Tokens left join Devices on Tokens.DeviceId=Devices.Id";

        private static void BindAuthenticationQueryParams(AuthenticationInfoQuery query, IStatement statement)
        {
            if (!string.IsNullOrEmpty(query.AccessToken))
            {
                statement.TryBind("@AccessToken", query.AccessToken);
            }

            if (!query.UserId.Equals(Guid.Empty))
            {
                statement.TryBind("@UserId", query.UserId.ToString("N", CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrEmpty(query.DeviceId))
            {
                statement.TryBind("@DeviceId", query.DeviceId);
            }
        }

        public QueryResult<AuthenticationInfo> Get(AuthenticationInfoQuery query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var commandText = BaseSelectText;

            var whereClauses = new List<string>();

            if (!string.IsNullOrEmpty(query.AccessToken))
            {
                whereClauses.Add("AccessToken=@AccessToken");
            }

            if (!string.IsNullOrEmpty(query.DeviceId))
            {
                whereClauses.Add("DeviceId=@DeviceId");
            }

            if (!query.UserId.Equals(Guid.Empty))
            {
                whereClauses.Add("UserId=@UserId");
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

            commandText += whereTextWithoutPaging;

            commandText += " ORDER BY DateLastActivity desc";

            if (query.Limit.HasValue || query.StartIndex.HasValue)
            {
                var offset = query.StartIndex ?? 0;

                if (query.Limit.HasValue || offset > 0)
                {
                    commandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                }

                if (offset > 0)
                {
                    commandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                }
            }

            var statementTexts = new[]
            {
                commandText,
                "select count (Id) from Tokens" + whereTextWithoutPaging
            };

            var list = new List<AuthenticationInfo>();
            var result = new QueryResult<AuthenticationInfo>();
            using (var connection = GetConnection(true))
            {
                connection.RunInTransaction(
                    db =>
                    {
                        var statements = PrepareAll(db, statementTexts)
                            .ToList();

                        using (var statement = statements[0])
                        {
                            BindAuthenticationQueryParams(query, statement);

                            foreach (var row in statement.ExecuteQuery())
                            {
                                list.Add(Get(row));
                            }

                            using (var totalCountStatement = statements[1])
                            {
                                BindAuthenticationQueryParams(query, totalCountStatement);

                                result.TotalRecordCount = totalCountStatement.ExecuteQuery()
                                    .SelectScalarInt()
                                    .First();
                            }
                        }
                    },
                    ReadTransactionMode);
            }

            result.Items = list.ToArray();
            return result;
        }

        private static AuthenticationInfo Get(IReadOnlyList<IResultSetValue> reader)
        {
            var info = new AuthenticationInfo
            {
                Id = reader[0].ToInt64(),
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
                info.UserId = new Guid(reader[6].ToString());
            }

            if (reader[7].SQLiteType != SQLiteType.Null)
            {
                info.UserName = reader[7].ToString();
            }

            info.DateCreated = reader[8].ReadDateTime();

            if (reader[9].SQLiteType != SQLiteType.Null)
            {
                info.DateLastActivity = reader[9].ReadDateTime();
            }
            else
            {
                info.DateLastActivity = info.DateCreated;
            }

            if (reader[10].SQLiteType != SQLiteType.Null)
            {
                info.DeviceName = reader[10].ToString();
            }

            return info;
        }

        public DeviceOptions GetDeviceOptions(string deviceId)
        {
            using (var connection = GetConnection(true))
            {
                return connection.RunInTransaction(db =>
                {
                    using (var statement = base.PrepareStatement(db, "select CustomName from Devices where Id=@DeviceId"))
                    {
                        statement.TryBind("@DeviceId", deviceId);

                        var result = new DeviceOptions();

                        foreach (var row in statement.ExecuteQuery())
                        {
                            if (row[0].SQLiteType != SQLiteType.Null)
                            {
                                result.CustomName = row[0].ToString();
                            }
                        }

                        return result;
                    }

                }, ReadTransactionMode);
            }
        }

        public void UpdateDeviceOptions(string deviceId, DeviceOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    using (var statement = db.PrepareStatement("replace into devices (Id, CustomName, Capabilities) VALUES (@Id, @CustomName, (Select Capabilities from Devices where Id=@Id))"))
                    {
                        statement.TryBind("@Id", deviceId);

                        if (string.IsNullOrWhiteSpace(options.CustomName))
                        {
                            statement.TryBindNull("@CustomName");
                        }
                        else
                        {
                            statement.TryBind("@CustomName", options.CustomName);
                        }

                        statement.MoveNext();
                    }

                }, TransactionMode);
            }
        }
    }
}
