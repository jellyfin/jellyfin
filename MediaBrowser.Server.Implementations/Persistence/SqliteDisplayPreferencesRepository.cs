using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
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
    /// Class SQLiteDisplayPreferencesRepository
    /// </summary>
    public class SqliteDisplayPreferencesRepository : BaseSqliteRepository, IDisplayPreferencesRepository
    {
        public SqliteDisplayPreferencesRepository(ILogManager logManager, IJsonSerializer jsonSerializer, IApplicationPaths appPaths, IDbConnector dbConnector)
            : base(logManager, dbConnector)
        {
            _jsonSerializer = jsonSerializer;
            DbFilePath = Path.Combine(appPaths.DataPath, "displaypreferences.db");
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
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                string[] queries = {

                                "create table if not exists userdisplaypreferences (id GUID, userId GUID, client text, data BLOB)",
                                "create unique index if not exists userdisplaypreferencesindex on userdisplaypreferences (id, userId, client)"
                               };

                connection.RunQueries(queries, Logger);
            }
        }

        /// <summary>
        /// Save the display preferences associated with an item in the repo
        /// </summary>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="client">The client.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public async Task SaveDisplayPreferences(DisplayPreferences displayPreferences, Guid userId, string client, CancellationToken cancellationToken)
        {
            if (displayPreferences == null)
            {
                throw new ArgumentNullException("displayPreferences");
            }
            if (string.IsNullOrWhiteSpace(displayPreferences.Id))
            {
                throw new ArgumentNullException("displayPreferences.Id");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var serialized = _jsonSerializer.SerializeToBytes(displayPreferences);

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                IDbTransaction transaction = null;

                try
                {
                    transaction = connection.BeginTransaction();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "replace into userdisplaypreferences (id, userid, client, data) values (@1, @2, @3, @4)";

                        cmd.Parameters.Add(cmd, "@1", DbType.Guid).Value = new Guid(displayPreferences.Id);
                        cmd.Parameters.Add(cmd, "@2", DbType.Guid).Value = userId;
                        cmd.Parameters.Add(cmd, "@3", DbType.String).Value = client;
                        cmd.Parameters.Add(cmd, "@4", DbType.Binary).Value = serialized;

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
                    Logger.ErrorException("Failed to save display preferences:", e);

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
        /// Save all display preferences associated with a user in the repo
        /// </summary>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public async Task SaveAllDisplayPreferences(IEnumerable<DisplayPreferences> displayPreferences, Guid userId, CancellationToken cancellationToken)
        {
            if (displayPreferences == null)
            {
                throw new ArgumentNullException("displayPreferences");
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = await CreateConnection().ConfigureAwait(false))
            {
                IDbTransaction transaction = null;

                try
                {
                    transaction = connection.BeginTransaction();

                    foreach (var displayPreference in displayPreferences)
                    {

                        var serialized = _jsonSerializer.SerializeToBytes(displayPreference);

                        using (var cmd = connection.CreateCommand())
                        {
                            cmd.CommandText = "replace into userdisplaypreferences (id, userid, client, data) values (@1, @2, @3, @4)";

                            cmd.Parameters.Add(cmd, "@1", DbType.Guid).Value = new Guid(displayPreference.Id);
                            cmd.Parameters.Add(cmd, "@2", DbType.Guid).Value = userId;
                            cmd.Parameters.Add(cmd, "@3", DbType.String).Value = displayPreference.Client;
                            cmd.Parameters.Add(cmd, "@4", DbType.Binary).Value = serialized;

                            cmd.Transaction = transaction;

                            cmd.ExecuteNonQuery();
                        }
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
                    Logger.ErrorException("Failed to save display preferences:", e);

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
        /// Gets the display preferences.
        /// </summary>
        /// <param name="displayPreferencesId">The display preferences id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="client">The client.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public DisplayPreferences GetDisplayPreferences(string displayPreferencesId, Guid userId, string client)
        {
            if (string.IsNullOrWhiteSpace(displayPreferencesId))
            {
                throw new ArgumentNullException("displayPreferencesId");
            }

            var guidId = displayPreferencesId.GetMD5();

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select data from userdisplaypreferences where id = @id and userId=@userId and client=@client";

                    cmd.Parameters.Add(cmd, "@id", DbType.Guid).Value = guidId;
                    cmd.Parameters.Add(cmd, "@userId", DbType.Guid).Value = userId;
                    cmd.Parameters.Add(cmd, "@client", DbType.String).Value = client;

                    using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                    {
                        if (reader.Read())
                        {
                            using (var stream = reader.GetMemoryStream(0))
                            {
                                return _jsonSerializer.DeserializeFromStream<DisplayPreferences>(stream);
                            }
                        }
                    }

                    return new DisplayPreferences
                    {
                        Id = guidId.ToString("N")
                    };
                }
            }
        }

        /// <summary>
        /// Gets all display preferences for the given user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public IEnumerable<DisplayPreferences> GetAllDisplayPreferences(Guid userId)
        {
            var list = new List<DisplayPreferences>();

            using (var connection = CreateConnection(true).Result)
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "select data from userdisplaypreferences where userId=@userId";

                    cmd.Parameters.Add(cmd, "@userId", DbType.Guid).Value = userId;

                    using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
                    {
                        while (reader.Read())
                        {
                            using (var stream = reader.GetMemoryStream(0))
                            {
                                list.Add(_jsonSerializer.DeserializeFromStream<DisplayPreferences>(stream));
                            }
                        }
                    }
                }
            }

            return list;
        }

        public Task SaveDisplayPreferences(DisplayPreferences displayPreferences, string userId, string client, CancellationToken cancellationToken)
        {
            return SaveDisplayPreferences(displayPreferences, new Guid(userId), client, cancellationToken);
        }

        public DisplayPreferences GetDisplayPreferences(string displayPreferencesId, string userId, string client)
        {
            return GetDisplayPreferences(displayPreferencesId, new Guid(userId), client);
        }
    }
}