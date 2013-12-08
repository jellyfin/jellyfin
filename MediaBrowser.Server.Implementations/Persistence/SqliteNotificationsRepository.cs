using System.IO;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Notifications;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class SqliteNotificationsRepository : INotificationsRepository
    {
        private  IDbConnection _connection;
        private readonly ILogger _logger;
        private readonly IServerApplicationPaths _appPaths;

        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        public SqliteNotificationsRepository(ILogManager logManager, IServerApplicationPaths appPaths)
        {
            _appPaths = appPaths;
            _logger = logManager.GetLogger(GetType().Name);
        }

        public event EventHandler<NotificationUpdateEventArgs> NotificationAdded;
        public event EventHandler<NotificationReadEventArgs> NotificationsMarkedRead;
        public event EventHandler<NotificationUpdateEventArgs> NotificationUpdated;

        private IDbCommand _replaceNotificationCommand;
        private IDbCommand _markReadCommand;

        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "notifications.db");

            _connection = await SqliteExtensions.ConnectToDb(dbFile, _logger).ConfigureAwait(false);
            
            string[] queries = {

                                "create table if not exists Notifications (Id GUID NOT NULL, UserId GUID NOT NULL, Date DATETIME NOT NULL, Name TEXT NOT NULL, Description TEXT, Url TEXT, Level TEXT NOT NULL, IsRead BOOLEAN NOT NULL, Category TEXT NOT NULL, RelatedId TEXT, PRIMARY KEY (Id, UserId))",
                                "create index if not exists idx_Notifications on Notifications(Id, UserId)",

                                //pragmas
                                "pragma temp_store = memory",

                                "pragma shrink_memory"
                               };

            _connection.RunQueries(queries, _logger);

            PrepareStatements();
        }

        private void PrepareStatements()
        {
            _replaceNotificationCommand = _connection.CreateCommand();
            _replaceNotificationCommand.CommandText = "replace into Notifications (Id, UserId, Date, Name, Description, Url, Level, IsRead, Category, RelatedId) values (@Id, @UserId, @Date, @Name, @Description, @Url, @Level, @IsRead, @Category, @RelatedId)";

            _replaceNotificationCommand.Parameters.Add(_replaceNotificationCommand, "@Id");
            _replaceNotificationCommand.Parameters.Add(_replaceNotificationCommand, "@UserId");
            _replaceNotificationCommand.Parameters.Add(_replaceNotificationCommand, "@Date");
            _replaceNotificationCommand.Parameters.Add(_replaceNotificationCommand, "@Name");
            _replaceNotificationCommand.Parameters.Add(_replaceNotificationCommand, "@Description");
            _replaceNotificationCommand.Parameters.Add(_replaceNotificationCommand, "@Url");
            _replaceNotificationCommand.Parameters.Add(_replaceNotificationCommand, "@Level");
            _replaceNotificationCommand.Parameters.Add(_replaceNotificationCommand, "@IsRead");
            _replaceNotificationCommand.Parameters.Add(_replaceNotificationCommand, "@Category");
            _replaceNotificationCommand.Parameters.Add(_replaceNotificationCommand, "@RelatedId");

            _markReadCommand = _connection.CreateCommand();
            _markReadCommand.CommandText = "update Notifications set IsRead=@IsRead where Id=@Id and UserId=@UserId";

            _markReadCommand.Parameters.Add(_replaceNotificationCommand, "@UserId");
            _markReadCommand.Parameters.Add(_replaceNotificationCommand, "@IsRead");
            _markReadCommand.Parameters.Add(_replaceNotificationCommand, "@Id");
        }

        /// <summary>
        /// Gets the notifications.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>NotificationResult.</returns>
        public NotificationResult GetNotifications(NotificationQuery query)
        {
            var result = new NotificationResult();

            using (var cmd = _connection.CreateCommand())
            {
                var clauses = new List<string>();

                if (query.IsRead.HasValue)
                {
                    clauses.Add("IsRead=@IsRead");
                    cmd.Parameters.Add(cmd, "@IsRead", DbType.Boolean).Value = query.IsRead.Value;
                }

                clauses.Add("UserId=@UserId");
                cmd.Parameters.Add(cmd, "@UserId", DbType.Guid).Value = new Guid(query.UserId);

                var whereClause = " where " + string.Join(" And ", clauses.ToArray());

                cmd.CommandText = string.Format("select count(Id) from Notifications{0};select Id,UserId,Date,Name,Description,Url,Level,IsRead,Category,RelatedId from Notifications{0} order by IsRead asc, Date desc", whereClause);

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    if (reader.Read())
                    {
                        result.TotalRecordCount = reader.GetInt32(0);
                    }

                    if (reader.NextResult())
                    {
                        var notifications = GetNotifications(reader);

                        if (query.StartIndex.HasValue)
                        {
                            notifications = notifications.Skip(query.StartIndex.Value);
                        }

                        if (query.Limit.HasValue)
                        {
                            notifications = notifications.Take(query.Limit.Value);
                        }

                        result.Notifications = notifications.ToArray();
                    }
                }

                return result;
            }
        }

        public NotificationsSummary GetNotificationsSummary(Guid userId)
        {
            var result = new NotificationsSummary();

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select Level from Notifications where UserId=@UserId and IsRead=@IsRead";

                cmd.Parameters.Add(cmd, "@UserId", DbType.Guid).Value = userId;
                cmd.Parameters.Add(cmd, "@IsRead", DbType.Boolean).Value = false;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                {
                    var levels = new List<NotificationLevel>();

                    while (reader.Read())
                    {
                        levels.Add(GetLevel(reader, 0));
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

        /// <summary>
        /// Gets the notifications.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>IEnumerable{Notification}.</returns>
        private IEnumerable<Notification> GetNotifications(IDataReader reader)
        {
            while (reader.Read())
            {
                yield return GetNotification(reader);
            }
        }

        private Notification GetNotification(IDataReader reader)
        {
            var notification = new Notification
            {
                Id = reader.GetGuid(0),
                UserId = reader.GetGuid(1),
                Date = reader.GetDateTime(2).ToUniversalTime(),
                Name = reader.GetString(3)
            };

            if (!reader.IsDBNull(4))
            {
                notification.Description = reader.GetString(4);
            }

            if (!reader.IsDBNull(5))
            {
                notification.Url = reader.GetString(5);
            }

            notification.Level = GetLevel(reader, 6);
            notification.IsRead = reader.GetBoolean(7);

            notification.Category = reader.GetString(8);

            if (!reader.IsDBNull(9))
            {
                notification.RelatedId = reader.GetString(9);
            }

            return notification;
        }

        /// <summary>
        /// Gets the notification.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="userId">The user id.</param>
        /// <returns>Notification.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// id
        /// or
        /// userId
        /// </exception>
        public Notification GetNotification(Guid id, Guid userId)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = "select Id,UserId,Date,Name,Description,Url,Level,IsRead,Category,RelatedId where Id=@Id And UserId = @UserId";

                cmd.Parameters.Add(cmd, "@Id", DbType.Guid).Value = id;
                cmd.Parameters.Add(cmd, "@UserId", DbType.Guid).Value = userId;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        return GetNotification(reader);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the level.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="index">The index.</param>
        /// <returns>NotificationLevel.</returns>
        private NotificationLevel GetLevel(IDataReader reader, int index)
        {
            NotificationLevel level;

            var val = reader.GetString(index);

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
                    _logger.ErrorException("Error in NotificationAdded event handler", ex);
                }
            }
        }

        /// <summary>
        /// Updates the notification.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task UpdateNotification(Notification notification, CancellationToken cancellationToken)
        {
            await ReplaceNotification(notification, cancellationToken).ConfigureAwait(false);

            if (NotificationUpdated != null)
            {
                try
                {
                    NotificationUpdated(this, new NotificationUpdateEventArgs
                    {
                        Notification = notification
                    });
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in NotificationUpdated event handler", ex);
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
            if (notification.Id == Guid.Empty)
            {
                throw new ArgumentException("The notification must have an id");
            }
            if (notification.UserId == Guid.Empty)
            {
                throw new ArgumentException("The notification must have a user id");
            }

            cancellationToken.ThrowIfCancellationRequested();

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                transaction = _connection.BeginTransaction();

                _replaceNotificationCommand.GetParameter(0).Value = notification.Id;
                _replaceNotificationCommand.GetParameter(1).Value = notification.UserId;
                _replaceNotificationCommand.GetParameter(2).Value = notification.Date.ToUniversalTime();
                _replaceNotificationCommand.GetParameter(3).Value = notification.Name;
                _replaceNotificationCommand.GetParameter(4).Value = notification.Description;
                _replaceNotificationCommand.GetParameter(5).Value = notification.Url;
                _replaceNotificationCommand.GetParameter(6).Value = notification.Level.ToString();
                _replaceNotificationCommand.GetParameter(7).Value = notification.IsRead;
                _replaceNotificationCommand.GetParameter(8).Value = notification.Category;
                _replaceNotificationCommand.GetParameter(9).Value = notification.RelatedId;

                _replaceNotificationCommand.Transaction = transaction;

                _replaceNotificationCommand.ExecuteNonQuery();

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
                _logger.ErrorException("Failed to save notification:", e);

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
        /// Marks the read.
        /// </summary>
        /// <param name="notificationIdList">The notification id list.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="isRead">if set to <c>true</c> [is read].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task MarkRead(IEnumerable<Guid> notificationIdList, Guid userId, bool isRead, CancellationToken cancellationToken)
        {
            var idArray = notificationIdList.ToArray();

            await MarkReadInternal(idArray, userId, isRead, cancellationToken).ConfigureAwait(false);

            if (NotificationsMarkedRead != null)
            {
                try
                {
                    NotificationsMarkedRead(this, new NotificationReadEventArgs
                    {
                        IdList = idArray.ToArray(),
                        IsRead = isRead,
                        UserId = userId
                    });
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error in NotificationsMarkedRead event handler", ex);
                }
            }
        }

        private async Task MarkReadInternal(IEnumerable<Guid> notificationIdList, Guid userId, bool isRead, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            IDbTransaction transaction = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                transaction = _connection.BeginTransaction();

                _markReadCommand.GetParameter(0).Value = userId;
                _markReadCommand.GetParameter(1).Value = isRead;

                foreach (var id in notificationIdList)
                {
                    _markReadCommand.GetParameter(2).Value = id;

                    _markReadCommand.Transaction = transaction;

                    _markReadCommand.ExecuteNonQuery();
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
                _logger.ErrorException("Failed to save notification:", e);

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
    }
}
