using System;
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
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Data;

/// <summary>
/// Provides static topic based lookups for the BaseItemKind.
/// </summary>
public class ItemTypeLookup : IItemTypeLookup
{
    /// <summary>
    /// Gets all values of the ItemFields type.
    /// </summary>
    public IReadOnlyList<ItemFields> AllItemFields { get; } = Enum.GetValues<ItemFields>();

    /// <summary>
    /// Gets all BaseItemKinds that are considered Programs.
    /// </summary>
    public IReadOnlyList<BaseItemKind> ProgramTypes { get; } =
    [
            BaseItemKind.Program,
            BaseItemKind.TvChannel,
            BaseItemKind.LiveTvProgram,
            BaseItemKind.LiveTvChannel
    ];

    /// <summary>
    /// Gets all BaseItemKinds that should be excluded from parent lookup.
    /// </summary>
    public IReadOnlyList<BaseItemKind> ProgramExcludeParentTypes { get; } =
    [
            BaseItemKind.Series,
            BaseItemKind.Season,
            BaseItemKind.MusicAlbum,
            BaseItemKind.MusicArtist,
            BaseItemKind.PhotoAlbum
    ];

    /// <summary>
    /// Gets all BaseItemKinds that are considered to be provided by services.
    /// </summary>
    public IReadOnlyList<BaseItemKind> ServiceTypes { get; } =
    [
            BaseItemKind.TvChannel,
            BaseItemKind.LiveTvChannel
    ];

    /// <summary>
    /// Gets all BaseItemKinds that have a StartDate.
    /// </summary>
    public IReadOnlyList<BaseItemKind> StartDateTypes { get; } =
    [
            BaseItemKind.Program,
            BaseItemKind.LiveTvProgram
    ];

    /// <summary>
    /// Gets all BaseItemKinds that are considered Series.
    /// </summary>
    public IReadOnlyList<BaseItemKind> SeriesTypes { get; } =
    [
            BaseItemKind.Book,
            BaseItemKind.AudioBook,
            BaseItemKind.Episode,
            BaseItemKind.Season
    ];

    /// <summary>
    /// Gets all BaseItemKinds that are not to be evaluated for Artists.
    /// </summary>
    public IReadOnlyList<BaseItemKind> ArtistExcludeParentTypes { get; } =
    [
            BaseItemKind.Series,
            BaseItemKind.Season,
            BaseItemKind.PhotoAlbum
    ];

    /// <summary>
    /// Gets all BaseItemKinds that are considered Artists.
    /// </summary>
    public IReadOnlyList<BaseItemKind> ArtistsTypes { get; } =
    [
            BaseItemKind.Audio,
            BaseItemKind.MusicAlbum,
            BaseItemKind.MusicVideo,
            BaseItemKind.AudioBook
    ];

    /// <summary>
    /// Gets mapping for all BaseItemKinds and their expected serialisaition target.
    /// </summary>
    public IDictionary<BaseItemKind, string?> BaseItemKindNames { get; } = new Dictionary<BaseItemKind, string?>()
    {
        { BaseItemKind.AggregateFolder, typeof(AggregateFolder).FullName },
        { BaseItemKind.Audio, typeof(Audio).FullName },
        { BaseItemKind.AudioBook, typeof(AudioBook).FullName },
        { BaseItemKind.BasePluginFolder, typeof(BasePluginFolder).FullName },
        { BaseItemKind.Book, typeof(Book).FullName },
        { BaseItemKind.BoxSet, typeof(BoxSet).FullName },
        { BaseItemKind.Channel, typeof(Channel).FullName },
        { BaseItemKind.CollectionFolder, typeof(CollectionFolder).FullName },
        { BaseItemKind.Episode, typeof(Episode).FullName },
        { BaseItemKind.Folder, typeof(Folder).FullName },
        { BaseItemKind.Genre, typeof(Genre).FullName },
        { BaseItemKind.Movie, typeof(Movie).FullName },
        { BaseItemKind.LiveTvChannel, typeof(LiveTvChannel).FullName },
        { BaseItemKind.LiveTvProgram, typeof(LiveTvProgram).FullName },
        { BaseItemKind.MusicAlbum, typeof(MusicAlbum).FullName },
        { BaseItemKind.MusicArtist, typeof(MusicArtist).FullName },
        { BaseItemKind.MusicGenre, typeof(MusicGenre).FullName },
        { BaseItemKind.MusicVideo, typeof(MusicVideo).FullName },
        { BaseItemKind.Person, typeof(Person).FullName },
        { BaseItemKind.Photo, typeof(Photo).FullName },
        { BaseItemKind.PhotoAlbum, typeof(PhotoAlbum).FullName },
        { BaseItemKind.Playlist, typeof(Playlist).FullName },
        { BaseItemKind.PlaylistsFolder, typeof(PlaylistsFolder).FullName },
        { BaseItemKind.Season, typeof(Season).FullName },
        { BaseItemKind.Series, typeof(Series).FullName },
        { BaseItemKind.Studio, typeof(Studio).FullName },
        { BaseItemKind.Trailer, typeof(Trailer).FullName },
        { BaseItemKind.TvChannel, typeof(LiveTvChannel).FullName },
        { BaseItemKind.TvProgram, typeof(LiveTvProgram).FullName },
        { BaseItemKind.UserRootFolder, typeof(UserRootFolder).FullName },
        { BaseItemKind.UserView, typeof(UserView).FullName },
        { BaseItemKind.Video, typeof(Video).FullName },
        { BaseItemKind.Year, typeof(Year).FullName }
    }.AsReadOnly();
}
