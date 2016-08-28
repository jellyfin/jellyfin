using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.XbmcMetadata.Savers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.XbmcMetadata.Providers
{
    public abstract class BaseNfoProvider<T> : ILocalMetadataProvider<T>, IHasItemChangeMonitor
        where T : IHasMetadata, new()
    {
        protected IFileSystem FileSystem;

        public async Task<MetadataResult<T>> GetMetadata(ItemInfo info,
            IDirectoryService directoryService,
            CancellationToken cancellationToken)
        {
            var result = new MetadataResult<T>();

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

        protected abstract void Fetch(MetadataResult<T> result, string path, CancellationToken cancellationToken);

        protected BaseNfoProvider(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        protected abstract FileSystemMetadata GetXmlFile(ItemInfo info, IDirectoryService directoryService);

        public bool HasChanged(IHasMetadata item, IDirectoryService directoryService)
        {
            var file = GetXmlFile(new ItemInfo(item), directoryService);

            if (file == null)
            {
                return false;
            }

            return file.Exists && FileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
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
