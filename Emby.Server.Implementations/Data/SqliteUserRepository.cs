using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    /// <summary>
    /// Class SQLiteUserRepository
    /// </summary>
    public class SqliteUserRepository : BaseSqliteRepository, IUserRepository
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IMemoryStreamFactory _memoryStreamProvider;

        public SqliteUserRepository(ILogger logger, IServerApplicationPaths appPaths, IJsonSerializer jsonSerializer, IMemoryStreamFactory memoryStreamProvider)
            : base(logger)
        {
            _jsonSerializer = jsonSerializer;
            _memoryStreamProvider = memoryStreamProvider;

            DbFilePath = Path.Combine(appPaths.DataPath, "users.db");
        }

        /// <summary>
        /// Gets the name of the repository
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return "SQLite";
            }
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public void Initialize()
        {
            using (var connection = CreateConnection())
            {
                string[] queries = {

                                "create table if not exists users (guid GUID primary key, data BLOB)",
                                "create index if not exists idx_users on users(guid)",
                                "create table if not exists schema_version (table_name primary key, version)",

                                "pragma shrink_memory"
                               };

                connection.RunQueries(queries);
            }
        }

        /// <summary>
        /// Save a user in the repo
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public async Task SaveUser(User user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var serialized = _jsonSerializer.SerializeToBytes(user, _memoryStreamProvider);

            cancellationToken.ThrowIfCancellationRequested();

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        var commandText = "replace into users (guid, data) values (?, ?)";

                        db.Execute(commandText,
                            user.Id.ToGuidParamValue(),
                            serialized);
                    });
                }
            }
        }

        /// <summary>
        /// Retrieve all users from the database
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        public IEnumerable<User> RetrieveAllUsers()
        {
            var list = new List<User>();

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    foreach (var row in connection.Query("select guid,data from users"))
                    {
                        var id = row[0].ReadGuid();

                        using (var stream = _memoryStreamProvider.CreateNew(row[1].ToBlob()))
                        {
                            stream.Position = 0;
                            var user = _jsonSerializer.DeserializeFromStream<User>(stream);
                            user.Id = id;
                            list.Add(user);
                        }
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Deletes the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public async Task DeleteUser(User user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        var commandText = "delete from users where guid=?";

                        db.Execute(commandText,
                            user.Id.ToGuidParamValue());
                    });
                }
            }
        }
    }
}