using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Data;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using SQLitePCL.pretty;

namespace Emby.Server.Implementations.Notifications
{
    public class SqliteNotificationsRepository : BaseSqliteRepository, INotificationsRepository
    {
        protected IFileSystem FileSystem { get; private set; }

        public SqliteNotificationsRepository(ILogger logger, IServerApplicationPaths appPaths, IFileSystem fileSystem) : base(logger)
        {
            FileSystem = fileSystem;
            DbFilePath = Path.Combine(appPaths.DataPath, "notifications.db");
        }

        public event EventHandler<NotificationUpdateEventArgs> NotificationAdded;
        public event EventHandler<NotificationReadEventArgs> NotificationsMarkedRead;
        ////public event EventHandler<NotificationUpdateEventArgs> NotificationUpdated;

        public void Initialize()
        {
            try
            {
                InitializeInternal();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error loading notifications database file. Will reset and retry.", ex);

                FileSystem.DeleteFile(DbFilePath);

                InitializeInternal();
            }
        }

        private void InitializeInternal()
        {
            using (var connection = CreateConnection())
            {
                RunDefaultInitialization(connection);

                string[] queries = {

                                "create table if not exists Notifications (Id GUID NOT NULL, UserId GUID NOT NULL, Date DATETIME NOT NULL, Name TEXT NOT NULL, Description TEXT NULL, Url TEXT NULL, Level TEXT NOT NULL, IsRead BOOLEAN NOT NULL, Category TEXT NOT NULL, RelatedId TEXT NULL, PRIMARY KEY (Id, UserId))",
                                "create index if not exists idx_Notifications1 on Notifications(Id)",
                                "create index if not exists idx_Notifications2 on Notifications(UserId)"
                               };

                connection.RunQueries(queries);
            }
        }

        /// <summary>
        /// Gets the notifications.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>NotificationResult.</returns>
        public NotificationResult GetNotifications(NotificationQuery query)
        {
            var result = new NotificationResult();

            var clauses = new List<string>();
            var paramList = new List<object>();

            if (query.IsRead.HasValue)
            {
                clauses.Add("IsRead=?");
                paramList.Add(query.IsRead.Value);
            }

            clauses.Add("UserId=?");
            paramList.Add(query.UserId.ToGuidBlob());

            var whereClause = " where " + string.Join(" And ", clauses.ToArray());

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    result.TotalRecordCount = connection.Query("select count(Id) from Notifications" + whereClause, paramList.ToArray()).SelectScalarInt().First();

                    var commandText = string.Format("select Id,UserId,Date,Name,Description,Url,Level,IsRead,Category,RelatedId from Notifications{0} order by IsRead asc, Date desc", whereClause);

                    if (query.Limit.HasValue || query.StartIndex.HasValue)
                    {
                        var offset = query.StartIndex ?? 0;

                        if (query.Limit.HasValue || offset > 0)
                        {
                            commandText += " LIMIT " + (query.Limit ?? int.MaxValue).ToString(CultureInfo.InvariantCulture);
                        }

                        if (offset > 0)
                        {
                            commandText += " OFFSET " + offset.ToString(CultureInfo.InvariantCulture);
                        }
                    }

                    var resultList = new List<Notification>();

                    foreach (var row in connection.Query(commandText, paramList.ToArray()))
                    {
                        resultList.Add(GetNotification(row));
                    }

                    result.Notifications = resultList.ToArray();
                }
            }

            return result;
        }

        public NotificationsSummary GetNotificationsSummary(string userId)
        {
            var result = new NotificationsSummary();

            using (WriteLock.Read())
            {
                using (var connection = CreateConnection(true))
                {
                    using (var statement = connection.PrepareStatement("select Level from Notifications where UserId=@UserId and IsRead=@IsRead"))
                    {
                        statement.TryBind("@IsRead", false);
                        statement.TryBind("@UserId", userId.ToGuidBlob());

                        var levels = new List<NotificationLevel>();

                        foreach (var row in statement.ExecuteQuery())
                        {
                            levels.Add(GetLevel(row, 0));
                        }

                        result.UnreadCount = levels.Count;

                        if (levels.Count > 0)
                        {
                            result.MaxUnreadNotificationLevel = levels.Max();
                        }
                    }

                    return result;
                }
            }
        }

        private Notification GetNotification(IReadOnlyList<IResultSetValue> reader)
        {
            var notification = new Notification
            {
                Id = reader[0].ReadGuidFromBlob().ToString("N"),
                UserId = reader[1].ReadGuidFromBlob().ToString("N"),
                Date = reader[2].ReadDateTime(),
                Name = reader[3].ToString()
            };

            if (reader[4].SQLiteType != SQLiteType.Null)
            {
                notification.Description = reader[4].ToString();
            }

            if (reader[5].SQLiteType != SQLiteType.Null)
            {
                notification.Url = reader[5].ToString();
            }

            notification.Level = GetLevel(reader, 6);
            notification.IsRead = reader[7].ToBool();

            return notification;
        }

        /// <summary>
        /// Gets the level.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="index">The index.</param>
        /// <returns>NotificationLevel.</returns>
        private NotificationLevel GetLevel(IReadOnlyList<IResultSetValue> reader, int index)
        {
            NotificationLevel level;

            var val = reader[index].ToString();

            Enum.TryParse(val, true, out level);

            return level;
        }

        /// <summary>
        /// Adds the notification.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task AddNotification(Notification notification, CancellationToken cancellationToken)
        {
            await ReplaceNotification(notification, cancellationToken).ConfigureAwait(false);

            if (NotificationAdded != null)
            {
                try
                {
                    NotificationAdded(this, new NotificationUpdateEventArgs
                    {
                        Notification = notification
                    });
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error in NotificationAdded event handler", ex);
                }
            }
        }

        /// <summary>
        /// Replaces the notification.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task ReplaceNotification(Notification notification, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(notification.Id))
            {
                notification.Id = Guid.NewGuid().ToString("N");
            }
            if (string.IsNullOrEmpty(notification.UserId))
            {
                throw new ArgumentException("The notification must have a user id");
            }

            cancellationToken.ThrowIfCancellationRequested();

            lock (WriteLock)
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(conn =>
                    {
                        using (var statement = conn.PrepareStatement("replace into Notifications (Id, UserId, Date, Name, Description, Url, Level, IsRead, Category, RelatedId) values (@Id, @UserId, @Date, @Name, @Description, @Url, @Level, @IsRead, @Category, @RelatedId)"))
                        {
                            statement.TryBind("@Id", notification.Id.ToGuidBlob());
                            statement.TryBind("@UserId", notification.UserId.ToGuidBlob());
                            statement.TryBind("@Date", notification.Date.ToDateTimeParamValue());
                            statement.TryBind("@Name", notification.Name);
                            statement.TryBind("@Description", notification.Description);
                            statement.TryBind("@Url", notification.Url);
                            statement.TryBind("@Level", notification.Level.ToString());
                            statement.TryBind("@IsRead", notification.IsRead);
                            statement.TryBind("@Category", string.Empty);
                            statement.TryBind("@RelatedId", string.Empty);

                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
        }

        /// <summary>
        /// Marks the read.
        /// </summary>
        /// <param name="notificationIdList">The notification id list.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="isRead">if set to <c>true</c> [is read].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task MarkRead(IEnumerable<string> notificationIdList, string userId, bool isRead, CancellationToken cancellationToken)
        {
            var list = notificationIdList.ToList();
            var idArray = list.Select(i => new Guid(i)).ToArray();

            await MarkReadInternal(idArray, userId, isRead, cancellationToken).ConfigureAwait(false);

            if (NotificationsMarkedRead != null)
            {
                try
                {
                    NotificationsMarkedRead(this, new NotificationReadEventArgs
                    {
                        IdList = list.ToArray(),
                        IsRead = isRead,
                        UserId = userId
                    });
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error in NotificationsMarkedRead event handler", ex);
                }
            }
        }

        public async Task MarkAllRead(string userId, bool isRead, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(conn =>
                    {
                        using (var statement = conn.PrepareStatement("update Notifications set IsRead=@IsRead where UserId=@UserId"))
                        {
                            statement.TryBind("@IsRead", isRead);
                            statement.TryBind("@UserId", userId.ToGuidBlob());

                            statement.MoveNext();
                        }
                    }, TransactionMode);
                }
            }
        }

        private async Task MarkReadInternal(IEnumerable<Guid> notificationIdList, string userId, bool isRead, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (WriteLock.Write())
            {
                using (var connection = CreateConnection())
                {
                    connection.RunInTransaction(conn =>
                    {
                        using (var statement = conn.PrepareStatement("update Notifications set IsRead=@IsRead where UserId=@UserId and Id=@Id"))
                        {
                            statement.TryBind("@IsRead", isRead);
                            statement.TryBind("@UserId", userId.ToGuidBlob());

                            foreach (var id in notificationIdList)
                            {
                                statement.Reset();

                                statement.TryBind("@Id", id.ToGuidBlob());

                                statement.MoveNext();
                            }
                        }

                    }, TransactionMode);
                }
            }
        }
    }
}
