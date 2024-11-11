#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Playlists;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlaylistsNET.Content;
using PlaylistsNET.Models;
using Genre = MediaBrowser.Controller.Entities.Genre;
using MusicAlbum = MediaBrowser.Controller.Entities.Audio.MusicAlbum;

namespace Emby.Server.Implementations.Playlists
{
    public class PlaylistManager : IPlaylistManager
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IFileSystem _fileSystem;
        private readonly ILibraryMonitor _iLibraryMonitor;
        private readonly ILogger<PlaylistManager> _logger;
        private readonly IUserManager _userManager;
        private readonly IProviderManager _providerManager;
        private readonly IConfiguration _appConfig;

        public PlaylistManager(
            ILibraryManager libraryManager,
            IFileSystem fileSystem,
            ILibraryMonitor iLibraryMonitor,
            ILogger<PlaylistManager> logger,
            IUserManager userManager,
            IProviderManager providerManager,
            IConfiguration appConfig)
        {
            _libraryManager = libraryManager;
            _fileSystem = fileSystem;
            _iLibraryMonitor = iLibraryMonitor;
            _logger = logger;
            _userManager = userManager;
            _providerManager = providerManager;
            _appConfig = appConfig;
        }

        public Playlist GetPlaylistForUser(Guid playlistId, Guid userId)
        {
            return GetPlaylists(userId).Where(p => p.Id.Equals(playlistId)).FirstOrDefault();
        }

        public IEnumerable<Playlist> GetPlaylists(Guid userId)
        {
            var user = _userManager.GetUserById(userId);
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Playlist],
                Recursive = true,
                DtoOptions = new DtoOptions(false)
            })
            .Cast<Playlist>()
            .Where(p => p.IsVisible(user));
        }

        public async Task<PlaylistCreationResult> CreatePlaylist(PlaylistCreationRequest request)
        {
            var name = request.Name;
            var folderName = _fileSystem.GetValidFilename(name);
            var parentFolder = GetPlaylistsFolder(request.UserId);
            if (parentFolder is null)
            {
                throw new ArgumentException(nameof(parentFolder));
            }

            if (request.MediaType is null || request.MediaType == MediaType.Unknown)
            {
                foreach (var itemId in request.ItemIdList)
                {
                    var item = _libraryManager.GetItemById(itemId) ?? throw new ArgumentException("No item exists with the supplied Id");
                    if (item.MediaType != MediaType.Unknown)
                    {
                        request.MediaType = item.MediaType;
                    }
                    else if (item is MusicArtist || item is MusicAlbum || item is MusicGenre)
                    {
                        request.MediaType = MediaType.Audio;
                    }
                    else if (item is Genre)
                    {
                        request.MediaType = MediaType.Video;
                    }
                    else
                    {
                        if (item is Folder folder)
                        {
                            request.MediaType = folder.GetRecursiveChildren(i => !i.IsFolder && i.SupportsAddingToPlaylist)
                                .Select(i => i.MediaType)
                                .FirstOrDefault(i => i != MediaType.Unknown);
                        }
                    }

                    if (request.MediaType is not null && request.MediaType != MediaType.Unknown)
                    {
                        break;
                    }
                }
            }

            if (request.MediaType is null || request.MediaType == MediaType.Unknown)
            {
                request.MediaType = MediaType.Audio;
            }

            var user = _userManager.GetUserById(request.UserId);
            var path = Path.Combine(parentFolder.Path, folderName);
            path = GetTargetPath(path);

            _iLibraryMonitor.ReportFileSystemChangeBeginning(path);

            try
            {
                Directory.CreateDirectory(path);
                var playlist = new Playlist
                {
                    Name = name,
                    Path = path,
                    OwnerUserId = request.UserId,
                    Shares = request.Users ?? [],
                    OpenAccess = request.Public ?? false
                };

                playlist.SetMediaType(request.MediaType);
                parentFolder.AddChild(playlist);

                await playlist.RefreshMetadata(new MetadataRefreshOptions(new DirectoryService(_fileSystem)) { ForceSave = true }, CancellationToken.None)
                    .ConfigureAwait(false);

                if (request.ItemIdList.Count > 0)
                {
                    await AddToPlaylistInternal(playlist.Id, request.ItemIdList, user, new DtoOptions(false)
                    {
                        EnableImages = true
                    }).ConfigureAwait(false);
                }

                return new PlaylistCreationResult(playlist.Id.ToString("N", CultureInfo.InvariantCulture));
            }
            finally
            {
                // Refresh handled internally
                _iLibraryMonitor.ReportFileSystemChangeComplete(path, false);
            }
        }

        private List<Playlist> GetUserPlaylists(Guid userId)
        {
            var user = _userManager.GetUserById(userId);
            var playlistsFolder = GetPlaylistsFolder(userId);
            if (playlistsFolder is null)
            {
                return [];
            }

            return playlistsFolder.GetChildren(user, true).OfType<Playlist>().ToList();
        }

        private static string GetTargetPath(string path)
        {
            while (Directory.Exists(path))
            {
                path += "1";
            }

            return path;
        }

        private IReadOnlyList<BaseItem> GetPlaylistItems(IEnumerable<Guid> itemIds, User user, DtoOptions options)
        {
            var items = itemIds.Select(_libraryManager.GetItemById).Where(i => i is not null);

            return Playlist.GetPlaylistItems(items, user, options);
        }

        public Task AddItemToPlaylistAsync(Guid playlistId, IReadOnlyCollection<Guid> itemIds, Guid userId)
        {
            var user = userId.IsEmpty() ? null : _userManager.GetUserById(userId);

            return AddToPlaylistInternal(playlistId, itemIds, user, new DtoOptions(false)
            {
                EnableImages = true
            });
        }

        private async Task AddToPlaylistInternal(Guid playlistId, IReadOnlyCollection<Guid> newItemIds, User user, DtoOptions options)
        {
            // Retrieve the existing playlist
            var playlist = _libraryManager.GetItemById(playlistId) as Playlist
                ?? throw new ArgumentException("No Playlist exists with Id " + playlistId);

            // Retrieve all the items to be added to the playlist
            var newItems = GetPlaylistItems(newItemIds, user, options)
                .Where(i => i.SupportsAddingToPlaylist);

            // Filter out duplicate items
            var existingIds = playlist.LinkedChildren.Select(c => c.ItemId).ToHashSet();
            newItems = newItems
                .Where(i => !existingIds.Contains(i.Id))
                .Distinct();

            // Create a list of the new linked children to add to the playlist
            var childrenToAdd = newItems
                .Select(LinkedChild.Create)
                .ToList();

            // Log duplicates that have been ignored, if any
            int numDuplicates = newItemIds.Count - childrenToAdd.Count;
            if (numDuplicates > 0)
            {
                _logger.LogWarning("Ignored adding {DuplicateCount} duplicate items to playlist {PlaylistName}.", numDuplicates, playlist.Name);
            }

            // Do nothing else if there are no items to add to the playlist
            if (childrenToAdd.Count == 0)
            {
                return;
            }

            // Update the playlist in the repository
            playlist.LinkedChildren = [.. playlist.LinkedChildren, .. childrenToAdd];

            await UpdatePlaylistInternal(playlist).ConfigureAwait(false);

            // Refresh playlist metadata
            _providerManager.QueueRefresh(
                playlist.Id,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                {
                    ForceSave = true
                },
                RefreshPriority.High);
        }

        public async Task RemoveItemFromPlaylistAsync(string playlistId, IEnumerable<string> entryIds)
        {
            if (_libraryManager.GetItemById(playlistId) is not Playlist playlist)
            {
                throw new ArgumentException("No Playlist exists with the supplied Id");
            }

            var children = playlist.GetManageableItems().ToList();

            var idList = entryIds.ToList();

            var removals = children.Where(i => idList.Contains(i.Item1.ItemId?.ToString("N", CultureInfo.InvariantCulture)));

            playlist.LinkedChildren = children.Except(removals)
                .Select(i => i.Item1)
                .ToArray();

            await UpdatePlaylistInternal(playlist).ConfigureAwait(false);

            _providerManager.QueueRefresh(
                playlist.Id,
                new MetadataRefreshOptions(new DirectoryService(_fileSystem))
                {
                    ForceSave = true
                },
                RefreshPriority.High);
        }

        public async Task MoveItemAsync(string playlistId, string entryId, int newIndex, Guid callingUserId)
        {
            if (_libraryManager.GetItemById(playlistId) is not Playlist playlist)
            {
                throw new ArgumentException("No Playlist exists with the supplied Id");
            }

            var user = _userManager.GetUserById(callingUserId);
            var children = playlist.GetManageableItems().ToList();
            var accessibleChildren = children.Where(c => c.Item2.IsVisible(user)).ToArray();

            var oldIndexAll = children.FindIndex(i => string.Equals(entryId, i.Item1.ItemId?.ToString("N", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase));
            var oldIndexAccessible = accessibleChildren.FindIndex(i => string.Equals(entryId, i.Item1.ItemId?.ToString("N", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase));

            if (oldIndexAccessible == newIndex)
            {
                return;
            }

            var newPriorItemIndex = newIndex > oldIndexAccessible ? newIndex : newIndex - 1 < 0 ? 0 : newIndex - 1;
            var newPriorItemId = accessibleChildren[newPriorItemIndex].Item1.ItemId;
            var newPriorItemIndexOnAllChildren = children.FindIndex(c => c.Item1.ItemId.Equals(newPriorItemId));
            var adjustedNewIndex = newPriorItemIndexOnAllChildren + 1;

            var item = playlist.LinkedChildren.FirstOrDefault(i => string.Equals(entryId, i.ItemId?.ToString("N", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase));
            if (item is null)
            {
                _logger.LogWarning("Modified item not found in playlist. ItemId: {ItemId}, PlaylistId: {PlaylistId}", item.ItemId, playlistId);

                return;
            }

            var newList = playlist.LinkedChildren.ToList();
            newList.Remove(item);

            if (newIndex >= newList.Count)
            {
                newList.Add(item);
            }
            else
            {
                newList.Insert(adjustedNewIndex, item);
            }

            playlist.LinkedChildren = [.. newList];

            await UpdatePlaylistInternal(playlist).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public void SavePlaylistFile(Playlist item)
        {
            // this is probably best done as a metadata provider
            // saving a file over itself will require some work to prevent this from happening when not needed
            var playlistPath = item.Path;
            var extension = Path.GetExtension(playlistPath.AsSpan());

            if (extension.Equals(".wpl", StringComparison.OrdinalIgnoreCase))
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

                    if (child is IHasAlbumArtist hasAlbumArtist)
                    {
                        entry.AlbumArtist = hasAlbumArtist.AlbumArtists.Count > 0 ? hasAlbumArtist.AlbumArtists[0] : null;
                    }

                    if (child is IHasArtist hasArtist)
                    {
                        entry.TrackArtist = hasArtist.Artists.Count > 0 ? hasArtist.Artists[0] : null;
                    }

                    if (child.RunTimeTicks.HasValue)
                    {
                        entry.Duration = TimeSpan.FromTicks(child.RunTimeTicks.Value);
                    }

                    playlist.PlaylistEntries.Add(entry);
                }

                string text = new WplContent().ToText(playlist);
                File.WriteAllText(playlistPath, text);
            }
            else if (extension.Equals(".zpl", StringComparison.OrdinalIgnoreCase))
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

                    if (child is IHasAlbumArtist hasAlbumArtist)
                    {
                        entry.AlbumArtist = hasAlbumArtist.AlbumArtists.Count > 0 ? hasAlbumArtist.AlbumArtists[0] : null;
                    }

                    if (child is IHasArtist hasArtist)
                    {
                        entry.TrackArtist = hasArtist.Artists.Count > 0 ? hasArtist.Artists[0] : null;
                    }

                    if (child.RunTimeTicks.HasValue)
                    {
                        entry.Duration = TimeSpan.FromTicks(child.RunTimeTicks.Value);
                    }

                    playlist.PlaylistEntries.Add(entry);
                }

                string text = new ZplContent().ToText(playlist);
                File.WriteAllText(playlistPath, text);
            }
            else if (extension.Equals(".m3u", StringComparison.OrdinalIgnoreCase))
            {
                var playlist = new M3uPlaylist
                {
                    IsExtended = true
                };
                foreach (var child in item.GetLinkedChildren())
                {
                    var entry = new M3uPlaylistEntry()
                    {
                        Path = NormalizeItemPath(playlistPath, child.Path),
                        Title = child.Name,
                        Album = child.Album
                    };

                    if (child is IHasAlbumArtist hasAlbumArtist)
                    {
                        entry.AlbumArtist = hasAlbumArtist.AlbumArtists.Count > 0 ? hasAlbumArtist.AlbumArtists[0] : null;
                    }

                    if (child.RunTimeTicks.HasValue)
                    {
                        entry.Duration = TimeSpan.FromTicks(child.RunTimeTicks.Value);
                    }

                    playlist.PlaylistEntries.Add(entry);
                }

                string text = new M3uContent().ToText(playlist);
                File.WriteAllText(playlistPath, text);
            }
            else if (extension.Equals(".m3u8", StringComparison.OrdinalIgnoreCase))
            {
                var playlist = new M3uPlaylist
                {
                    IsExtended = true
                };

                foreach (var child in item.GetLinkedChildren())
                {
                    var entry = new M3uPlaylistEntry()
                    {
                        Path = NormalizeItemPath(playlistPath, child.Path),
                        Title = child.Name,
                        Album = child.Album
                    };

                    if (child is IHasAlbumArtist hasAlbumArtist)
                    {
                        entry.AlbumArtist = hasAlbumArtist.AlbumArtists.Count > 0 ? hasAlbumArtist.AlbumArtists[0] : null;
                    }

                    if (child.RunTimeTicks.HasValue)
                    {
                        entry.Duration = TimeSpan.FromTicks(child.RunTimeTicks.Value);
                    }

                    playlist.PlaylistEntries.Add(entry);
                }

                string text = new M3uContent().ToText(playlist);
                File.WriteAllText(playlistPath, text);
            }
            else if (extension.Equals(".pls", StringComparison.OrdinalIgnoreCase))
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

                string text = new PlsContent().ToText(playlist);
                File.WriteAllText(playlistPath, text);
            }
        }

        private static string NormalizeItemPath(string playlistPath, string itemPath)
        {
            return MakeRelativePath(Path.GetDirectoryName(playlistPath), itemPath);
        }

        private static string MakeRelativePath(string folderPath, string fileAbsolutePath)
        {
            ArgumentException.ThrowIfNullOrEmpty(folderPath);
            ArgumentException.ThrowIfNullOrEmpty(fileAbsolutePath);

            if (!folderPath.EndsWith(Path.DirectorySeparatorChar))
            {
                folderPath += Path.DirectorySeparatorChar;
            }

            var folderUri = new Uri(folderPath);
            var fileAbsoluteUri = new Uri(fileAbsolutePath);

            // path can't be made relative
            if (folderUri.Scheme != fileAbsoluteUri.Scheme)
            {
                return fileAbsolutePath;
            }

            var relativeUri = folderUri.MakeRelativeUri(fileAbsoluteUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (fileAbsoluteUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }

        /// <inheritdoc />
        public Folder GetPlaylistsFolder()
        {
            return GetPlaylistsFolder(Guid.Empty);
        }

        /// <inheritdoc />
        public Folder GetPlaylistsFolder(Guid userId)
        {
            const string TypeName = "PlaylistsFolder";

            return _libraryManager.RootFolder.Children.OfType<Folder>().FirstOrDefault(i => string.Equals(i.GetType().Name, TypeName, StringComparison.Ordinal)) ??
                _libraryManager.GetUserRootFolder().Children.OfType<Folder>().FirstOrDefault(i => string.Equals(i.GetType().Name, TypeName, StringComparison.Ordinal));
        }

        /// <inheritdoc />
        public async Task RemovePlaylistsAsync(Guid userId)
        {
            var playlists = GetUserPlaylists(userId);
            foreach (var playlist in playlists)
            {
                // Update owner if shared
                var rankedShares = playlist.Shares.OrderByDescending(x => x.CanEdit).ToList();
                if (rankedShares.Count > 0)
                {
                    playlist.OwnerUserId = rankedShares[0].UserId;
                    playlist.Shares = rankedShares.Skip(1).ToArray();
                    await UpdatePlaylistInternal(playlist).ConfigureAwait(false);
                }
                else if (!playlist.OpenAccess)
                {
                    // Remove playlist if not shared
                    _libraryManager.DeleteItem(
                        playlist,
                        new DeleteOptions
                        {
                            DeleteFileLocation = false,
                            DeleteFromExternalProvider = false
                        },
                        playlist.GetParent(),
                        false);
                }
            }
        }

        public async Task UpdatePlaylist(PlaylistUpdateRequest request)
        {
            var playlist = GetPlaylistForUser(request.Id, request.UserId);

            if (request.Ids is not null)
            {
                playlist.LinkedChildren = [];
                await UpdatePlaylistInternal(playlist).ConfigureAwait(false);

                var user = _userManager.GetUserById(request.UserId);
                await AddToPlaylistInternal(request.Id, request.Ids, user, new DtoOptions(false)
                {
                    EnableImages = true
                }).ConfigureAwait(false);

                playlist = GetPlaylistForUser(request.Id, request.UserId);
            }

            if (request.Name is not null)
            {
                playlist.Name = request.Name;
            }

            if (request.Users is not null)
            {
                playlist.Shares = request.Users;
            }

            if (request.Public is not null)
            {
                playlist.OpenAccess = request.Public.Value;
            }

            await UpdatePlaylistInternal(playlist).ConfigureAwait(false);
        }

        public async Task AddUserToShares(PlaylistUserUpdateRequest request)
        {
            var userId = request.UserId;
            var playlist = GetPlaylistForUser(request.Id, userId);
            var shares = playlist.Shares.ToList();
            var existingUserShare = shares.FirstOrDefault(s => s.UserId.Equals(userId));
            if (existingUserShare is not null)
            {
                shares.Remove(existingUserShare);
            }

            shares.Add(new PlaylistUserPermissions(userId, request.CanEdit ?? false));
            playlist.Shares = shares;
            await UpdatePlaylistInternal(playlist).ConfigureAwait(false);
        }

        public async Task RemoveUserFromShares(Guid playlistId, Guid userId, PlaylistUserPermissions share)
        {
            var playlist = GetPlaylistForUser(playlistId, userId);
            var shares = playlist.Shares.ToList();
            shares.Remove(share);
            playlist.Shares = shares;
            await UpdatePlaylistInternal(playlist).ConfigureAwait(false);
        }

        private async Task UpdatePlaylistInternal(Playlist playlist)
        {
            await playlist.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);

            if (playlist.IsFile)
            {
                SavePlaylistFile(playlist);
            }
        }
    }
}
