#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Data
{
    public class SqliteUserDataRepository : BaseSqliteRepository, IUserDataRepository
    {
        public SqliteUserDataRepository(
            ILogger<SqliteUserDataRepository> logger,
            IApplicationPaths appPaths)
            : base(logger)
        {
            DbFilePath = Path.Combine(appPaths.DataPath, "library.db");
        }

        /// <inheritdoc />
        public string Name => "SQLite";

        /// <summary>
        /// Opens the connection to the database.
        /// </summary>
        public void Initialize(IUserManager userManager, SemaphoreSlim dbLock, SQLiteDatabaseConnection dbConnection)
        {
            WriteLock.Dispose();
            WriteLock = dbLock;
            WriteConnection?.Dispose();
            WriteConnection = dbConnection;

            using (var connection = GetConnection())
            {
                var userDatasTableExists = TableExists(connection, "UserDatas");
                var userDataTableExists = TableExists(connection, "userdata");

                var users = userDatasTableExists ? null : userManager.Users;

                connection.RunInTransaction(db =>
                {
                    db.ExecuteAll(string.Join(";", new[] {

                        "create table if not exists UserDatas (key nvarchar not null, userId INT not null, rating float null, played bit not null, playCount int not null, isFavorite bit not null, playbackPositionTicks bigint not null, lastPlayedDate datetime null, AudioStreamIndex INT, SubtitleStreamIndex INT)",

                        "drop index if exists idx_userdata",
                        "drop index if exists idx_userdata1",
                        "drop index if exists idx_userdata2",
                        "drop index if exists userdataindex1",
                        "drop index if exists userdataindex",
                        "drop index if exists userdataindex3",
                        "drop index if exists userdataindex4",
                        "create unique index if not exists UserDatasIndex1 on UserDatas (key, userId)",
                        "create index if not exists UserDatasIndex2 on UserDatas (key, userId, played)",
                        "create index if not exists UserDatasIndex3 on UserDatas (key, userId, playbackPositionTicks)",
                        "create index if not exists UserDatasIndex4 on UserDatas (key, userId, isFavorite)"
                    }));

                    if (userDataTableExists)
                    {
                        var existingColumnNames = GetColumnNames(db, "userdata");

                        AddColumn(db, "userdata", "InternalUserId", "int", existingColumnNames);
                        AddColumn(db, "userdata", "AudioStreamIndex", "int", existingColumnNames);
                        AddColumn(db, "userdata", "SubtitleStreamIndex", "int", existingColumnNames);

                        if (!userDatasTableExists)
                        {
                            ImportUserIds(db, users);

                            db.ExecuteAll("INSERT INTO UserDatas (key, userId, rating, played, playCount, isFavorite, playbackPositionTicks, lastPlayedDate, AudioStreamIndex, SubtitleStreamIndex) SELECT key, InternalUserId, rating, played, playCount, isFavorite, playbackPositionTicks, lastPlayedDate, AudioStreamIndex, SubtitleStreamIndex from userdata where InternalUserId not null");
                        }
                    }
                }, TransactionMode);
            }
        }

        private void ImportUserIds(IDatabaseConnection db, IEnumerable<User> users)
        {
            var userIdsWithUserData = GetAllUserIdsWithUserData(db);

            using (var statement = db.PrepareStatement("update userdata set InternalUserId=@InternalUserId where UserId=@UserId"))
            {
                foreach (var user in users)
                {
                    if (!userIdsWithUserData.Contains(user.Id))
                    {
                        continue;
                    }

                    statement.TryBind("@UserId", user.Id.ToByteArray());
                    statement.TryBind("@InternalUserId", user.InternalId);

                    statement.MoveNext();
                    statement.Reset();
                }
            }
        }

        private List<Guid> GetAllUserIdsWithUserData(IDatabaseConnection db)
        {
            var list = new List<Guid>();

            using (var statement = PrepareStatement(db, "select DISTINCT UserId from UserData where UserId not null"))
            {
                foreach (var row in statement.ExecuteQuery())
                {
                    try
                    {
                        list.Add(row[0].ReadGuidFromBlob());
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error while getting user");
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Saves the user data.
        /// </summary>
        public void SaveUserData(long internalUserId, string key, UserItemData userData, CancellationToken cancellationToken)
        {
            if (userData == null)
            {
                throw new ArgumentNullException(nameof(userData));
            }
            if (internalUserId <= 0)
            {
                throw new ArgumentNullException(nameof(internalUserId));
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            PersistUserData(internalUserId, key, userData, cancellationToken);
        }

        public void SaveAllUserData(long internalUserId, UserItemData[] userData, CancellationToken cancellationToken)
        {
            if (userData == null)
            {
                throw new ArgumentNullException(nameof(userData));
            }
            if (internalUserId <= 0)
            {
                throw new ArgumentNullException(nameof(internalUserId));
            }

            PersistAllUserData(internalUserId, userData, cancellationToken);
        }

        /// <summary>
        /// Persists the user data.
        /// </summary>
        /// <param name="internalUserId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void PersistUserData(long internalUserId, string key, UserItemData userData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    SaveUserData(db, internalUserId, key, userData);
                }, TransactionMode);
            }
        }

        private static void SaveUserData(IDatabaseConnection db, long internalUserId, string key, UserItemData userData)
        {
            using (var statement = db.PrepareStatement("replace into UserDatas (key, userId, rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate,AudioStreamIndex,SubtitleStreamIndex) values (@key, @userId, @rating,@played,@playCount,@isFavorite,@playbackPositionTicks,@lastPlayedDate,@AudioStreamIndex,@SubtitleStreamIndex)"))
            {
                statement.TryBind("@userId", internalUserId);
                statement.TryBind("@key", key);

                if (userData.Rating.HasValue)
                {
                    statement.TryBind("@rating", userData.Rating.Value);
                }
                else
                {
                    statement.TryBindNull("@rating");
                }

                statement.TryBind("@played", userData.Played);
                statement.TryBind("@playCount", userData.PlayCount);
                statement.TryBind("@isFavorite", userData.IsFavorite);
                statement.TryBind("@playbackPositionTicks", userData.PlaybackPositionTicks);

                if (userData.LastPlayedDate.HasValue)
                {
                    statement.TryBind("@lastPlayedDate", userData.LastPlayedDate.Value.ToDateTimeParamValue());
                }
                else
                {
                    statement.TryBindNull("@lastPlayedDate");
                }

                if (userData.AudioStreamIndex.HasValue)
                {
                    statement.TryBind("@AudioStreamIndex", userData.AudioStreamIndex.Value);
                }
                else
                {
                    statement.TryBindNull("@AudioStreamIndex");
                }

                if (userData.SubtitleStreamIndex.HasValue)
                {
                    statement.TryBind("@SubtitleStreamIndex", userData.SubtitleStreamIndex.Value);
                }
                else
                {
                    statement.TryBindNull("@SubtitleStreamIndex");
                }

                statement.MoveNext();
            }
        }

        /// <summary>
        /// Persist all user data for the specified user
        /// </summary>
        private void PersistAllUserData(long internalUserId, UserItemData[] userDataList, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(db =>
                {
                    foreach (var userItemData in userDataList)
                    {
                        SaveUserData(db, internalUserId, userItemData.Key, userItemData);
                    }
                }, TransactionMode);
            }
        }

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="internalUserId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>Task{UserItemData}.</returns>
        /// <exception cref="ArgumentNullException">
        /// userId
        /// or
        /// key
        /// </exception>
        public UserItemData GetUserData(long internalUserId, string key)
        {
            if (internalUserId <= 0)
            {
                throw new ArgumentNullException(nameof(internalUserId));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            using (var connection = GetConnection(true))
            {
                using (var statement = connection.PrepareStatement("select key,userid,rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate,AudioStreamIndex,SubtitleStreamIndex from UserDatas where key =@Key and userId=@UserId"))
                {
                    statement.TryBind("@UserId", internalUserId);
                    statement.TryBind("@Key", key);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        return ReadRow(row);
                    }
                }

                return null;
            }
        }

        public UserItemData GetUserData(long internalUserId, List<string> keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (keys.Count == 0)
            {
                return null;
            }

            return GetUserData(internalUserId, keys[0]);
        }

        /// <summary>
        /// Return all user-data associated with the given user
        /// </summary>
        /// <param name="internalUserId"></param>
        /// <returns></returns>
        public List<UserItemData> GetAllUserData(long internalUserId)
        {
            if (internalUserId <= 0)
            {
                throw new ArgumentNullException(nameof(internalUserId));
            }

            var list = new List<UserItemData>();

            using (var connection = GetConnection())
            {
                using (var statement = connection.PrepareStatement("select key,userid,rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate,AudioStreamIndex,SubtitleStreamIndex from UserDatas where userId=@UserId"))
                {
                    statement.TryBind("@UserId", internalUserId);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        list.Add(ReadRow(row));
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Read a row from the specified reader into the provided userData object
        /// </summary>
        /// <param name="reader"></param>
        private UserItemData ReadRow(IReadOnlyList<IResultSetValue> reader)
        {
            var userData = new UserItemData();

            userData.Key = reader[0].ToString();
            //userData.UserId = reader[1].ReadGuidFromBlob();

            if (reader[2].SQLiteType != SQLiteType.Null)
            {
                userData.Rating = reader[2].ToDouble();
            }

            userData.Played = reader[3].ToBool();
            userData.PlayCount = reader[4].ToInt();
            userData.IsFavorite = reader[5].ToBool();
            userData.PlaybackPositionTicks = reader[6].ToInt64();

            if (reader[7].SQLiteType != SQLiteType.Null)
            {
                userData.LastPlayedDate = reader[7].TryReadDateTime();
            }

            if (reader[8].SQLiteType != SQLiteType.Null)
            {
                userData.AudioStreamIndex = reader[8].ToInt();
            }

            if (reader[9].SQLiteType != SQLiteType.Null)
            {
                userData.SubtitleStreamIndex = reader[9].ToInt();
            }

            return userData;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// There is nothing to dispose here since <see cref="BaseSqliteRepository.WriteLock"/> and
        /// <see cref="BaseSqliteRepository.WriteConnection"/> are managed by <see cref="SqliteItemRepository"/>.
        /// See <see cref="Initialize(IUserManager, SemaphoreSlim, SQLiteDatabaseConnection)"/>.
        /// </remarks>
        protected override void Dispose(bool dispose)
        {
        }
    }
}
