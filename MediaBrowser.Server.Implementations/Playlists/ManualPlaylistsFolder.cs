using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Playlists;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Playlists
{
    public class PlaylistsFolder : BasePluginFolder
    {
        public PlaylistsFolder()
        {
            Name = "Playlists";
        }

        public override bool IsVisible(User user)
        {
            return base.IsVisible(user) && GetRecursiveChildren(user, false)
                .OfType<Playlist>()
                .Any(i => i.IsVisible(user));
        }

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return RecursiveChildren
                .OfType<Playlist>();
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
            get { return Model.Entities.CollectionType.Playlists; }
        }
    }

    public class PlaylistssDynamicFolder : IVirtualFolderCreator
    {
        private readonly IApplicationPaths _appPaths;

        public PlaylistssDynamicFolder(IApplicationPaths appPaths)
        {
            _appPaths = appPaths;
        }

        public BasePluginFolder GetFolder()
        {
            var path = Path.Combine(_appPaths.DataPath, "playlists");

            Directory.CreateDirectory(path);

            return new PlaylistsFolder
            {
                Path = path
            };
        }
    }
}

