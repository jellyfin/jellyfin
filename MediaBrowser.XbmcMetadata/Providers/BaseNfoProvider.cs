using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.XbmcMetadata.Savers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.XbmcMetadata.Providers
{
    public abstract class BaseNfoProvider<T> : ILocalMetadataProvider<T>, IHasChangeMonitor
        where T : IHasMetadata, new()
    {
        protected IFileSystem FileSystem;

        public async Task<LocalMetadataResult<T>> GetMetadata(ItemInfo info,
            IDirectoryService directoryService,
            CancellationToken cancellationToken)
        {
            var result = new LocalMetadataResult<T>();

            var file = GetXmlFile(info, directoryService);

            if (file == null)
            {
                return result;
            }

            var path = file.FullName;

            try
            {
                result.Item = new T();

                Fetch(result, path, cancellationToken);
                result.HasMetadata = true;
            }
            catch (FileNotFoundException)
            {
                result.HasMetadata = false;
            }
            catch (DirectoryNotFoundException)
            {
                result.HasMetadata = false;
            }

            return result;
        }

        protected abstract void Fetch(LocalMetadataResult<T> result, string path, CancellationToken cancellationToken);

        protected BaseNfoProvider(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        protected abstract FileSystemInfo GetXmlFile(ItemInfo info, IDirectoryService directoryService);

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService, DateTime date)
        {
            var file = GetXmlFile(new ItemInfo { IsInMixedFolder = item.IsInMixedFolder, Path = item.Path }, directoryService);

            if (file == null)
            {
                return false;
            }

            return file.Exists && FileSystem.GetLastWriteTimeUtc(file) > date;
        }

        public string Name
        {
            get
            {
                return BaseNfoSaver.SaverName;
            }
        }
    }

    static class XmlProviderUtils
    {
        internal static readonly SemaphoreSlim XmlParsingResourcePool = new SemaphoreSlim(4, 4);
    }
}
