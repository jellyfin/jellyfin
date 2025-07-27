#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jellyfin.Database.Implementations.ModelConfiguration;

public class BaseItemKindConfiguration : IEntityTypeConfiguration<BaseItemKindEntity>
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BaseItemKindEntity> builder)
    {
        builder.HasKey(e => e.Kind);
        builder.Property(e => e.TypeName).IsRequired().HasMaxLength(64);
        builder.Property(e => e.Description).HasMaxLength(128);

        builder.HasData(
            new BaseItemKindEntity { Kind = -1, TypeName = "PLACEHOLDER", Description = "Default if no class found" },
            new BaseItemKindEntity { Kind = 0, TypeName = "MediaBrowser.Controller.Entities.AggregateFolder", Description = "Aggregate Folder" },
            new BaseItemKindEntity { Kind = 1, TypeName = "MediaBrowser.Controller.Entities.Audio.Audio", Description = "Audio File" },
            new BaseItemKindEntity { Kind = 2, TypeName = "MediaBrowser.Controller.Entities.AudioBook", Description = "Audio Book" },
            new BaseItemKindEntity { Kind = 3, TypeName = "MediaBrowser.Controller.Entities.BasePluginFolder", Description = "Plugin Folder" },
            new BaseItemKindEntity { Kind = 4, TypeName = "MediaBrowser.Controller.Entities.Book", Description = "Book" },
            new BaseItemKindEntity { Kind = 5, TypeName = "MediaBrowser.Controller.Entities.Movies.BoxSet", Description = "Box Set" },
            new BaseItemKindEntity { Kind = 6, TypeName = "MediaBrowser.Controller.Channels.Channel", Description = "Channel" },
            new BaseItemKindEntity { Kind = 7, TypeName = "?.ChannelFolderItem", Description = "Channel Folder Item (virtual class)" },
            new BaseItemKindEntity { Kind = 8, TypeName = "MediaBrowser.Controller.Entities.CollectionFolder", Description = "Collection Folder" },
            new BaseItemKindEntity { Kind = 9, TypeName = "MediaBrowser.Controller.Entities.TV.Episode", Description = "TV Episode" },
            new BaseItemKindEntity { Kind = 10, TypeName = "MediaBrowser.Controller.Entities.Folder", Description = "Folder" },
            new BaseItemKindEntity { Kind = 11, TypeName = "MediaBrowser.Controller.Entities.Genre", Description = "Genre" },
            new BaseItemKindEntity { Kind = 12, TypeName = "MediaBrowser.Controller.LiveTv.LiveTvChannel", Description = "Live TV Channel" },
            new BaseItemKindEntity { Kind = 13, TypeName = "MediaBrowser.Controller.LiveTv.LiveTvProgram", Description = "Live TV Program" },
            new BaseItemKindEntity { Kind = 14, TypeName = "Emby.Server.Implementations.Playlists.PlaylistsFolder", Description = "Manual Playlists Folder" },
            new BaseItemKindEntity { Kind = 15, TypeName = "MediaBrowser.Controller.Entities.Movies.Movie", Description = "Movie" },
            new BaseItemKindEntity { Kind = 16, TypeName = "MediaBrowser.Controller.Entities.Audio.MusicAlbum", Description = "Music Album" },
            new BaseItemKindEntity { Kind = 17, TypeName = "MediaBrowser.Controller.Entities.Audio.MusicArtist", Description = "Music Artist" },
            new BaseItemKindEntity { Kind = 18, TypeName = "MediaBrowser.Controller.Entities.Audio.MusicGenre", Description = "Music Genre" },
            new BaseItemKindEntity { Kind = 19, TypeName = "MediaBrowser.Controller.Entities.MusicVideo", Description = "Music Video" },
            new BaseItemKindEntity { Kind = 20, TypeName = "MediaBrowser.Controller.Entities.Person", Description = "Person" },
            new BaseItemKindEntity { Kind = 21, TypeName = "MediaBrowser.Controller.Entities.Photo", Description = "Photo" },
            new BaseItemKindEntity { Kind = 22, TypeName = "MediaBrowser.Controller.Entities.PhotoAlbum", Description = "Photo Album" },
            new BaseItemKindEntity { Kind = 23, TypeName = "MediaBrowser.Controller.Playlists.Playlist", Description = "Playlist" },
            new BaseItemKindEntity { Kind = 24, TypeName = "?.Recording", Description = "Recording (obsolete?)" },
            new BaseItemKindEntity { Kind = 25, TypeName = "MediaBrowser.Controller.Entities.TV.Season", Description = "TV Season" },
            new BaseItemKindEntity { Kind = 26, TypeName = "MediaBrowser.Controller.Entities.TV.Series", Description = "TV Series" },
            new BaseItemKindEntity { Kind = 27, TypeName = "MediaBrowser.Controller.Entities.Studio", Description = "Studio" },
            new BaseItemKindEntity { Kind = 28, TypeName = "MediaBrowser.Controller.Entities.Trailer", Description = "Trailer" },
            new BaseItemKindEntity { Kind = 29, TypeName = "MediaBrowser.Controller.Entities.UserRootFolder", Description = "User Root Folder" },
            new BaseItemKindEntity { Kind = 30, TypeName = "MediaBrowser.Controller.Entities.UserView", Description = "User View" },
            new BaseItemKindEntity { Kind = 31, TypeName = "MediaBrowser.Controller.Entities.Video", Description = "Video" },
            new BaseItemKindEntity { Kind = 32, TypeName = "MediaBrowser.Controller.Entities.Year", Description = "Year" });
    }
}
