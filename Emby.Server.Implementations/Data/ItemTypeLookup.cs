using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading.Channels;
using Emby.Server.Implementations.Playlists;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Playlists;

namespace Emby.Server.Implementations.Data;

/// <inheritdoc />
public class ItemTypeLookup : IItemTypeLookup
{
    /// <inheritdoc />
    public IReadOnlyList<string> MusicGenreTypes { get; } = [
         typeof(Audio).FullName!,
         typeof(MusicVideo).FullName!,
         typeof(MusicAlbum).FullName!,
         typeof(MusicArtist).FullName!,
    ];

    /// <inheritdoc />
    public IReadOnlyDictionary<BaseItemKind, string> BaseItemKindNames { get; } = new Dictionary<BaseItemKind, string>()
    {
        { BaseItemKind.AggregateFolder, typeof(AggregateFolder).FullName! },
        { BaseItemKind.Audio, typeof(Audio).FullName! },
        { BaseItemKind.AudioBook, typeof(AudioBook).FullName! },
        { BaseItemKind.BasePluginFolder, typeof(BasePluginFolder).FullName! },
        { BaseItemKind.Book, typeof(Book).FullName! },
        { BaseItemKind.BoxSet, typeof(BoxSet).FullName! },
        { BaseItemKind.Channel, typeof(Channel).FullName! },
        { BaseItemKind.CollectionFolder, typeof(CollectionFolder).FullName! },
        { BaseItemKind.Episode, typeof(Episode).FullName! },
        { BaseItemKind.Folder, typeof(Folder).FullName! },
        { BaseItemKind.Genre, typeof(Genre).FullName! },
        { BaseItemKind.Movie, typeof(Movie).FullName! },
        { BaseItemKind.LiveTvChannel, typeof(LiveTvChannel).FullName! },
        { BaseItemKind.LiveTvProgram, typeof(LiveTvProgram).FullName! },
        { BaseItemKind.MusicAlbum, typeof(MusicAlbum).FullName! },
        { BaseItemKind.MusicArtist, typeof(MusicArtist).FullName! },
        { BaseItemKind.MusicGenre, typeof(MusicGenre).FullName! },
        { BaseItemKind.MusicVideo, typeof(MusicVideo).FullName! },
        { BaseItemKind.Person, typeof(Person).FullName! },
        { BaseItemKind.Photo, typeof(Photo).FullName! },
        { BaseItemKind.PhotoAlbum, typeof(PhotoAlbum).FullName! },
        { BaseItemKind.Playlist, typeof(Playlist).FullName! },
        { BaseItemKind.PlaylistsFolder, typeof(PlaylistsFolder).FullName! },
        { BaseItemKind.Season, typeof(Season).FullName! },
        { BaseItemKind.Series, typeof(Series).FullName! },
        { BaseItemKind.Studio, typeof(Studio).FullName! },
        { BaseItemKind.Trailer, typeof(Trailer).FullName! },
        { BaseItemKind.TvChannel, typeof(LiveTvChannel).FullName! },
        { BaseItemKind.TvProgram, typeof(LiveTvProgram).FullName! },
        { BaseItemKind.UserRootFolder, typeof(UserRootFolder).FullName! },
        { BaseItemKind.UserView, typeof(UserView).FullName! },
        { BaseItemKind.Video, typeof(Video).FullName! },
        { BaseItemKind.Year, typeof(Year).FullName! }
    }.ToFrozenDictionary();
}
