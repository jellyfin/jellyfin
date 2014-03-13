using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Collections
{
    public class CollectionsDynamicFolder : IVirtualFolderCreator
    {
        private readonly IApplicationPaths _appPaths;

        public CollectionsDynamicFolder(IApplicationPaths appPaths)
        {
            _appPaths = appPaths;
        }

        public BasePluginFolder GetFolder()
        {
            var path = Path.Combine(_appPaths.DataPath, "collections");

            Directory.CreateDirectory(path);

            return new ManualCollectionsFolder
            {
                Path = path
            };
        }
    }

    public class ManualCollectionsFolder : BasePluginFolder
    {
        public ManualCollectionsFolder()
        {
            Name = "Collections";
        }

        public override bool IsVisible(User user)
        {
            if (!GetChildren(user, true).Any())
            {
                return false;
            }

            return base.IsVisible(user);
        }

        public override bool IsHidden
        {
            get
            {
                return !ActualChildren.Any() || base.IsHidden;
            }
        }
    }
}
