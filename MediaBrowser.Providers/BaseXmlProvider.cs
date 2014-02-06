using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Providers;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers
{
    public abstract class BaseXmlProvider<T> : ILocalMetadataProvider<T>, IHasChangeMonitor
        where T : IHasMetadata, new()
    {
        protected IFileSystem FileSystem;

        public async Task<MetadataResult<T>> GetMetadata(ItemInfo info, CancellationToken cancellationToken)
        {
            var result = new MetadataResult<T>();

            var file = GetXmlFile(info);

            if (file == null)
            {
                return result;
            }

            var path = file.FullName;

            await XmlProviderUtils.XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                result.Item = new T();

                Fetch(result.Item, path, cancellationToken);
                result.HasMetadata = true;
            }
            catch (FileNotFoundException)
            {
                result.HasMetadata = false;
            }
            finally
            {
                XmlProviderUtils.XmlParsingResourcePool.Release();
            }

            return result;
        }

        protected abstract void Fetch(T item, string path, CancellationToken cancellationToken);

        protected BaseXmlProvider(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        protected abstract FileInfo GetXmlFile(ItemInfo info);

        public bool HasChanged(IHasMetadata item, DateTime date)
        {
            var file = GetXmlFile(new ItemInfo { IsInMixedFolder = item.IsInMixedFolder, Path = item.Path });

            if (file == null)
            {
                return false;
            }

            return FileSystem.GetLastWriteTimeUtc(file) > date;
        }

        public string Name
        {
            get
            {
                return "Media Browser Xml";
            }
        }
    }

    static class XmlProviderUtils
    {
        internal static readonly SemaphoreSlim XmlParsingResourcePool = new SemaphoreSlim(4, 4);
    }
}
