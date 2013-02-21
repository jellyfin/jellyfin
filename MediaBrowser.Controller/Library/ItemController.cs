using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Common.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Library
{
    public class ItemController
    {

        /// <summary>
        /// Resolves a path into a BaseItem
        /// </summary>
        public async Task<BaseItem> GetItem(string path, Folder parent = null, WIN32_FIND_DATA? fileInfo = null, bool allowInternetProviders = true)
        {
            var args = new ItemResolveEventArgs
            {
                FileInfo = fileInfo ?? FileData.GetFileData(path),
                Parent = parent,
                Cancel = false,
                Path = path
            };

            // Gather child folder and files
            if (args.IsDirectory)
            {
                args.FileSystemChildren = FileData.GetFileSystemEntries(path, "*").ToArray();

                bool isVirtualFolder = parent != null && parent.IsRoot;
                args = FileSystemHelper.FilterChildFileSystemEntries(args, isVirtualFolder);
            }
            else
            {
                args.FileSystemChildren = new WIN32_FIND_DATA[] { };
            }


            // Check to see if we should resolve based on our contents
            if (!EntityResolutionHelper.ShouldResolvePathContents(args))
            {
                return null;
            }

            BaseItem item = Kernel.Instance.ResolveItem(args);

            return item;
        }

        /// <summary>
        /// Gets a Person
        /// </summary>
        public Task<Person> GetPerson(string name)
        {
            return GetImagesByNameItem<Person>(Kernel.Instance.ApplicationPaths.PeoplePath, name);
        }

        /// <summary>
        /// Gets a Studio
        /// </summary>
        public Task<Studio> GetStudio(string name)
        {
            return GetImagesByNameItem<Studio>(Kernel.Instance.ApplicationPaths.StudioPath, name);
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        public Task<Genre> GetGenre(string name)
        {
            return GetImagesByNameItem<Genre>(Kernel.Instance.ApplicationPaths.GenrePath, name);
        }

        /// <summary>
        /// Gets a Year
        /// </summary>
        public Task<Year> GetYear(int value)
        {
            return GetImagesByNameItem<Year>(Kernel.Instance.ApplicationPaths.YearPath, value.ToString());
        }

        private readonly ConcurrentDictionary<string, object> ImagesByNameItemCache = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Generically retrieves an IBN item
        /// </summary>
        private Task<T> GetImagesByNameItem<T>(string path, string name)
            where T : BaseEntity, new()
        {
            name = FileData.GetValidFilename(name);

            path = Path.Combine(path, name);

            // Look for it in the cache, if it's not there, create it
            if (!ImagesByNameItemCache.ContainsKey(path))
            {
                ImagesByNameItemCache[path] = CreateImagesByNameItem<T>(path, name);
            }

            return ImagesByNameItemCache[path] as Task<T>;
        }

        /// <summary>
        /// Creates an IBN item based on a given path
        /// </summary>
        private async Task<T> CreateImagesByNameItem<T>(string path, string name)
            where T : BaseEntity, new()
        {
            var item = new T { };

            item.Name = name;
            item.Id = path.GetMD5();

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            item.DateCreated = Directory.GetCreationTimeUtc(path);
            item.DateModified = Directory.GetLastWriteTimeUtc(path);

            var args = new ItemResolveEventArgs { };
            args.FileInfo = FileData.GetFileData(path);
            args.FileSystemChildren = FileData.GetFileSystemEntries(path, "*").ToArray();

            await Kernel.Instance.ExecuteMetadataProviders(item).ConfigureAwait(false);

            return item;
        }
    }
}
