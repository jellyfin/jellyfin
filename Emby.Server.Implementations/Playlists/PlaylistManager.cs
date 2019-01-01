using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Playlists;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Extensions;
using PlaylistsNET;
using PlaylistsNET.Content;
using PlaylistsNET.Models;
using PlaylistsNET.Utils;

namespace Emby.Server.Implementations.Playlists
{
    public class PlaylistManager : IPlaylistManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _iLibraryMonitor;
        private readonly ILogger _logger;
        private readonly IUserManager _userManager;
        private readonly IProviderManager _providerManager;

        public PlaylistManager(ILibraryManager libraryManager, IFileSystem fileSystem, ILibraryMonitor iLibraryMonitor, ILogger logger, IUserManager userManager, IProviderManager providerManager)
        {
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _iLibraryMonitor = iLibraryMonitor;
            _logger = logger;
            _userManager = userManager;
            _providerManager = providerManager;
        }

        public IEnumerable<Playlist> GetPlaylists(Guid userId)
        {
            var user = _userManager.GetUserById(userId);

            return GetPlaylistsFolder(userId).GetChildren(user, true).OfType<Playlist>();
        }

        public async Task<PlaylistCreationResult> CreatePlaylist(PlaylistCreationRequest options)
        {
            var name = options.Name;

            var folderName = _fileSystem.GetValidFilename(name) + " [playlist]";

            var parentFolder = GetPlaylistsFolder(Guid.Empty);

            if (parentFolder == null)
            {
                throw new ArgumentException();
            }

            if (string.IsNullOrEmpty(options.MediaType))
            {
                foreach (var itemId in options.ItemIdList)
                {
                    var item = _libraryManager.GetItemById(itemId);

                    if (item == null)
                    {
                        throw new ArgumentException("No item exists with the supplied Id");
                    }

                    if (!string.IsNullOrEmpty(item.MediaType))
                    {
                        options.MediaType = item.MediaType;
                    }
                    else if (item is MusicArtist || item is MusicAlbum || item is MusicGenre)
                    {
                        options.MediaType = MediaType.Audio;
                    }
                    else if (item is Genre)
                    {
                        options.MediaType = MediaType.Video;
                    }
                    else
                    {
                        var folder = item as Folder;
                        if (folder != null)
                        {
                            options.MediaType = folder.GetRecursiveChildren(i => !i.IsFolder && i.SupportsAddingToPlaylist)
                                .Select(i => i.MediaType)
                                .FirstOrDefault(i => !string.IsNullOrEmpty(i));
                        }
                    }

                    if (!string.IsNullOrEmpty(options.MediaType))
                    {
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(options.MediaType))
            {
                options.MediaType = "Audio";
            }

            var user = _userManager.GetUserById(options.UserId);

            var path = Path.Combine(parentFolder.Path, folderName);
            path = GetTargetPath(path);

            _iLibraryMonitor.ReportFileSystemChangeBeginning(path);

            try
            {
                _fileSystem.CreateDirectory(path);

                var playlist = new Playlist
                {
                    Name = name,
                    Path = path,
                    Shares = new[]
                    {
                        new Share
                        {
                            UserId = options.UserId.Equals(Guid.Empty) ? null : options.UserId.ToString("N"),
                            CanEdit = true
                        }
                    }
                };

                playlist.SetMediaType(options.MediaType);

                parentFolder.AddChild(playlist, CancellationToken.None);

                await playlist.RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem)) { ForceSave = true }, CancellationToken.None)
                    .ConfigureAwait(false);

                if (options.ItemIdList.Length > 0)
                {
                    AddToPlaylistInternal(playlist.Id.ToString("N"), options.ItemIdList, user, new DtoOptions(false)
                    {
                        EnableImages = true
                    });
                }

                return new PlaylistCreationResult
                {
                    Id = playlist.Id.ToString("N")
                };
            }
            finally
            {
                // Refresh handled internally
                _iLibraryMonitor.ReportFileSystemChangeComplete(path, false);
            }
        }

        private string GetTargetPath(string path)
        {
            while (_fileSystem.DirectoryExists(path))
            {
                path += "1";
            }

            return path;
        }

        private List<BaseItem> GetPlaylistItems(IEnumerable<Guid> itemIds, string playlistMediaType, User user, DtoOptions options)
        {
            var items = itemIds.Select(i => _libraryManager.GetItemById(i)).Where(i => i != null);

            return Playlist.GetPlaylistItems(playlistMediaType, items, user, options);
        }

        public void AddToPlaylist(string playlistId, IEnumerable<Guid> itemIds, Guid userId)
        {
            var user = userId.Equals(Guid.Empty) ? null : _userManager.GetUserById(userId);

            AddToPlaylistInternal(playlistId, itemIds, user, new DtoOptions(false)
            {
                EnableImages = true
            });
        }

        private void AddToPlaylistInternal(string playlistId, IEnumerable<Guid> itemIds, User user, DtoOptions options)
        {
            var playlist = _libraryManager.GetItemById(playlistId) as Playlist;

            if (playlist == null)
            {
                throw new ArgumentException("No Playlist exists with the supplied Id");
            }

            var list = new List<LinkedChild>();

            var items = (GetPlaylistItems(itemIds, playlist.MediaType, user, options))
                .Where(i => i.SupportsAddingToPlaylist)
                .ToList();

            foreach (var item in items)
            {
                list.Add(LinkedChild.Create(item));
            }

            var newList = playlist.LinkedChildren.ToList();
            newList.AddRange(list);
            playlist.LinkedChildren = newList.ToArray();

            playlist.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);

            if (playlist.IsFile)
            {
                SavePlaylistFile(playlist);
            }

            _providerManager.QueueRefresh(playlist.Id, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem))
            {
                ForceSave = true

            }, RefreshPriority.High);
        }

        public void RemoveFromPlaylist(string playlistId, IEnumerable<string> entryIds)
        {
            var playlist = _libraryManager.GetItemById(playlistId) as Playlist;

            if (playlist == null)
            {
                throw new ArgumentException("No Playlist exists with the supplied Id");
            }

            var children = playlist.GetManageableItems().ToList();

            var idList = entryIds.ToList();

            var removals = children.Where(i => idList.Contains(i.Item1.Id));

            playlist.LinkedChildren = children.Except(removals)
                .Select(i => i.Item1)
                .ToArray();

            playlist.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);

            if (playlist.IsFile)
            {
                SavePlaylistFile(playlist);
            }

            _providerManager.QueueRefresh(playlist.Id, new MetadataRefreshOptions(new DirectoryService(_logger, _fileSystem))
            {
                ForceSave = true

            }, RefreshPriority.High);
        }

        public void MoveItem(string playlistId, string entryId, int newIndex)
        {
            var playlist = _libraryManager.GetItemById(playlistId) as Playlist;

            if (playlist == null)
            {
                throw new ArgumentException("No Playlist exists with the supplied Id");
            }

            var children = playlist.GetManageableItems().ToList();

            var oldIndex = children.FindIndex(i => string.Equals(entryId, i.Item1.Id, StringComparison.OrdinalIgnoreCase));

            if (oldIndex == newIndex)
            {
                return;
            }

            var item = playlist.LinkedChildren[oldIndex];

            var newList = playlist.LinkedChildren.ToList();

            newList.Remove(item);

            if (newIndex >= newList.Count)
            {
                newList.Add(item);
            }
            else
            {
                newList.Insert(newIndex, item);
            }

            playlist.LinkedChildren = newList.ToArray();

            playlist.UpdateToRepository(ItemUpdateType.MetadataEdit, CancellationToken.None);

            if (playlist.IsFile)
            {
                SavePlaylistFile(playlist);
            }
        }

        private void SavePlaylistFile(Playlist item)
        {
            // This is probably best done as a metatata provider, but saving a file over itself will first require some core work to prevent this from happening when not needed
            var playlistPath = item.Path;
            var extension = Path.GetExtension(playlistPath);

            if (string.Equals(".wpl", extension, StringComparison.OrdinalIgnoreCase))
            {
                var playlist = new WplPlaylist();
                foreach (var child in item.GetLinkedChildren())
                {
                    var entry = new WplPlaylistEntry()
                    {
                        Path = NormalizeItemPath(playlistPath, child.Path),
                        TrackTitle = child.Name,
                        AlbumTitle = child.Album
                    };

                    var hasAlbumArtist = child as IHasAlbumArtist;
                    if (hasAlbumArtist != null)
                    {
                        entry.AlbumArtist = hasAlbumArtist.AlbumArtists.FirstOrDefault();
                    }

                    var hasArtist = child as IHasArtist;
                    if (hasArtist != null)
                    {
                        entry.TrackArtist = hasArtist.Artists.FirstOrDefault();
                    }

                    if (child.RunTimeTicks.HasValue)
                    {
                        entry.Duration = TimeSpan.FromTicks(child.RunTimeTicks.Value);
                    }
                    playlist.PlaylistEntries.Add(entry);
                }

                _fileSystem.WriteAllText(playlistPath, new WplContent().ToText(playlist));
            }
            if (string.Equals(".zpl", extension, StringComparison.OrdinalIgnoreCase))
            {
                var playlist = new ZplPlaylist();
                foreach (var child in item.GetLinkedChildren())
                {
                    var entry = new ZplPlaylistEntry()
                    {
                        Path = NormalizeItemPath(playlistPath, child.Path),
                        TrackTitle = child.Name,
                        AlbumTitle = child.Album
                    };

                    var hasAlbumArtist = child as IHasAlbumArtist;
                    if (hasAlbumArtist != null)
                    {
                        entry.AlbumArtist = hasAlbumArtist.AlbumArtists.FirstOrDefault();
                    }

                    var hasArtist = child as IHasArtist;
                    if (hasArtist != null)
                    {
                        entry.TrackArtist = hasArtist.Artists.FirstOrDefault();
                    }

                    if (child.RunTimeTicks.HasValue)
                    {
                        entry.Duration = TimeSpan.FromTicks(child.RunTimeTicks.Value);
                    }
                    playlist.PlaylistEntries.Add(entry);
                }

                _fileSystem.WriteAllText(playlistPath, new ZplContent().ToText(playlist));
            }
            if (string.Equals(".m3u", extension, StringComparison.OrdinalIgnoreCase))
            {
                var playlist = new M3uPlaylist();
                playlist.IsExtended = true;
                foreach (var child in item.GetLinkedChildren())
                {
                    var entry = new M3uPlaylistEntry()
                    {
                        Path = NormalizeItemPath(playlistPath, child.Path),
                        Title = child.Name,
                        Album = child.Album
                    };

                    var hasAlbumArtist = child as IHasAlbumArtist;
                    if (hasAlbumArtist != null)
                    {
                        entry.AlbumArtist = hasAlbumArtist.AlbumArtists.FirstOrDefault();
                    }

                    if (child.RunTimeTicks.HasValue)
                    {
                        entry.Duration = TimeSpan.FromTicks(child.RunTimeTicks.Value);
                    }
                    playlist.PlaylistEntries.Add(entry);
                }

                _fileSystem.WriteAllText(playlistPath, new M3uContent().ToText(playlist));
            }
            if (string.Equals(".m3u8", extension, StringComparison.OrdinalIgnoreCase))
            {
                var playlist = new M3uPlaylist();
                playlist.IsExtended = true;
                foreach (var child in item.GetLinkedChildren())
                {
                    var entry = new M3uPlaylistEntry()
                    {
                        Path = NormalizeItemPath(playlistPath, child.Path),
                        Title = child.Name,
                        Album = child.Album
                    };

                    var hasAlbumArtist = child as IHasAlbumArtist;
                    if (hasAlbumArtist != null)
                    {
                        entry.AlbumArtist = hasAlbumArtist.AlbumArtists.FirstOrDefault();
                    }

                    if (child.RunTimeTicks.HasValue)
                    {
                        entry.Duration = TimeSpan.FromTicks(child.RunTimeTicks.Value);
                    }
                    playlist.PlaylistEntries.Add(entry);
                }

                _fileSystem.WriteAllText(playlistPath, new M3u8Content().ToText(playlist));
            }
            if (string.Equals(".pls", extension, StringComparison.OrdinalIgnoreCase))
            {
                var playlist = new PlsPlaylist();
                foreach (var child in item.GetLinkedChildren())
                {
                    var entry = new PlsPlaylistEntry()
                    {
                        Path = NormalizeItemPath(playlistPath, child.Path),
                        Title = child.Name
                    };

                    if (child.RunTimeTicks.HasValue)
                    {
                        entry.Length = TimeSpan.FromTicks(child.RunTimeTicks.Value);
                    }
                    playlist.PlaylistEntries.Add(entry);
                }

                _fileSystem.WriteAllText(playlistPath, new PlsContent().ToText(playlist));
            }
        }

        private string NormalizeItemPath(string playlistPath, string itemPath)
        {
            return MakeRelativePath(_fileSystem.GetDirectoryName(playlistPath), itemPath);
        }

        private static String MakeRelativePath(string folderPath, string fileAbsolutePath)
        {
            if (String.IsNullOrEmpty(folderPath)) throw new ArgumentNullException("folderPath");
            if (String.IsNullOrEmpty(fileAbsolutePath)) throw new ArgumentNullException("filePath");

            if (!folderPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folderPath = folderPath + Path.DirectorySeparatorChar;
            }

            Uri folderUri = new Uri(folderPath);
            Uri fileAbsoluteUri = new Uri(fileAbsolutePath);

            if (folderUri.Scheme != fileAbsoluteUri.Scheme) { return fileAbsolutePath; } // path can't be made relative.

            Uri relativeUri = folderUri.MakeRelativeUri(fileAbsoluteUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (fileAbsoluteUri.Scheme.Equals("file", StringComparison.CurrentCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        private static string UnEscape(string content)
        {
            if (content == null) return content;
            return content.Replace("&amp;", "&").Replace("&apos;", "'").Replace("&quot;", "\"").Replace("&gt;", ">").Replace("&lt;", "<");
        }

        private static string Escape(string content)
        {
            if (content == null) return null;
            return content.Replace("&", "&amp;").Replace("'", "&apos;").Replace("\"", "&quot;").Replace(">", "&gt;").Replace("<", "&lt;");
        }

        public Folder GetPlaylistsFolder(Guid userId)
        {
            var typeName = "PlaylistsFolder";

            return _libraryManager.RootFolder.Children.OfType<Folder>().FirstOrDefault(i => string.Equals(i.GetType().Name, typeName, StringComparison.Ordinal)) ??
                _libraryManager.GetUserRootFolder().Children.OfType<Folder>().FirstOrDefault(i => string.Equals(i.GetType().Name, typeName, StringComparison.Ordinal));
        }
    }
}
