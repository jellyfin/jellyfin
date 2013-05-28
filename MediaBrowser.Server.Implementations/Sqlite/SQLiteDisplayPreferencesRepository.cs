using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sqlite
{
    /// <summary>
    /// Class SQLiteDisplayPreferencesRepository
    /// </summary>
    public class SQLiteDisplayPreferencesRepository : SqliteRepository, IDisplayPreferencesRepository
    {
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
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IApplicationPaths _appPaths;

        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        
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
        public SQLiteDisplayPreferencesRepository(IApplicationPaths appPaths, IJsonSerializer jsonSerializer, ILogManager logManager)
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
            var dbFile = Path.Combine(_appPaths.DataPath, "displaypreferences.db");

            await ConnectToDb(dbFile).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists displaypreferences (id GUID, data BLOB)",
                                "create unique index if not exists displaypreferencesindex on displaypreferences (id)",
                                "create table if not exists schema_version (table_name primary key, version)",
                                //pragmas
                                "pragma temp_store = memory"
                               };

            RunQueries(queries);
        }

        /// <summary>
        /// Save the display preferences associated with an item in the repo
        /// </summary>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public async Task SaveDisplayPreferences(DisplayPreferences displayPreferences, CancellationToken cancellationToken)
        {
            if (displayPreferences == null)
            {
                throw new ArgumentNullException("displayPreferences");
            }
            if (displayPreferences.Id == Guid.Empty)
            {
                throw new ArgumentNullException("displayPreferences.Id");
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var serialized = _jsonSerializer.SerializeToBytes(displayPreferences);

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            SQLiteTransaction transaction = null;

            try
            {
                transaction = Connection.BeginTransaction();

                using (var cmd = Connection.CreateCommand())
                {
                    cmd.CommandText = "replace into displaypreferences (id, data) values (@1, @2)";
                    cmd.AddParam("@1", displayPreferences.Id);
                    cmd.AddParam("@2", serialized);

                    cmd.Transaction = transaction;

                    await cmd.ExecuteNonQueryAsync(cancellationToken);
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

                _writeLock.Release();
            }
        }

        /// <summary>
        /// Gets the display preferences.
        /// </summary>
        /// <param name="displayPreferencesId">The display preferences id.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public async Task<DisplayPreferences> GetDisplayPreferences(Guid displayPreferencesId)
        {
            if (displayPreferencesId == Guid.Empty)
            {
                throw new ArgumentNullException("displayPreferencesId");
            }

            var cmd = Connection.CreateCommand();
            cmd.CommandText = "select data from displaypreferences where id = @id";

            var idParam = cmd.Parameters.Add("@id", DbType.Guid);
            idParam.Value = displayPreferencesId;

            using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow).ConfigureAwait(false))
            {
                if (reader.Read())
                {
                    using (var stream = GetStream(reader, 0))
                    {
                        return _jsonSerializer.DeserializeFromStream<DisplayPreferences>(stream);
                    }
                }
            }

            return null;
        }
    }
}
