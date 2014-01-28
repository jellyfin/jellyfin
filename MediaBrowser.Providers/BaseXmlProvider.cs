using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Providers;
using System;
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

        protected abstract string GetXmlPath(string path);

        public bool HasChanged(IHasMetadata item, DateTime date)
        {
            var path = GetXmlPath(item.Path);

            return FileSystem.GetLastWriteTimeUtc(path) > date;
        }
    }
}
