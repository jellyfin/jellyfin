using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public static class UserDataMigration
    {
        /// <summary>
        /// Migrates the specified old file.
        /// </summary>
        /// <param name="oldFile">The old file.</param>
        /// <param name="newDatabase">The new database.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="json">The json.</param>
        /// <returns>Task.</returns>
        public static async Task Migrate(string oldFile, IDbConnection newDatabase, ILogger logger, IJsonSerializer json)
        {
            var oldDb = await SqliteExtensions.ConnectToDb(oldFile).ConfigureAwait(false);

            using (oldDb)
            {
                IDbTransaction transaction = null;

                var data = GetAllUserData(oldDb, json).ToList();

                try
                {
                    transaction = newDatabase.BeginTransaction();

                    foreach (var userdata in data)
                    {
                        PersistUserData(userdata, newDatabase, transaction);
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
                    logger.ErrorException("Failed to save user data:", e);

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

            var backupFile = Path.Combine(Path.GetDirectoryName(oldFile), "userdata_v1.db.bak");

            if (File.Exists(backupFile))
            {
                File.Delete(backupFile);
            }

            File.Move(oldFile, backupFile);
        }

        /// <summary>
        /// Gets all user data.
        /// </summary>
        /// <param name="oldDatabase">The old database.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <returns>IEnumerable{UserItemData}.</returns>
        private static IEnumerable<UserItemData> GetAllUserData(IDbConnection oldDatabase, IJsonSerializer jsonSerializer)
        {
            using (var cmd = oldDatabase.CreateCommand())
            {
                cmd.CommandText = "select userId,key,data from userdata";

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                {
                    while (reader.Read())
                    {
                        var userId = reader.GetGuid(0);
                        var key = reader.GetString(1);

                        using (var stream = reader.GetMemoryStream(2))
                        {
                            var userData = jsonSerializer.DeserializeFromStream<UserItemData>(stream);

                            userData.UserId = userId;
                            userData.Key = key;

                            yield return userData;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Persists the user data.
        /// </summary>
        /// <param name="userData">The user data.</param>
        /// <param name="database">The database.</param>
        /// <param name="transaction">The transaction.</param>
        private static void PersistUserData(UserItemData userData, IDbConnection database, IDbTransaction transaction)
        {
            using (var cmd = database.CreateCommand())
            {
                cmd.CommandText = "replace into userdata (key, userId, rating,played,playCount,isFavorite,playbackPositionTicks,lastPlayedDate) values (@key, @userId, @rating,@played,@playCount,@isFavorite,@playbackPositionTicks,@lastPlayedDate)";

                cmd.Parameters.Add(cmd, "@key", DbType.String).Value = userData.Key;
                cmd.Parameters.Add(cmd, "@userId", DbType.Guid).Value = userData.UserId;
                cmd.Parameters.Add(cmd, "@rating", DbType.Double).Value = userData.Rating;
                cmd.Parameters.Add(cmd, "@played", DbType.Boolean).Value = userData.Played;
                cmd.Parameters.Add(cmd, "@playCount", DbType.Int32).Value = userData.PlayCount;
                cmd.Parameters.Add(cmd, "@isFavorite", DbType.Boolean).Value = userData.IsFavorite;
                cmd.Parameters.Add(cmd, "@playbackPositionTicks", DbType.Int64).Value = userData.PlaybackPositionTicks;
                cmd.Parameters.Add(cmd, "@lastPlayedDate", DbType.DateTime).Value = userData.LastPlayedDate;

                cmd.Transaction = transaction;

                cmd.ExecuteNonQuery();
            }
        }

    }
}
