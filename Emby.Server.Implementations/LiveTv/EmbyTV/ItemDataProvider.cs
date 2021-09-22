#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Jellyfin.Extensions.Json;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
{
    public class ItemDataProvider<T>
        where T : class
    {
        private readonly string _dataPath;
        private readonly object _fileDataLock = new object();
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private T[] _items;

        public ItemDataProvider(
            ILogger logger,
            string dataPath,
            Func<T, T, bool> equalityComparer)
        {
            Logger = logger;
            _dataPath = dataPath;
            EqualityComparer = equalityComparer;
        }

        protected ILogger Logger { get; }

        protected Func<T, T, bool> EqualityComparer { get; }

        private void EnsureLoaded()
        {
            if (_items != null)
            {
                return;
            }

            if (File.Exists(_dataPath))
            {
                Logger.LogInformation("Loading live tv data from {Path}", _dataPath);

                try
                {
                    var bytes = File.ReadAllBytes(_dataPath);
                    _items = JsonSerializer.Deserialize<T[]>(bytes, _jsonOptions);
                    return;
                }
                catch (JsonException ex)
                {
                    Logger.LogError(ex, "Error deserializing {Path}", _dataPath);
                }
            }

            _items = Array.Empty<T>();
        }

        private void SaveList()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_dataPath));
            var jsonString = JsonSerializer.Serialize(_items, _jsonOptions);
            File.WriteAllText(_dataPath, jsonString);
        }

        public IReadOnlyList<T> GetAll()
        {
            lock (_fileDataLock)
            {
                EnsureLoaded();
                return (T[])_items.Clone();
            }
        }

        public virtual void Update(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            lock (_fileDataLock)
            {
                EnsureLoaded();

                var index = Array.FindIndex(_items, i => EqualityComparer(i, item));
                if (index == -1)
                {
                    throw new ArgumentException("item not found");
                }

                _items[index] = item;

                SaveList();
            }
        }

        public virtual void Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            lock (_fileDataLock)
            {
                EnsureLoaded();

                if (_items.Any(i => EqualityComparer(i, item)))
                {
                    throw new ArgumentException("item already exists", nameof(item));
                }

                int oldLen = _items.Length;
                var newList = new T[oldLen + 1];
                _items.CopyTo(newList, 0);
                newList[oldLen] = item;
                _items = newList;

                SaveList();
            }
        }

        public virtual void AddOrUpdate(T item)
        {
            lock (_fileDataLock)
            {
                EnsureLoaded();

                int index = Array.FindIndex(_items, i => EqualityComparer(i, item));
                if (index == -1)
                {
                    int oldLen = _items.Length;
                    var newList = new T[oldLen + 1];
                    _items.CopyTo(newList, 0);
                    newList[oldLen] = item;
                    _items = newList;
                }
                else
                {
                    _items[index] = item;
                }

                SaveList();
            }
        }

        public virtual void Delete(T item)
        {
            lock (_fileDataLock)
            {
                EnsureLoaded();
                _items = _items.Where(i => !EqualityComparer(i, item)).ToArray();

                SaveList();
            }
        }
    }
}
