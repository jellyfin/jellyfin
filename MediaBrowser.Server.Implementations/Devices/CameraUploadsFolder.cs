using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using System;
using System.IO;
using System.Linq;
using CommonIO;
using MediaBrowser.Common.IO;

namespace MediaBrowser.Server.Implementations.Devices
{
    public class CameraUploadsFolder : BasePluginFolder
    {
        public CameraUploadsFolder()
        {
            Name = "Camera Uploads";
        }

        public override bool IsVisible(User user)
        {
            if (!user.Policy.EnableAllFolders && !user.Policy.EnabledFolders.Contains(Id.ToString("N"), StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            return base.IsVisible(user) && HasChildren();
        }

        public override string CollectionType
        {
            get { return Model.Entities.CollectionType.Photos; }
        }

        public override string GetClientTypeName()
        {
            return typeof(CollectionFolder).Name;
        }

        private bool? _hasChildren;
        private bool HasChildren()
        {
            if (!_hasChildren.HasValue)
            {
                _hasChildren = LibraryManager.GetItemIds(new InternalItemsQuery { ParentId = Id }).Count > 0;
            }

            return _hasChildren.Value;
        }

        protected override System.Threading.Tasks.Task ValidateChildrenInternal(IProgress<double> progress, System.Threading.CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, Controller.Providers.MetadataRefreshOptions refreshOptions, Controller.Providers.IDirectoryService directoryService)
        {
            _hasChildren = null;
            return base.ValidateChildrenInternal(progress, cancellationToken, recursive, refreshChildMetadata, refreshOptions, directoryService);
        }
    }

    public class CameraUploadsDynamicFolder : IVirtualFolderCreator
    {
        private readonly IApplicationPaths _appPaths;
        private readonly IFileSystem _fileSystem;

        public CameraUploadsDynamicFolder(IApplicationPaths appPaths, IFileSystem fileSystem)
        {
            _appPaths = appPaths;
            _fileSystem = fileSystem;
        }

        public BasePluginFolder GetFolder()
        {
            var path = Path.Combine(_appPaths.DataPath, "camerauploads");

            _fileSystem.CreateDirectory(path);

            return new CameraUploadsFolder
            {
                Path = path
            };
        }
    }

}
