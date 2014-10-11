using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Devices
{
    public class CameraUploadsFolder : BasePluginFolder
    {
        public CameraUploadsFolder()
        {
            Name = "Camera Uploads";
            DisplayMediaType = "CollectionFolder";
        }

        public override bool IsVisible(User user)
        {
            return GetChildren(user, true).Any() &&
                base.IsVisible(user);
        }

        public override bool IsHidden
        {
            get
            {
                return true;
            }
        }

        public override bool IsHiddenFromUser(User user)
        {
            return false;
        }

        public override string CollectionType
        {
            get { return Model.Entities.CollectionType.Photos; }
        }

        public override string GetClientTypeName()
        {
            return typeof(CollectionFolder).Name;
        }
    }

    public class CameraUploadsDynamicFolder : IVirtualFolderCreator
    {
        private readonly IApplicationPaths _appPaths;

        public CameraUploadsDynamicFolder(IApplicationPaths appPaths)
        {
            _appPaths = appPaths;
        }

        public BasePluginFolder GetFolder()
        {
            var path = Path.Combine(_appPaths.DataPath, "camerauploads");

            Directory.CreateDirectory(path);

            return new CameraUploadsFolder
            {
                Path = path
            };
        }
    }

}
