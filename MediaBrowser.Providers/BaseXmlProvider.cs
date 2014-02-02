using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Providers;
using System;
using System.IO;
using System.Threading;

namespace MediaBrowser.Providers
{
    public abstract class BaseXmlProvider: IHasChangeMonitor
    {
        protected static readonly SemaphoreSlim XmlParsingResourcePool = new SemaphoreSlim(4, 4);

        protected IFileSystem FileSystem;

        protected BaseXmlProvider(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
        }

        protected abstract FileInfo GetXmlFile(string path);

        public bool HasChanged(IHasMetadata item, DateTime date)
        {
            var file = GetXmlFile(item.Path);

            return FileSystem.GetLastWriteTimeUtc(file) > date;
        }

        public bool HasLocalMetadata(IHasMetadata item)
        {
            return GetXmlFile(item.Path).Exists;
        }
    }
}
