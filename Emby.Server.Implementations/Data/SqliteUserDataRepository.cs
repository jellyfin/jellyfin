using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;
using MediaBrowser.Controller.Library;

namespace Emby.Server.Implementations.Data
{
    public class SqliteUserDataRepository : BaseSqliteRepository, IUserDataRepository
    {
        private readonly IFileSystem _fileSystem;

        public SqliteUserDataRepository(ILogger logger, IApplicationPaths appPaths, IFileSystem fileSystem)
            : base(logger)
        {
            _fileSystem = fileSystem;
            DbFilePath = Path.Combine(appPaths.DataPath, "library.db");
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
        public void Initialize(ReaderWriterLockSlim writeLock, ManagedConnection managedConnection, IUserManager userManager)
        {
            _connection = managedConnection;

            WriteLock.Dispose();
            WriteLock = writeLock;

            using (var connection = CreateConnection())
            {
                var userDatasTableExists = TableExists(connection, "UserDatas");
                var userDataTableExists = TableExists(connection, "userdata");

                var users = userDatasTableExists ? null : userManager.Users.ToArray();

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

        private void ImportUserIds(IDatabaseConnection db, User[] users)
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

                    statement.TryBind("@UserId", user.Id.ToGuidBlob());
                    statement.TryBind("@InternalUserId", user.InternalId);

                    statement.MoveNext();
                    statement.Reset();
                }
            }
        }

        private List<Guid> GetAllUserIdsWithUserData(IDatabaseConnection db)
        {
            List<Guid> list = new List<Guid>();

            using (var statement = PrepareStatement(db, "select DISTINCT UserId from UserData where UserId not null"))
            {
                foreach (var row in statement.ExecuteQuery())
                {
                    try
                    {
                        list.Add(row[0].ReadGuidFromBlob());
                    }
                    catch
                    {

                    }
                }
            }

            return list;
        }

        protected override bool EnableTempStoreMemory
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Saves the user data.
        /// </summary>
        public void SaveUserData(long internalUserId, string key, UserItemData userData, CancellationToken cancellationToken)
        {
            if (userData == null)
            {
                throw new ArgumentNullException("userData");
            }
            if (internalUserId <= 0)
            {
                throw new ArgumentNullException("internalUserId");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            PersistUserData(internalUserId, key, userData, cancellationToken);
        }

        public void SaveAllUserData(long internalUserId, UserItemData[] userData, CancellationToken cancellationToken)
        {
            if (userData == null)
            {
                throw new ArgumentNullException("userData");
            }
            if (internalUserId <= 0)
            {
                throw new ArgumentNullException("internalUserId");
            }

            PersistAllUserData(internalUserId, userData, cancellationToken);
        }

        /// <summary>
        /// Persists the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public void PersistUserData(long internalUserId, string key, UserItemData userData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(db =>
                    {
                        SaveUserData(db, internalUserId, key, userData);
                    }, TransactionMode);
                }
            }
        }

        private void SaveUserData(IDatabaseConnection db, long internalUserId, string key, UserItemData userData)
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

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
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
        public UserItemData GetUserData(long internalUserId, string key)
        {
            if (internalUserId <= 0)
            {
                throw new ArgumentNullException("internalUserId");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
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
        }

        public UserItemData GetUserData(long internalUserId, List<string> keys)
        {
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
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
        /// <param name="userId"></param>
        /// <returns></returns>
        public List<UserItemData> GetAllUserData(long internalUserId)
        {
            if (internalUserId <= 0)
            {
                throw new ArgumentNullException("internalUserId");
            }

            var list = new List<UserItemData>();

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection())
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
