using MediaBrowser.Controller;
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

namespace MediaBrowser.Server.Implementations.Persistence
{
    /// <summary>
    /// Class SQLiteUserRepository
    /// </summary>
    public class SqliteUserRepository : BaseSqliteRepository, IUserRepository
    {
        private readonly IJsonSerializer _jsonSerializer;

        public SqliteUserRepository(ILogManager logManager, IServerApplicationPaths appPaths, IJsonSerializer jsonSerializer, IDbConnector dbConnector) : base(logManager, dbConnector)
        {
            _jsonSerializer = jsonSerializer;

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
        public async Task Initialize()
        {
            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                string[] queries = {

                                "create table if not exists users (guid GUID primary key, data BLOB)",
                                "create index if not exists idx_users on users(guid)",
                                "create table if not exists schema_version (table_name primary key, version)",

                                "pragma shrink_memory"
                               };

                connection.RunQueries(queries, Logger);
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

            var serialized = _jsonSerializer.SerializeToBytes(user);

            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                IDbTransaction transaction = null;

                try
                {
                    transaction = connection.BeginTransaction();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "replace into users (guid, data) values (@1, @2)";
                        cmd.Parameters.Add(cmd, "@1", DbType.Guid).Value = user.Id;
                        cmd.Parameters.Add(cmd, "@2", DbType.Binary).Value = serialized;

                        cmd.Transaction = transaction;

                        cmd.ExecuteNonQuery();
                    }

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
                    Logger.ErrorException("Failed to save user:", e);

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

        /// <summary>
        /// Retrieve all users from the database
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        public IEnumerable<User> RetrieveAllUsers()
        {
            var list = new List<User>();

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select guid,data from users";

                    using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                    {
                        while (reader.Read())
                        {
                            var id = reader.GetGuid(0);

                            using (var stream = reader.GetMemoryStream(1))
                            {
                                var user = _jsonSerializer.DeserializeFromStream<User>(stream);
                                user.Id = id;
                                list.Add(user);
                            }
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

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                IDbTransaction transaction = null;

                try
                {
                    transaction = connection.BeginTransaction();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "delete from users where guid=@guid";

                        cmd.Parameters.Add(cmd, "@guid", DbType.Guid).Value = user.Id;

                        cmd.Transaction = transaction;

                        cmd.ExecuteNonQuery();
                    }

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
                    Logger.ErrorException("Failed to delete user:", e);

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
}