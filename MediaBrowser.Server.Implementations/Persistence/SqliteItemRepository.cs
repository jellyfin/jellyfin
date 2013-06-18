using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    /// <summary>
    /// Class SQLiteItemRepository
    /// </summary>
    public class SqliteItemRepository : SqliteRepository, IItemRepository
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

        private readonly string _criticReviewsPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteItemRepository"/> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logManager">The log manager.</param>
        /// <exception cref="System.ArgumentNullException">
        /// appPaths
        /// or
        /// jsonSerializer
        /// </exception>
        public SqliteItemRepository(IApplicationPaths appPaths, IJsonSerializer jsonSerializer, ILogManager logManager)
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

            _criticReviewsPath = Path.Combine(_appPaths.DataPath, "critic-reviews");
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

                                "create table if not exists baseitems (guid GUID primary key, data BLOB)",
                                "create index if not exists idx_baseitems on baseitems(guid)",
                                "create table if not exists schema_version (table_name primary key, version)",
                                //pragmas
                                "pragma temp_store = memory"
                               };

            RunQueries(queries);

            PrepareStatements();
        }

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
                CommandText = "replace into baseitems (guid, data) values (@1, @2)"
            };

            _saveItemCommand.Parameters.Add(new SQLiteParameter("@1"));
            _saveItemCommand.Parameters.Add(new SQLiteParameter("@2"));
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
                    _saveItemCommand.Parameters[1].Value = _jsonSerializer.SerializeToBytes(item);

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

                throw;
            }
            catch (Exception e)
            {
                Logger.ErrorException("Failed to save items:", e);

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
        /// Internal retrieve from items or users table
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="type">The type.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        /// <exception cref="System.ArgumentException"></exception>
        public BaseItem RetrieveItem(Guid id, Type type)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            using (var cmd = Connection.CreateCommand())
            {
                cmd.CommandText = "select data from baseitems where guid = @guid";
                var guidParam = cmd.Parameters.Add("@guid", DbType.Guid);
                guidParam.Value = id;

                using (var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.SingleRow))
                {
                    if (reader.Read())
                    {
                        using (var stream = GetStream(reader, 0))
                        {
                            return _jsonSerializer.DeserializeFromStream(stream, type) as BaseItem;
                        }
                    }
                }
                return null;
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
                    var path = Path.Combine(_criticReviewsPath, itemId + ".json");

                    return _jsonSerializer.DeserializeFromFile<List<ItemReview>>(path);
                }
                catch (DirectoryNotFoundException)
                {
                    return new List<ItemReview>();
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
                if (!Directory.Exists(_criticReviewsPath))
                {
                    Directory.CreateDirectory(_criticReviewsPath);
                }

                var path = Path.Combine(_criticReviewsPath, itemId + ".json");

                _jsonSerializer.SerializeToFile(criticReviews.ToList(), path);
            });
        }
    }
}