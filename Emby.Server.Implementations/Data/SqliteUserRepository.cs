#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MediaBrowser.Common.Json;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    /// <summary>
    /// Class SQLiteUserRepository
    /// </summary>
    public class SqliteUserRepository : BaseSqliteRepository, IUserRepository
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public SqliteUserRepository(
            ILogger<SqliteUserRepository> logger,
            IServerApplicationPaths appPaths)
            : base(logger)
        {
            _jsonOptions = JsonDefaults.GetOptions();

            DbFilePath = Path.Combine(appPaths.DataPath, "users.db");
        }

        /// <summary>
        /// Gets the name of the repository
        /// </summary>
        /// <value>The name.</value>
        public string Name => "SQLite";

        /// <summary>
        /// Opens the connection to the database.
        /// </summary>
        public void Initialize()
        {
            using (var connection = GetConnection())
            {
                var localUsersTableExists = TableExists(connection, "LocalUsersv2");

                connection.RunQueries(new[] {
                    "create table if not exists LocalUsersv2 (Id INTEGER PRIMARY KEY, guid GUID NOT NULL, data BLOB NOT NULL)",
                    "drop index if exists idx_users"
                });

                if (!localUsersTableExists && TableExists(connection, "Users"))
                {
                    TryMigrateToLocalUsersTable(connection);
                }

                RemoveEmptyPasswordHashes(connection);
            }
        }

        private void TryMigrateToLocalUsersTable(ManagedConnection connection)
        {
            try
            {
                connection.RunQueries(new[]
                {
                    "INSERT INTO LocalUsersv2 (guid, data) SELECT guid,data from users"
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error migrating users database");
            }
        }

        private void RemoveEmptyPasswordHashes(ManagedConnection connection)
        {
            foreach (var user in RetrieveAllUsers(connection))
            {
                // If the user password is the sha1 hash of the empty string, remove it
                if (!string.Equals(user.Password, "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", StringComparison.Ordinal)
                    && !string.Equals(user.Password, "$SHA1$DA39A3EE5E6B4B0D3255BFEF95601890AFD80709", StringComparison.Ordinal))
                {
                    continue;
                }

                user.Password = null;
                var serialized = JsonSerializer.SerializeToUtf8Bytes(user, _jsonOptions);

                connection.RunInTransaction(db =>
                {
                    using (var statement = db.PrepareStatement("update LocalUsersv2 set data=@data where Id=@InternalId"))
                    {
                        statement.TryBind("@InternalId", user.InternalId);
                        statement.TryBind("@data", serialized);
                        statement.MoveNext();
                    }
                }, TransactionMode);
            }
        }

        /// <summary>
        /// Save a user in the repo
        /// </summary>
        public void CreateUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var serialized = JsonSerializer.SerializeToUtf8Bytes(user, _jsonOptions);

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    using (var statement = db.PrepareStatement("insert into LocalUsersv2 (guid, data) values (@guid, @data)"))
                    {
                        statement.TryBind("@guid", user.Id.ToByteArray());
                        statement.TryBind("@data", serialized);

                        statement.MoveNext();
                    }

                    var createdUser = GetUser(user.Id, connection);

                    if (createdUser == null)
                    {
                        throw new ApplicationException("created user should never be null");
                    }

                    user.InternalId = createdUser.InternalId;

                }, TransactionMode);
            }
        }

        public void UpdateUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var serialized = JsonSerializer.SerializeToUtf8Bytes(user, _jsonOptions);

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    using (var statement = db.PrepareStatement("update LocalUsersv2 set data=@data where Id=@InternalId"))
                    {
                        statement.TryBind("@InternalId", user.InternalId);
                        statement.TryBind("@data", serialized);
                        statement.MoveNext();
                    }

                }, TransactionMode);
            }
        }

        private User GetUser(Guid guid, ManagedConnection connection)
        {
            using (var statement = connection.PrepareStatement("select id,guid,data from LocalUsersv2 where guid=@guid"))
            {
                statement.TryBind("@guid", guid);

                foreach (var row in statement.ExecuteQuery())
                {
                    return GetUser(row);
                }
            }

            return null;
        }

        private User GetUser(IReadOnlyList<IResultSetValue> row)
        {
            var id = row[0].ToInt64();
            var guid = row[1].ReadGuidFromBlob();

            var user = JsonSerializer.Deserialize<User>(row[2].ToBlob(), _jsonOptions);
            user.InternalId = id;
            user.Id = guid;
            return user;
        }

        /// <summary>
        /// Retrieve all users from the database
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        public List<User> RetrieveAllUsers()
        {
            using (var connection = GetConnection(true))
            {
                return new List<User>(RetrieveAllUsers(connection));
            }
        }

        /// <summary>
        /// Retrieve all users from the database
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        private IEnumerable<User> RetrieveAllUsers(ManagedConnection connection)
        {
            foreach (var row in connection.Query("select id,guid,data from LocalUsersv2"))
            {
                yield return GetUser(row);
            }
        }

        /// <summary>
        /// Deletes the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">user</exception>
        public void DeleteUser(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    using (var statement = db.PrepareStatement("delete from LocalUsersv2 where Id=@id"))
                    {
                        statement.TryBind("@id", user.InternalId);
                        statement.MoveNext();
                    }
                }, TransactionMode);
            }
        }
    }
}
