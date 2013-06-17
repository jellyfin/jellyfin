using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class JsonItemRepository : IItemRepository
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        private SemaphoreSlim GetLock(string filename)
        {
            return _fileLocks.GetOrAdd(filename, key => new SemaphoreSlim(1, 1));
        }
        
        /// <summary>
        /// Gets the name of the repository
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return "Json";
            }
        }

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        private readonly IJsonSerializer _jsonSerializer;

        private readonly string _criticReviewsPath;

        private readonly FileSystemRepository _itemRepo;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonUserDataRepository" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logManager">The log manager.</param>
        /// <exception cref="System.ArgumentNullException">appPaths</exception>
        public JsonItemRepository(IApplicationPaths appPaths, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }

            _jsonSerializer = jsonSerializer;

            _criticReviewsPath = Path.Combine(appPaths.DataPath, "critic-reviews");

            _itemRepo = new FileSystemRepository(Path.Combine(appPaths.DataPath, "library"));
        }

        /// <summary>
        /// Opens the connection to the database
        /// </summary>
        /// <returns>Task.</returns>
        public Task Initialize()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Save a standard item in the repo
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public async Task SaveItem(BaseItem item, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            if (!Directory.Exists(_criticReviewsPath))
            {
                Directory.CreateDirectory(_criticReviewsPath);
            }

            var path = _itemRepo.GetResourcePath(item.Id + ".json");

            var parentPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }

            var semaphore = GetLock(path);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                _jsonSerializer.SerializeToFile(item, path);
            }
            finally
            {
                semaphore.Release();
            }
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
        public Task SaveItems(IEnumerable<BaseItem> items, CancellationToken cancellationToken)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            var tasks = items.Select(i => SaveItem(i, cancellationToken));

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Retrieves the item.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="type">The type.</param>
        /// <returns>BaseItem.</returns>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public BaseItem RetrieveItem(Guid id, Type type)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException("id");
            }

            var path = _itemRepo.GetResourcePath(id + ".json");

            try
            {
                return (BaseItem)_jsonSerializer.DeserializeFromFile(type, path);
            }
            catch (IOException)
            {
                // File doesn't exist or is currently bring written to
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
                var path = Path.Combine(_criticReviewsPath, itemId + ".json");

                try
                {
                    return _jsonSerializer.DeserializeFromFile<List<ItemReview>>(path);
                }
                catch (IOException)
                {
                    // File doesn't exist or is currently bring written to
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

        public void Dispose()
        {
            // Wait up to two seconds for any existing writes to finish
            var locks = _fileLocks.Values.ToList()
                                  .Where(i => i.CurrentCount == 1)
                                  .Select(i => i.WaitAsync(2000));

            var task = Task.WhenAll(locks);

            Task.WaitAll(task);
        }
    }
}
