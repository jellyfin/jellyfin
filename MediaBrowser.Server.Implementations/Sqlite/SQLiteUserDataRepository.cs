using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sqlite
{
    /// <summary>
    /// Class SQLiteUserDataRepository
    /// </summary>
    public class SQLiteUserDataRepository : SqliteRepository, IUserDataRepository
    {
        private readonly ConcurrentDictionary<string, Task<UserItemData>> _userData = new ConcurrentDictionary<string, Task<UserItemData>>();
        
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
        /// Gets a value indicating whether [enable delayed commands].
        /// </summary>
        /// <value><c>true</c> if [enable delayed commands]; otherwise, <c>false</c>.</value>
        protected override bool EnableDelayedCommands
        {
            get
            {
                return false;
            }
        }
        
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
        /// <exception cref="System.ArgumentNullException">
        /// jsonSerializer
        /// or
        /// appPaths
        /// </exception>
        public SQLiteUserDataRepository(IApplicationPaths appPaths, IJsonSerializer jsonSerializer, ILogManager logManager)
            : base(logManager)
        {
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }

            _jsonSerializer = jsonSerializer;
            _appPaths = appPaths;
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "userdata.db");

            await ConnectToDb(dbFile).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists userdata (key nvarchar, userId GUID, data BLOB)",
                                "create unique index if not exists userdataindex on userdata (key, userId)",
                                "create table if not exists schema_version (table_name primary key, version)",
                                //pragmas
                                "pragma temp_store = memory"
                               };

            RunQueries(queries);
        }

        /// <summary>
        /// Saves the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">userData
        /// or
        /// cancellationToken
        /// or
        /// userId
        /// or
        /// userDataId</exception>
        public async Task SaveUserData(Guid userId, string key, UserItemData userData, CancellationToken cancellationToken)
        {
            if (userData == null)
            {
                throw new ArgumentNullException("userData");
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await PersistUserData(userId, key, userData, cancellationToken).ConfigureAwait(false);

                var newValue = Task.FromResult(userData);

                // Once it succeeds, put it into the dictionary to make it available to everyone else
                _userData.AddOrUpdate(key, newValue, delegate { return newValue; });
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error saving user data", ex);

                throw;
            }
        }

        /// <summary>
        /// Persists the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task PersistUserData(Guid userId, string key, UserItemData userData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var serialized = _jsonSerializer.SerializeToBytes(userData);

            cancellationToken.ThrowIfCancellationRequested();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "replace into userdata (key, userId, data) values (@1, @2, @3)";
            cmd.AddParam("@1", key);
            cmd.AddParam("@2", userId);
            cmd.AddParam("@3", serialized);

            using (var tran = connection.BeginTransaction())
            {
                try
                {
                    cmd.Transaction = tran;

                    await cmd.ExecuteNonQueryAsync(cancellationToken);

                    tran.Commit();
                }
                catch (OperationCanceledException)
                {
                    tran.Rollback();
                }
                catch (Exception e)
                {
                    Logger.ErrorException("Failed to commit transaction.", e);
                    tran.Rollback();
                }
            }
        }

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>Task{UserItemData}.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// userId
        /// or
        /// key
        /// </exception>
        public Task<UserItemData> GetUserData(Guid userId, string key)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }
            
            return _userData.GetOrAdd(key, keyName => RetrieveUserData(userId, key));
        }

        /// <summary>
        /// Retrieves the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>Task{UserItemData}.</returns>
        private async Task<UserItemData> RetrieveUserData(Guid userId, string key)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = "select data from userdata where key = @key and userId=@userId";

            var idParam = cmd.Parameters.Add("@key", DbType.String);
            idParam.Value = key;

            var userIdParam = cmd.Parameters.Add("@userId", DbType.Guid);
            userIdParam.Value = userId;

            using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow).ConfigureAwait(false))
            {
                if (reader.Read())
                {
                    using (var stream = GetStream(reader, 0))
                    {
                        return _jsonSerializer.DeserializeFromStream<UserItemData>(stream);
                    }
                }
            }

            return new UserItemData();
        }
    }
}
