using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Playlists
{
    public class PlaylistManager : IPlaylistManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _iLibraryMonitor;
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;

        public PlaylistManager(ILibraryManager libraryManager, IFileSystem fileSystem, ILibraryMonitor iLibraryMonitor, ILogger logger, IUserManager userManager)
        {
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _iLibraryMonitor = iLibraryMonitor;
            _logger = logger;
            _userManager = userManager;
        }

        public IEnumerable<Playlist> GetPlaylists(string userId)
        {
            var user = _userManager.GetUserById(new Guid(userId));

            return GetPlaylistsFolder(userId).GetChildren(user, true).OfType<Playlist>();
        }

        public async Task<Playlist> CreatePlaylist(PlaylistCreationOptions options)
        {
            var name = options.Name;

            // Need to use the [boxset] suffix
            // If internet metadata is not found, or if xml saving is off there will be no collection.xml
            // This could cause it to get re-resolved as a plain folder
            var folderName = _fileSystem.GetValidFilename(name) + " [playlist]";

            var parentFolder = GetPlaylistsFolder(null);

            if (parentFolder == null)
            {
                throw new ArgumentException();
            }

            var path = Path.Combine(parentFolder.Path, folderName);

            _iLibraryMonitor.ReportFileSystemChangeBeginning(path);

            try
            {
                Directory.CreateDirectory(path);

                var collection = new Playlist
                {
                    Name = name,
                    Parent = parentFolder,
                    Path = path
                };

                await parentFolder.AddChild(collection, CancellationToken.None).ConfigureAwait(false);

                await collection.RefreshMetadata(new MetadataRefreshOptions(), CancellationToken.None)
                    .ConfigureAwait(false);

                if (options.ItemIdList.Count > 0)
                {
                    await AddToPlaylist(collection.Id.ToString("N"), options.ItemIdList);
                }

                return collection;
            }
            finally
            {
                // Refresh handled internally
                _iLibraryMonitor.ReportFileSystemChangeComplete(path, false);
            }
        }

        public async Task AddToPlaylist(string playlistId, IEnumerable<string> itemIds)
        {
            var collection = _libraryManager.GetItemById(playlistId) as Playlist;

            if (collection == null)
            {
                throw new ArgumentException("No Playlist exists with the supplied Id");
            }

            var list = new List<LinkedChild>();
            var itemList = new List<BaseItem>();

            foreach (var itemId in itemIds)
            {
                var item = _libraryManager.GetItemById(itemId);

                if (item == null)
                {
                    throw new ArgumentException("No item exists with the supplied Id");
                }

                itemList.Add(item);

                list.Add(new LinkedChild
                {
                    Type = LinkedChildType.Manual,
                    ItemId = item.Id
                });
            }

            collection.LinkedChildren.AddRange(list);

            await collection.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
            await collection.RefreshMetadata(CancellationToken.None).ConfigureAwait(false);
        }

        public Task RemoveFromPlaylist(string playlistId, IEnumerable<int> indeces)
        {
            throw new NotImplementedException();
        }

        public Folder GetPlaylistsFolder(string userId)
        {
            return _libraryManager.RootFolder.Children.OfType<PlaylistsFolder>()
                .FirstOrDefault();
        }
    }
}
