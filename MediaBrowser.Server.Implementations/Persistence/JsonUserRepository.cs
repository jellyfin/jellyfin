using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
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
    public class JsonUserRepository : IUserRepository
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

        private readonly string _dataPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonUserRepository"/> class.
        /// </summary>
        /// <param name="appPaths">The app paths.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logManager">The log manager.</param>
        /// <exception cref="System.ArgumentNullException">
        /// appPaths
        /// or
        /// jsonSerializer
        /// </exception>
        public JsonUserRepository(IApplicationPaths appPaths, IJsonSerializer jsonSerializer, ILogManager logManager)
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

            _dataPath = Path.Combine(appPaths.DataPath, "users");
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
        /// Save a user in the repo
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public async Task SaveUser(User user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }

            var path = Path.Combine(_dataPath, user.Id + ".json");

            var semaphore = GetLock(path);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                _jsonSerializer.SerializeToFile(user, path);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Retrieve all users from the database
        /// </summary>
        /// <returns>IEnumerable{User}.</returns>
        public IEnumerable<User> RetrieveAllUsers()
        {
            try
            {
                return Directory.EnumerateFiles(_dataPath, "*.json", SearchOption.TopDirectoryOnly)
                    .Select(i => _jsonSerializer.DeserializeFromFile<User>(i));
            }
            catch (IOException)
            {
                return new List<User>();
            }
        }

        /// <summary>
        /// Deletes the user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">user</exception>
        public async Task DeleteUser(User user, CancellationToken cancellationToken)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var path = Path.Combine(_dataPath, user.Id + ".json");

            var semaphore = GetLock(path);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                File.Delete(path);
            }
            finally
            {
                semaphore.Release();
            }
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
