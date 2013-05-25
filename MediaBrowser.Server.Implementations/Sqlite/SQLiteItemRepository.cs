using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Server.Implementations.Reflection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
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
        /// The _save item command
        /// </summary>
        private SQLiteCommand _saveItemCommand;
        /// <summary>
        /// The _delete children command
        /// </summary>
        private SQLiteCommand _deleteChildrenCommand;
        /// <summary>
        /// The _save children command
        /// </summary>
        private SQLiteCommand _saveChildrenCommand;

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

            PrepareStatements();
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
        /// The _write lock
        /// </summary>
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Prepares the statements.
        /// </summary>
        private void PrepareStatements()
        {
            _saveItemCommand = new SQLiteCommand
            {
                CommandText = "replace into items (guid, obj_type, data) values (@1, @2, @3)"
            };

            _saveItemCommand.Parameters.Add(new SQLiteParameter("@1"));
            _saveItemCommand.Parameters.Add(new SQLiteParameter("@2"));
            _saveItemCommand.Parameters.Add(new SQLiteParameter("@3"));

            _deleteChildrenCommand = new SQLiteCommand
            {
                CommandText = "delete from children where guid = @guid"
            };
            _deleteChildrenCommand.Parameters.Add(new SQLiteParameter("@guid"));

            _saveChildrenCommand = new SQLiteCommand
            {
                CommandText = "replace into children (guid, child) values (@guid, @child)"
            };
            _saveChildrenCommand.Parameters.Add(new SQLiteParameter("@guid"));
            _saveChildrenCommand.Parameters.Add(new SQLiteParameter("@child"));
        }

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

            return SaveItems(new[] { item }, cancellationToken);
        }

        /// <summary>
        /// Saves the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// items
        /// or
        /// cancellationToken
        /// </exception>
        public async Task SaveItems(IEnumerable<BaseItem> items, CancellationToken cancellationToken)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            cancellationToken.ThrowIfCancellationRequested();

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            SQLiteTransaction transaction = null;

            try
            {
                transaction = Connection.BeginTransaction();

                foreach (var item in items)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    _saveItemCommand.Parameters[0].Value = item.Id;
                    _saveItemCommand.Parameters[1].Value = item.GetType().FullName;
                    _saveItemCommand.Parameters[2].Value = _jsonSerializer.SerializeToBytes(item);

                    _saveItemCommand.Transaction = transaction;

                    await _saveItemCommand.ExecuteNonQueryAsync(cancellationToken);
                }
                
                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }
            }
            catch (Exception e)
            {
                Logger.ErrorException("Failed to save item:", e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }
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
        /// Retrieve a standard item from the repo
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        /// <exception cref="System.ArgumentException"></exception>
        public BaseItem GetItem(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            return RetrieveItemInternal(id);
        }

        /// <summary>
        /// Retrieves the items.
        /// </summary>
        /// <param name="ids">The ids.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException">ids</exception>
        public IEnumerable<BaseItem> GetItems(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException("ids");
            }

            return ids.Select(RetrieveItemInternal);
        }

        /// <summary>
        /// Internal retrieve from items or users table
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        /// <exception cref="System.ArgumentException"></exception>
        protected BaseItem RetrieveItemInternal(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            using (var cmd = Connection.CreateCommand())
            {
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

            using (var cmd = Connection.CreateCommand())
            {
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
        public async Task SaveChildren(Guid id, IEnumerable<BaseItem> children, CancellationToken cancellationToken)
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

            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            SQLiteTransaction transaction = null;

            try
            {
                transaction = Connection.BeginTransaction();

                // Delete exising children
                _deleteChildrenCommand.Parameters[0].Value = id;
                _deleteChildrenCommand.Transaction = transaction;
                await _deleteChildrenCommand.ExecuteNonQueryAsync(cancellationToken);

                // Save new children
                foreach (var child in children)
                {
                    _saveChildrenCommand.Transaction = transaction;

                    _saveChildrenCommand.Parameters[0].Value = id;
                    _saveChildrenCommand.Parameters[1].Value = child.Id;

                    await _saveChildrenCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                transaction.Commit();
            }
            catch (OperationCanceledException)
            {
                if (transaction != null)
                {
                    transaction.Rollback();
                }
            }
            catch (Exception e)
            {
                Logger.ErrorException("Failed to save item:", e);

                if (transaction != null)
                {
                    transaction.Rollback();
                }
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
        /// Gets the critic reviews path.
        /// </summary>
        /// <value>The critic reviews path.</value>
        private string CriticReviewsPath
        {
            get
            {
                var path = Path.Combine(_appPaths.DataPath, "critic-reviews");

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            }
        }

        /// <summary>
        /// Gets the critic reviews.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <returns>Task{IEnumerable{ItemReview}}.</returns>
        public Task<IEnumerable<ItemReview>> GetCriticReviews(Guid itemId)
        {
            return Task.Run<IEnumerable<ItemReview>>(() =>
            {

                try
                {
                    var path = Path.Combine(CriticReviewsPath, itemId + ".json");

                    return _jsonSerializer.DeserializeFromFile<List<ItemReview>>(path);
                }
                catch (FileNotFoundException)
                {
                    return new List<ItemReview>();
                }

            });
        }

        /// <summary>
        /// Saves the critic reviews.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <param name="criticReviews">The critic reviews.</param>
        /// <returns>Task.</returns>
        public Task SaveCriticReviews(Guid itemId, IEnumerable<ItemReview> criticReviews)
        {
            return Task.Run(() =>
            {
                var path = Path.Combine(CriticReviewsPath, itemId + ".json");

                _jsonSerializer.SerializeToFile(criticReviews.ToList(), path);
            });
        }
    }
}
