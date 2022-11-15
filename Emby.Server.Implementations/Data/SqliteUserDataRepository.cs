#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Jellyfin.Data.Entities;
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

        /// <summary>
        /// Opens the connection to the database.
        /// </summary>
        /// <param name="userManager">The user manager.</param>
        /// <param name="dbLock">The lock to use for database IO.</param>
        /// <param name="dbConnection">The connection to use for database IO.</param>
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

                connection.RunInTransaction(
                    db =>
                    {
                        db.ExecuteAll(string.Join(';', new[]
                        {
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
                    },
                    TransactionMode);
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

                    statement.TryBind("@UserId", user.Id);
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

        /// <inheritdoc />
        public void SaveUserData(long userId, string key, UserItemData userData, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(userData);

            if (userId <= 0)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            PersistUserData(userId, key, userData, cancellationToken);
        }

        /// <inheritdoc />
        public void SaveAllUserData(long userId, UserItemData[] userData, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(userData);

            if (userId <= 0)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            PersistAllUserData(userId, userData, cancellationToken);
        }

        /// <summary>
        /// Persists the user data.
        /// </summary>
        /// <param name="internalUserId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public void PersistUserData(long internalUserId, string key, UserItemData userData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(
                    db =>
                    {
                        SaveUserData(db, internalUserId, key, userData);
                    },
                    TransactionMode);
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
        /// Persist all user data for the specified user.
        /// </summary>
        private void PersistAllUserData(long internalUserId, UserItemData[] userDataList, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = GetConnection())
            {
                connection.RunInTransaction(
                    db =>
                    {
                        foreach (var userItemData in userDataList)
                        {
                            SaveUserData(db, internalUserId, userItemData.Key, userItemData);
                        }
                    },
                    TransactionMode);
            }
        }

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>Task{UserItemData}.</returns>
        /// <exception cref="ArgumentNullException">
        /// userId
        /// or
        /// key.
        /// </exception>
        public UserItemData GetUserData(long userId, string key)
        {
            if (userId <= 0)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            using (var connection = GetConnection(true))
            {
                using (var statement = connection.PrepareStatement("select key,userid,rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate,AudioStreamIndex,SubtitleStreamIndex from UserDatas where key =@Key and userId=@UserId"))
                {
                    statement.TryBind("@UserId", userId);
                    statement.TryBind("@Key", key);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        return ReadRow(row);
                    }
                }

                return null;
            }
        }

        public UserItemData GetUserData(long userId, List<string> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);

            if (keys.Count == 0)
            {
                return null;
            }

            return GetUserData(userId, keys[0]);
        }

        /// <summary>
        /// Return all user-data associated with the given user.
        /// </summary>
        /// <param name="userId">The internal user id.</param>
        /// <returns>The list of user item data.</returns>
        public List<UserItemData> GetAllUserData(long userId)
        {
            if (userId <= 0)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            var list = new List<UserItemData>();

            using (var connection = GetConnection())
            {
                using (var statement = connection.PrepareStatement("select key,userid,rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate,AudioStreamIndex,SubtitleStreamIndex from UserDatas where userId=@UserId"))
                {
                    statement.TryBind("@UserId", userId);

                    foreach (var row in statement.ExecuteQuery())
                    {
                        list.Add(ReadRow(row));
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Read a row from the specified reader into the provided userData object.
        /// </summary>
        /// <param name="reader">The list of result set values.</param>
        /// <returns>The user item data.</returns>
        private UserItemData ReadRow(IReadOnlyList<ResultSetValue> reader)
        {
            var userData = new UserItemData();

            userData.Key = reader[0].ToString();
            // userData.UserId = reader[1].ReadGuidFromBlob();

            if (reader.TryGetDouble(2, out var rating))
            {
                userData.Rating = rating;
            }

            userData.Played = reader[3].ToBool();
            userData.PlayCount = reader[4].ToInt();
            userData.IsFavorite = reader[5].ToBool();
            userData.PlaybackPositionTicks = reader[6].ToInt64();

            if (reader.TryReadDateTime(7, out var lastPlayedDate))
            {
                userData.LastPlayedDate = lastPlayedDate;
            }

            if (reader.TryGetInt32(8, out var audioStreamIndex))
            {
                userData.AudioStreamIndex = audioStreamIndex;
            }

            if (reader.TryGetInt32(9, out var subtitleStreamIndex))
            {
                userData.SubtitleStreamIndex = subtitleStreamIndex;
            }

            return userData;
        }

#pragma warning disable CA2215
        /// <inheritdoc/>
        /// <remarks>
        /// There is nothing to dispose here since <see cref="BaseSqliteRepository.WriteLock"/> and
        /// <see cref="BaseSqliteRepository.WriteConnection"/> are managed by <see cref="SqliteItemRepository"/>.
        /// See <see cref="Initialize(IUserManager, SemaphoreSlim, SQLiteDatabaseConnection)"/>.
        /// </remarks>
        protected override void Dispose(bool dispose)
        {
            // The write lock and connection for the item repository are shared with the user data repository
            // since they point to the same database. The item repo has responsibility for disposing these two objects,
            // so the user data repo should not attempt to dispose them as well
        }
#pragma warning restore CA2215
    }
}
