using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Persistence
{
    public class JsonUserDataRepository : IUserDataRepository
    {
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        private SemaphoreSlim GetLock(string filename)
        {
            return _fileLocks.GetOrAdd(filename, key => new SemaphoreSlim(1, 1));
        }
        
        private readonly ConcurrentDictionary<string, UserItemData> _userData = new ConcurrentDictionary<string, UserItemData>();

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

        private readonly IJsonSerializer _jsonSerializer;

        private readonly string _dataPath;

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonUserDataRepository" /> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logManager">The log manager.</param>
        /// <exception cref="System.ArgumentNullException">
        /// jsonSerializer
        /// or
        /// appPaths
        /// </exception>
        public JsonUserDataRepository(IApplicationPaths appPaths, IJsonSerializer jsonSerializer, ILogManager logManager)
        {
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            if (appPaths == null)
            {
                throw new ArgumentNullException("appPaths");
            }

            _logger = logManager.GetLogger(GetType().Name);
            _jsonSerializer = jsonSerializer;
            _dataPath = Path.Combine(appPaths.DataPath, "userdata");
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
        /// Saves the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">userData
        /// or
        /// cancellationToken
        /// or
        /// userId
        /// or
        /// userDataId</exception>
        public async Task SaveUserData(Guid userId, string key, UserItemData userData, CancellationToken cancellationToken)
        {
            if (userData == null)
            {
                throw new ArgumentNullException("userData");
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await PersistUserData(userId, key, userData, cancellationToken).ConfigureAwait(false);

                // Once it succeeds, put it into the dictionary to make it available to everyone else
                _userData.AddOrUpdate(GetInternalKey(userId, key), userData, delegate { return userData; });
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error saving user data", ex);

                throw;
            }
        }

        /// <summary>
        /// Gets the internal key.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>System.String.</returns>
        private string GetInternalKey(Guid userId, string key)
        {
            return userId + key;
        }

        /// <summary>
        /// Persists the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <param name="userData">The user data.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task PersistUserData(Guid userId, string key, UserItemData userData, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = GetUserDataPath(userId, key);

            var parentPath = Path.GetDirectoryName(path);
            if (!Directory.Exists(parentPath))
            {
                Directory.CreateDirectory(parentPath);
            }

            var semaphore = GetLock(path);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                _jsonSerializer.SerializeToFile(userData, path);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>Task{UserItemData}.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// userId
        /// or
        /// key
        /// </exception>
        public UserItemData GetUserData(Guid userId, string key)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException("key");
            }

            return _userData.GetOrAdd(GetInternalKey(userId, key), keyName => RetrieveUserData(userId, key));
        }

        /// <summary>
        /// Retrieves the user data.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="key">The key.</param>
        /// <returns>Task{UserItemData}.</returns>
        private UserItemData RetrieveUserData(Guid userId, string key)
        {
            var path = GetUserDataPath(userId, key);

            try
            {
                return _jsonSerializer.DeserializeFromFile<UserItemData>(path);
            }
            catch (IOException)
            {
                // File doesn't exist or is currently bring written to
                return new UserItemData { UserId = userId };
            }
        }

        private string GetUserDataPath(Guid userId, string key)
        {
            var userFolder = Path.Combine(_dataPath, userId.ToString());

            var keyHash = key.GetMD5().ToString();

            var prefix = keyHash.Substring(0, 1);

            return Path.Combine(userFolder, prefix, keyHash + ".json");
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