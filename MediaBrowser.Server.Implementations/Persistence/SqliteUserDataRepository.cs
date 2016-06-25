using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class SqliteUserDataRepository : BaseSqliteRepository, IUserDataRepository
    {
        private IDbConnection _connection;

        public SqliteUserDataRepository(ILogManager logManager, IApplicationPaths appPaths, IDbConnector connector) : base(logManager, connector)
        {
            DbFilePath = Path.Combine(appPaths.DataPath, "userdata_v2.db");
        }

        protected override bool EnableConnectionPooling
        {
            get { return false; }
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

        protected override async Task<IDbConnection> CreateConnection(bool isReadOnly = false)
        {
            var connection = await DbConnector.Connect(DbFilePath, false, false, 10000).ConfigureAwait(false);

            connection.RunQueries(new[]
            {
                "pragma temp_store = memory"

            }, Logger);

            return connection;
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize(IDbConnection connection, SemaphoreSlim writeLock)
        {
            WriteLock.Dispose();
            WriteLock = writeLock;
            _connection = connection;

            string[] queries = {

                                "create table if not exists UserDataDb.userdata (key nvarchar, userId GUID, rating float null, played bit, playCount int, isFavorite bit, playbackPositionTicks bigint, lastPlayedDate datetime null)",

                                "drop index if exists UserDataDb.idx_userdata",
                                "drop index if exists UserDataDb.idx_userdata1",
                                "drop index if exists UserDataDb.idx_userdata2",
                                "drop index if exists UserDataDb.userdataindex1",

                                "create unique index if not exists UserDataDb.userdataindex on userdata (key, userId)",
                                "create index if not exists UserDataDb.userdataindex2 on userdata (key, userId, played)",
                                "create index if not exists UserDataDb.userdataindex3 on userdata (key, userId, playbackPositionTicks)",
                                "create index if not exists UserDataDb.userdataindex4 on userdata (key, userId, isFavorite)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, Logger);

            _connection.AddColumn(Logger, "userdata", "AudioStreamIndex", "int");
            _connection.AddColumn(Logger, "userdata", "SubtitleStreamIndex", "int");
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

            await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                using (var cmd = _connection.CreateCommand())
                {
                    cmd.CommandText = "replace into userdata (key, userId, rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate,AudioStreamIndex,SubtitleStreamIndex) values (@key, @userId, @rating,@played,@playCount,@isFavorite,@playbackPositionTicks,@lastPlayedDate,@AudioStreamIndex,@SubtitleStreamIndex)";

                    cmd.Parameters.Add(cmd, "@key", DbType.String).Value = key;
                    cmd.Parameters.Add(cmd, "@userId", DbType.Guid).Value = userId;
                    cmd.Parameters.Add(cmd, "@rating", DbType.Double).Value = userData.Rating;
                    cmd.Parameters.Add(cmd, "@played", DbType.Boolean).Value = userData.Played;
                    cmd.Parameters.Add(cmd, "@playCount", DbType.Int32).Value = userData.PlayCount;
                    cmd.Parameters.Add(cmd, "@isFavorite", DbType.Boolean).Value = userData.IsFavorite;
                    cmd.Parameters.Add(cmd, "@playbackPositionTicks", DbType.Int64).Value = userData.PlaybackPositionTicks;
                    cmd.Parameters.Add(cmd, "@lastPlayedDate", DbType.DateTime).Value = userData.LastPlayedDate;
                    cmd.Parameters.Add(cmd, "@AudioStreamIndex", DbType.Int32).Value = userData.AudioStreamIndex;
                    cmd.Parameters.Add(cmd, "@SubtitleStreamIndex", DbType.Int32).Value = userData.SubtitleStreamIndex;

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
                Logger.ErrorException("Failed to save user data:", e);

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

            await WriteLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                foreach (var userItemData in userData)
                {
                    using (var cmd = _connection.CreateCommand())
                    {
                        cmd.CommandText = "replace into userdata (key, userId, rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate,AudioStreamIndex,SubtitleStreamIndex) values (@key, @userId, @rating,@played,@playCount,@isFavorite,@playbackPositionTicks,@lastPlayedDate,@AudioStreamIndex,@SubtitleStreamIndex)";

                        cmd.Parameters.Add(cmd, "@key", DbType.String).Value = userItemData.Key;
                        cmd.Parameters.Add(cmd, "@userId", DbType.Guid).Value = userId;
                        cmd.Parameters.Add(cmd, "@rating", DbType.Double).Value = userItemData.Rating;
                        cmd.Parameters.Add(cmd, "@played", DbType.Boolean).Value = userItemData.Played;
                        cmd.Parameters.Add(cmd, "@playCount", DbType.Int32).Value = userItemData.PlayCount;
                        cmd.Parameters.Add(cmd, "@isFavorite", DbType.Boolean).Value = userItemData.IsFavorite;
                        cmd.Parameters.Add(cmd, "@playbackPositionTicks", DbType.Int64).Value = userItemData.PlaybackPositionTicks;
                        cmd.Parameters.Add(cmd, "@lastPlayedDate", DbType.DateTime).Value = userItemData.LastPlayedDate;
                        cmd.Parameters.Add(cmd, "@AudioStreamIndex", DbType.Int32).Value = userItemData.AudioStreamIndex;
                        cmd.Parameters.Add(cmd, "@SubtitleStreamIndex", DbType.Int32).Value = userItemData.SubtitleStreamIndex;

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
                Logger.ErrorException("Failed to save user data:", e);

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
                cmd.CommandText = "select key,userid,rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate,AudioStreamIndex,SubtitleStreamIndex from userdata where key = @key and userId=@userId";

                cmd.Parameters.Add(cmd, "@key", DbType.String).Value = key;
                cmd.Parameters.Add(cmd, "@userId", DbType.Guid).Value = userId;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        return ReadRow(reader);
                    }
                }

                return null;
            }
        }

        public UserItemData GetUserData(Guid userId, List<string> keys)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }

            using (var cmd = _connection.CreateCommand())
            {
                var index = 0;
                var userdataKeys = new List<string>();
                var builder = new StringBuilder();
                foreach (var key in keys)
                {
                    var paramName = "@Key" + index;
                    userdataKeys.Add("Key =" + paramName);
                    cmd.Parameters.Add(cmd, paramName, DbType.String).Value = key;
                    builder.Append(" WHEN Key=" + paramName + " THEN " + index);
                    index++;
                    break;
                }

                var keyText = string.Join(" OR ", userdataKeys.ToArray());

                cmd.CommandText = "select key,userid,rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate,AudioStreamIndex,SubtitleStreamIndex from userdata where userId=@userId AND (" + keyText + ") ";

                cmd.CommandText += " ORDER BY (Case " + builder + " Else " + keys.Count.ToString(CultureInfo.InvariantCulture) + " End )";
                cmd.CommandText += " LIMIT 1";

                cmd.Parameters.Add(cmd, "@userId", DbType.Guid).Value = userId;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        return ReadRow(reader);
                    }
                }

                return null;
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
                cmd.CommandText = "select key,userid,rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate,AudioStreamIndex,SubtitleStreamIndex from userdata where userId=@userId";

                cmd.Parameters.Add(cmd, "@userId", DbType.Guid).Value = userId;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        yield return ReadRow(reader);
                    }
                }
            }
        }

        /// <summary>
        /// Read a row from the specified reader into the provided userData object
        /// </summary>
        /// <param name="reader"></param>
        private UserItemData ReadRow(IDataReader reader)
        {
            var userData = new UserItemData();

            userData.Key = reader.GetString(0);
            userData.UserId = reader.GetGuid(1);

            if (!reader.IsDBNull(2))
            {
                userData.Rating = reader.GetDouble(2);
            }

            userData.Played = reader.GetBoolean(3);
            userData.PlayCount = reader.GetInt32(4);
            userData.IsFavorite = reader.GetBoolean(5);
            userData.PlaybackPositionTicks = reader.GetInt64(6);

            if (!reader.IsDBNull(7))
            {
                userData.LastPlayedDate = reader.GetDateTime(7).ToUniversalTime();
            }

            if (!reader.IsDBNull(8))
            {
                userData.AudioStreamIndex = reader.GetInt32(8);
            }

            if (!reader.IsDBNull(9))
            {
                userData.SubtitleStreamIndex = reader.GetInt32(9);
            }

            return userData;
        }

        protected override void Dispose(bool dispose)
        {
            // handled by library database
        }

        protected override void CloseConnection()
        {
            // handled by library database
        }
    }
}