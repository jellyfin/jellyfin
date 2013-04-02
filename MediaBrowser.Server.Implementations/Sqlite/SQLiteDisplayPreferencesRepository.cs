using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sqlite
{
    /// <summary>
    /// Class SQLiteDisplayPreferencesRepository
    /// </summary>
    class SQLiteDisplayPreferencesRepository : SqliteRepository, IDisplayPreferencesRepository
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
        /// The _protobuf serializer
        /// </summary>
        private readonly IProtobufSerializer _protobufSerializer;

        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IApplicationPaths _appPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteUserDataRepository" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="protobufSerializer">The protobuf serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <exception cref="System.ArgumentNullException">protobufSerializer</exception>
        public SQLiteDisplayPreferencesRepository(IApplicationPaths appPaths, IProtobufSerializer protobufSerializer, ILogger logger)
            : base(logger)
        {
            if (protobufSerializer == null)
            {
                throw new ArgumentNullException("protobufSerializer");
            }
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }

            _protobufSerializer = protobufSerializer;
            _appPaths = appPaths;
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "displaypreferences.db");

            await ConnectToDB(dbFile).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists displaypreferences (id GUID, userId GUID, data BLOB)",
                                "create unique index if not exists displaypreferencesindex on displaypreferences (id, userId)",
                                "create table if not exists schema_version (table_name primary key, version)",
                                //pragmas
                                "pragma temp_store = memory"
                               };

            RunQueries(queries);
        }

        /// <summary>
        /// Save the display preferences associated with an item in the repo
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="displayPreferencesId">The display preferences id.</param>
        /// <param name="displayPreferences">The display preferences.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Task SaveDisplayPreferences(Guid userId, Guid displayPreferencesId, DisplayPreferences displayPreferences, CancellationToken cancellationToken)
        {
            if (displayPreferences == null)
            {
                throw new ArgumentNullException("displayPreferences");
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (displayPreferencesId == Guid.Empty)
            {
                throw new ArgumentNullException("displayPreferencesId");
            }

            cancellationToken.ThrowIfCancellationRequested();
            
            return Task.Run(() =>
            {
                var serialized = _protobufSerializer.SerializeToBytes(displayPreferences);

                cancellationToken.ThrowIfCancellationRequested();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "replace into displaypreferences (id, userId, data) values (@1, @2, @3)";
                cmd.AddParam("@1", displayPreferencesId);
                cmd.AddParam("@2", userId);
                cmd.AddParam("@3", serialized);
                QueueCommand(cmd);
            });
        }

        /// <summary>
        /// Gets the display preferences.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="displayPreferencesId">The display preferences id.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public async Task<DisplayPreferences> GetDisplayPreferences(Guid userId, Guid displayPreferencesId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (displayPreferencesId == Guid.Empty)
            {
                throw new ArgumentNullException("displayPreferencesId");
            }

            var cmd = connection.CreateCommand();
            cmd.CommandText = "select data from displaypreferences where id = @id and userId=@userId";
            
            var idParam = cmd.Parameters.Add("@id", DbType.Guid);
            idParam.Value = displayPreferencesId;

            var userIdParam = cmd.Parameters.Add("@userId", DbType.Guid);
            userIdParam.Value = userId;

            using (var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow).ConfigureAwait(false))
            {
                if (reader.Read())
                {
                    using (var stream = GetStream(reader, 0))
                    {
                        return _protobufSerializer.DeserializeFromStream<DisplayPreferences>(stream);
                    }
                }
            }

            return null;
        }
    }
}
