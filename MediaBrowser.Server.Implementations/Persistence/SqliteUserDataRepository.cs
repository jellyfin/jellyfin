using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class SqliteUserDataRepository : IUserDataRepository
    {
        private readonly ILogger _logger;

        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        private IDbConnection _connection;

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
        /// The _app paths
        /// </summary>
        private readonly IApplicationPaths _appPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteUserDataRepository" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="logManager">The log manager.</param>
        /// <exception cref="System.ArgumentNullException">jsonSerializer
        /// or
        /// appPaths</exception>
        public SqliteUserDataRepository(IApplicationPaths appPaths, ILogManager logManager)
        {
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }

            _appPaths = appPaths;
            _logger = logManager.GetLogger(GetType().Name);
        }

        private SqliteShrinkMemoryTimer _shrinkMemoryTimer;
        
        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "userdata_v2.db");

            _connection = await SqliteExtensions.ConnectToDb(dbFile, _logger).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists userdata (key nvarchar, userId GUID, rating float null, played bit, playCount int, isFavorite bit, playbackPositionTicks bigint, lastPlayedDate datetime null)",

                                "create unique index if not exists userdataindex on userdata (key, userId)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, _logger);

            _shrinkMemoryTimer = new SqliteShrinkMemoryTimer(_connection, _writeLock, _logger);
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
        public Task SaveUserData(Guid userId, string key, UserItemData userData, CancellationToken cancellationToken)
        {
            if (userData == null)
            {
                throw new ArgumentNullException("userData");
            }
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            return PersistUserData(userId, key, userData, cancellationToken);
        }

        public Task SaveAllUserData(Guid userId, IEnumerable<UserItemData> userData, CancellationToken cancellationToken)
        {
            if (userData == null)
            {
                throw new ArgumentNullException("userData");
            }
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            return PersistAllUserData(userId, userData, cancellationToken);
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

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = "replace into userdata (key, userId, rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate) values (@key, @userId, @rating,@played,@playCount,@isFavorite,@playbackPositionTicks,@lastPlayedDate)";

                    cmd.Parameters.Add(cmd, "@key", DbType.String).Value = key;
                    cmd.Parameters.Add(cmd, "@userId", DbType.Guid).Value = userId;
                    cmd.Parameters.Add(cmd, "@rating", DbType.Double).Value = userData.Rating;
                    cmd.Parameters.Add(cmd, "@played", DbType.Boolean).Value = userData.Played;
                    cmd.Parameters.Add(cmd, "@playCount", DbType.Int32).Value = userData.PlayCount;
                    cmd.Parameters.Add(cmd, "@isFavorite", DbType.Boolean).Value = userData.IsFavorite;
                    cmd.Parameters.Add(cmd, "@playbackPositionTicks", DbType.Int64).Value = userData.PlaybackPositionTicks;
                    cmd.Parameters.Add(cmd, "@lastPlayedDate", DbType.DateTime).Value = userData.LastPlayedDate;

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
                _logger.ErrorException("Failed to save user data:", e);

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

                _writeLock.Release();
            }
        }

        /// <summary>
        /// Persist all user data for the specified user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task PersistAllUserData(Guid userId, IEnumerable<UserItemData> userData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                foreach (var userItemData in userData)
                {
                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandText = "replace into userdata (key, userId, rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate) values (@key, @userId, @rating,@played,@playCount,@isFavorite,@playbackPositionTicks,@lastPlayedDate)";

                        cmd.Parameters.Add(cmd, "@key", DbType.String).Value = userItemData.Key;
                        cmd.Parameters.Add(cmd, "@userId", DbType.Guid).Value = userId;
                        cmd.Parameters.Add(cmd, "@rating", DbType.Double).Value = userItemData.Rating;
                        cmd.Parameters.Add(cmd, "@played", DbType.Boolean).Value = userItemData.Played;
                        cmd.Parameters.Add(cmd, "@playCount", DbType.Int32).Value = userItemData.PlayCount;
                        cmd.Parameters.Add(cmd, "@isFavorite", DbType.Boolean).Value = userItemData.IsFavorite;
                        cmd.Parameters.Add(cmd, "@playbackPositionTicks", DbType.Int64).Value = userItemData.PlaybackPositionTicks;
                        cmd.Parameters.Add(cmd, "@lastPlayedDate", DbType.DateTime).Value = userItemData.LastPlayedDate;

                        cmd.Transaction = transaction;

                        cmd.ExecuteNonQuery();
                    }
                    
                    cancellationToken.ThrowIfCancellationRequested();
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
                _logger.ErrorException("Failed to save user data:", e);

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

                _writeLock.Release();
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
        public UserItemData GetUserData(Guid userId, string key)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate from userdata where key = @key and userId=@userId";

                cmd.Parameters.Add(cmd, "@key", DbType.String).Value = key;
                cmd.Parameters.Add(cmd, "@userId", DbType.Guid).Value = userId;

                var userData = new UserItemData
                {
                    UserId = userId,
                    Key = key
                };

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        ReadRow(reader, ref userData);
                    }
                }

                return userData;
            }
        }

        /// <summary>
        /// Return all user-data associated with the given user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public IEnumerable<UserItemData> GetAllUserData(Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate,key from userdata where userId=@userId";

                cmd.Parameters.Add(cmd, "@userId", DbType.Guid).Value = userId;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        var userData = new UserItemData
                        {
                            UserId = userId,
                        };
                        ReadRow(reader, ref userData);
                        userData.Key = reader.GetString(6);
                        yield return userData;
                    }
                }

            }
            
        }

        /// <summary>
        /// Read a row from the specified reader into the provided userData object
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="userData"></param>
        private static void ReadRow(IDataReader reader, ref UserItemData userData)
        {
            if (!reader.IsDBNull(0))
            {
                userData.Rating = reader.GetDouble(0);
            }

            userData.Played = reader.GetBoolean(1);
            userData.PlayCount = reader.GetInt32(2);
            userData.IsFavorite = reader.GetBoolean(3);
            userData.PlaybackPositionTicks = reader.GetInt64(4);

            if (!reader.IsDBNull(5))
            {
                userData.LastPlayedDate = reader.GetDateTime(5).ToUniversalTime();
            }
            
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private readonly object _disposeLock = new object();

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                try
                {
                    lock (_disposeLock)
                    {
                        if (_shrinkMemoryTimer != null)
                        {
                            _shrinkMemoryTimer.Dispose();
                            _shrinkMemoryTimer = null;
                        }

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
                catch (Exception ex)
                {
                    _logger.ErrorException("Error disposing database", ex);
                }
            }
        }
    }
}