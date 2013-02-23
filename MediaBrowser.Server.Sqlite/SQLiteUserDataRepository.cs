using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Sqlite
{
    /// <summary>
    /// Class SQLiteUserDataRepository
    /// </summary>
    public class SQLiteUserDataRepository : SqliteRepository, IUserDataRepository
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
        /// Initializes a new instance of the <see cref="SQLiteUserDataRepository" /> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public SQLiteUserDataRepository(ILogger logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            var dbFile = Path.Combine(Kernel.Instance.ApplicationPaths.DataPath, "userdata.db");

            await ConnectToDB(dbFile).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists user_data (item_id GUID, user_id GUID, data BLOB)",
                                "create unique index if not exists idx_user_data on user_data (item_id, user_id)",
                                "create table if not exists schema_version (table_name primary key, version)",
                                //pragmas
                                "pragma temp_store = memory"
                               };

            RunQueries(queries);
        }

        /// <summary>
        /// Save the user specific data associated with an item in the repo
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Task SaveUserData(BaseItem item, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }
            
            return Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cmd = connection.CreateCommand();

                cmd.CommandText = "delete from user_data where item_id = @guid";
                cmd.AddParam("@guid", item.UserDataId);

                QueueCommand(cmd);

                if (item.UserData != null)
                {
                    foreach (var data in item.UserData)
                    {
                        cmd = connection.CreateCommand();
                        cmd.CommandText = "insert into user_data (item_id, user_id, data) values (@1, @2, @3)";
                        cmd.AddParam("@1", item.UserDataId);
                        cmd.AddParam("@2", data.UserId);

                        cmd.AddParam("@3", Kernel.Instance.ProtobufSerializer.SerializeToBytes(data));

                        QueueCommand(cmd);
                    }
                }
            });
        }

        /// <summary>
        /// Gets user data for an item
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IEnumerable{UserItemData}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public IEnumerable<UserItemData> RetrieveUserData(BaseItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var cmd = connection.CreateCommand();
            cmd.CommandText = "select data from user_data where item_id = @guid";
            var guidParam = cmd.Parameters.Add("@guid", DbType.Guid);
            guidParam.Value = item.UserDataId;

            using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
            {
                while (reader.Read())
                {
                    using (var stream = GetStream(reader, 0))
                    {
                        var data = Kernel.Instance.ProtobufSerializer.DeserializeFromStream<UserItemData>(stream);
                        if (data != null)
                        {
                            yield return data;
                        }
                    }
                }
            }
        }
    }
}
