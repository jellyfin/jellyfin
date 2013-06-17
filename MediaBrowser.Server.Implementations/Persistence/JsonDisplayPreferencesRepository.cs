using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
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
    public class JsonDisplayPreferencesRepository : IDisplayPreferencesRepository
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
        /// The _json serializer
        /// </summary>
        private readonly IJsonSerializer _jsonSerializer;

        private readonly string _dataPath;

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
        public JsonDisplayPreferencesRepository(IApplicationPaths appPaths, IJsonSerializer jsonSerializer, ILogManager logManager)
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
            _dataPath = Path.Combine(appPaths.DataPath, "display-preferences");
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

            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }

            var path = Path.Combine(_dataPath, displayPreferences.Id + ".json");

            var semaphore = GetLock(path);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                _jsonSerializer.SerializeToFile(displayPreferences, path);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the display preferences.
        /// </summary>
        /// <param name="displayPreferencesId">The display preferences id.</param>
        /// <returns>Task{DisplayPreferences}.</returns>
        /// <exception cref="System.ArgumentNullException">item</exception>
        public Task<DisplayPreferences> GetDisplayPreferences(Guid displayPreferencesId)
        {
            if (displayPreferencesId == Guid.Empty)
            {
                throw new ArgumentNullException("displayPreferencesId");
            }

            return Task.Run(() =>
            {
                var path = Path.Combine(_dataPath, displayPreferencesId + ".json");

                try
                {
                    return _jsonSerializer.DeserializeFromFile<DisplayPreferences>(path);
                }
                catch (IOException)
                {
                    // File doesn't exist or is currently bring written to
                    return null;
                }
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
