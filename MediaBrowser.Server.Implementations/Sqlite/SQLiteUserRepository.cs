using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sqlite
{
    /// <summary>
    /// Class SQLiteUserRepository
    /// </summary>
    public class SQLiteUserRepository : SqliteRepository, IUserRepository
    {
        /// <summary>
        /// The repository name
        /// </summary>
        public const string RepositoryName = "SQLite";

        /// <summary>
        /// Gets the name of the repository
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return RepositoryName;
            }
        }

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IApplicationPaths _appPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteUserDataRepository" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logManager">The log manager.</param>
        /// <exception cref="System.ArgumentNullException">appPaths</exception>
        public SQLiteUserRepository(IApplicationPaths appPaths, IJsonSerializer jsonSerializer, ILogManager logManager)
            : base(logManager)
        {
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }

            _appPaths = appPaths;
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "users.db");

            await ConnectToDB(dbFile).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists users (guid GUID primary key, data BLOB)",
                                "create index if not exists idx_users on users(guid)",
                                "create table if not exists schema_version (table_name primary key, version)",
                                //pragmas
                                "pragma temp_store = memory"
                               };

            RunQueries(queries);
        }

        /// <summary>
        /// Save a user in the repo
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public Task SaveUser(User user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var serialized = _jsonSerializer.SerializeToBytes(user);

                cancellationToken.ThrowIfCancellationRequested();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "replace into users (guid, data) values (@1, @2)";
                cmd.AddParam("@1", user.Id);
                cmd.AddParam("@2", serialized);
                QueueCommand(cmd);
            });
        }

        /// <summary>
        /// Retrieve all users from the database
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        public IEnumerable<User> RetrieveAllUsers()
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = "select data from users";

            using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
            {
                while (reader.Read())
                {
                    using (var stream = GetStream(reader, 0))
                    {
                        var user = _jsonSerializer.DeserializeFromStream<User>(stream);
                        yield return user;
                    }
                }
            }
        }

        /// <summary>
        /// Deletes the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public Task DeleteUser(User user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "delete from users where guid=@guid";
                var guidParam = cmd.Parameters.Add("@guid", DbType.Guid);
                guidParam.Value = user.Id;

                return ExecuteCommand(cmd);
            });
        }
    }
}
