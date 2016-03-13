using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonIO;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    public class ItemDataProvider<T>
        where T : class
    {
        private readonly object _fileDataLock = new object();
        private List<T> _items;
        private readonly IJsonSerializer _jsonSerializer;
        protected readonly ILogger Logger;
        private readonly string _dataPath;
        protected readonly Func<T, T, bool> EqualityComparer;
        private readonly IFileSystem _fileSystem;

        public ItemDataProvider(IFileSystem fileSystem, IJsonSerializer jsonSerializer, ILogger logger, string dataPath, Func<T, T, bool> equalityComparer)
        {
            Logger = logger;
            _dataPath = dataPath;
            EqualityComparer = equalityComparer;
            _jsonSerializer = jsonSerializer;
            _fileSystem = fileSystem;
        }

        public IReadOnlyList<T> GetAll()
        {
            lock (_fileDataLock)
            {
                if (_items == null)
                {
                    Logger.Info("Loading live tv data from {0}", _dataPath);
                    _items = GetItemsFromFile(_dataPath);
                }
                return _items.ToList();
            }
        }

        private List<T> GetItemsFromFile(string path)
        {
            var jsonFile = path + ".json";

            try
            {
                return _jsonSerializer.DeserializeFromFile<List<T>>(jsonFile) ?? new List<T>();
            }
            catch (FileNotFoundException)
            {
            }
            catch (DirectoryNotFoundException ex)
            {
            }
            catch (IOException ex)
            {
                Logger.ErrorException("Error deserializing {0}", ex, jsonFile);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error deserializing {0}", ex, jsonFile);
            }
            return new List<T>();
        }

        private void UpdateList(List<T> newList)
        {
            if (newList == null)
            {
                throw new ArgumentNullException("newList");
            }

            var file = _dataPath + ".json";
            _fileSystem.CreateDirectory(Path.GetDirectoryName(file));

            lock (_fileDataLock)
            {
                _jsonSerializer.SerializeToFile(newList, file);
                _items = newList;
            }
        }

        public virtual void Update(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var list = GetAll().ToList();

            var index = list.FindIndex(i => EqualityComparer(i, item));

            if (index == -1)
            {
                throw new ArgumentException("item not found");
            }

            list[index] = item;

            UpdateList(list);
        }

        public virtual void Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("item");
            }

            var list = GetAll().ToList();

            if (list.Any(i => EqualityComparer(i, item)))
            {
                throw new ArgumentException("item already exists");
            }

            list.Add(item);

            UpdateList(list);
        }

        public void AddOrUpdate(T item)
        {
            var list = GetAll().ToList();

            if (!list.Any(i => EqualityComparer(i, item)))
            {
                Add(item);
            }
            else
            {
                Update(item);
            }
        }

        public virtual void Delete(T item)
        {
            var list = GetAll().Where(i => !EqualityComparer(i, item)).ToList();

            UpdateList(list);
        }
    }
}
