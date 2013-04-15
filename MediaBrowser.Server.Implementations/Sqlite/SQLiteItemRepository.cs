using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Server.Implementations.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sqlite
{
    /// <summary>
    /// Class SQLiteItemRepository
    /// </summary>
    public class SQLiteItemRepository : SqliteRepository, IItemRepository
    {
        /// <summary>
        /// The _type mapper
        /// </summary>
        private readonly TypeMapper _typeMapper = new TypeMapper();

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
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        private readonly IJsonSerializer _jsonSerializer;

        /// <summary>
        /// The _app paths
        /// </summary>
        private readonly IApplicationPaths _appPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteUserDataRepository" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logManager">The log manager.</param>
        /// <exception cref="System.ArgumentNullException">appPaths</exception>
        public SQLiteItemRepository(IApplicationPaths appPaths, IJsonSerializer jsonSerializer, ILogManager logManager)
            : base(logManager)
        {
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }

            _appPaths = appPaths;
            _jsonSerializer = jsonSerializer;
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public async Task Initialize()
        {
            var dbFile = Path.Combine(_appPaths.DataPath, "library.db");

            await ConnectToDb(dbFile).ConfigureAwait(false);

            string[] queries = {

                                "create table if not exists items (guid GUID primary key, obj_type, data BLOB)",
                                "create index if not exists idx_items on items(guid)",
                                "create table if not exists children (guid GUID, child GUID)", 
                                "create unique index if not exists idx_children on children(guid, child)",
                                "create table if not exists schema_version (table_name primary key, version)",
                                //triggers
                                TriggerSql,
                                //pragmas
                                "pragma temp_store = memory"
                               };

            RunQueries(queries);
        }

        //cascade delete triggers
        /// <summary>
        /// The trigger SQL
        /// </summary>
        protected string TriggerSql =
            @"CREATE TRIGGER if not exists delete_item
                AFTER DELETE
                ON items
                FOR EACH ROW
                BEGIN
                    DELETE FROM children WHERE children.guid = old.child;
                    DELETE FROM children WHERE children.child = old.child;
                END";

        /// <summary>
        /// Save a standard item in the repo
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Task SaveItem(BaseItem item, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            cancellationToken.ThrowIfCancellationRequested();

            return Task.Run(() =>
            {
                var serialized = _jsonSerializer.SerializeToBytes(item);

                cancellationToken.ThrowIfCancellationRequested();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "replace into items (guid, obj_type, data) values (@1, @2, @3)";
                cmd.AddParam("@1", item.Id);
                cmd.AddParam("@2", item.GetType().FullName);
                cmd.AddParam("@3", serialized);
                QueueCommand(cmd);
            });
        }

        /// <summary>
        /// Retrieve a standard item from the repo
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentException"></exception>
        public BaseItem RetrieveItem(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException();
            }

            return RetrieveItemInternal(id);
        }

        /// <summary>
        /// Internal retrieve from items or users table
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentException"></exception>
        protected BaseItem RetrieveItemInternal(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException();
            }

            var cmd = connection.CreateCommand();
            cmd.CommandText = "select obj_type,data from items where guid = @guid";
            var guidParam = cmd.Parameters.Add("@guid", DbType.Guid);
            guidParam.Value = id;

            using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
            {
                if (reader.Read())
                {
                    var type = reader.GetString(0);
                    using (var stream = GetStream(reader, 1))
                    {
                        var itemType = _typeMapper.GetType(type);

                        if (itemType == null)
                        {
                            Logger.Error("Cannot find type {0}.  Probably belongs to plug-in that is no longer loaded.", type);
                            return null;
                        }

                        var item = _jsonSerializer.DeserializeFromStream(stream, itemType);
                        return item as BaseItem;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieve all the children of the given folder
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public IEnumerable<BaseItem> RetrieveChildren(Folder parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException();
            }

            var cmd = connection.CreateCommand();
            cmd.CommandText = "select obj_type,data from items where guid in (select child from children where guid = @guid)";
            var guidParam = cmd.Parameters.Add("@guid", DbType.Guid);
            guidParam.Value = parent.Id;

            using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult))
            {
                while (reader.Read())
                {
                    var type = reader.GetString(0);

                    using (var stream = GetStream(reader, 1))
                    {
                        var itemType = _typeMapper.GetType(type);
                        if (itemType == null)
                        {
                            Logger.Error("Cannot find type {0}.  Probably belongs to plug-in that is no longer loaded.", type);
                            continue;
                        }
                        var item = _jsonSerializer.DeserializeFromStream(stream, itemType) as BaseItem;
                        if (item != null)
                        {
                            item.Parent = parent;
                            yield return item;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save references to all the children for the given folder
        /// (Doesn't actually save the child entities)
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="children">The children.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public Task SaveChildren(Guid id, IEnumerable<BaseItem> children, CancellationToken cancellationToken)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            if (children == null)
            {
                throw new ArgumentNullException("children");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            cancellationToken.ThrowIfCancellationRequested();

            return Task.Run(() =>
            {
                var cmd = connection.CreateCommand();

                cmd.CommandText = "delete from children where guid = @guid";
                cmd.AddParam("@guid", id);

                QueueCommand(cmd);

                foreach (var child in children)
                {
                    var guid = child.Id;
                    cmd = connection.CreateCommand();
                    cmd.AddParam("@guid", id);
                    cmd.CommandText = "replace into children (guid, child) values (@guid, @child)";
                    var childParam = cmd.Parameters.Add("@child", DbType.Guid);

                    childParam.Value = guid;
                    QueueCommand(cmd);
                }
            });
        }
    }
}
