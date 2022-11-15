#pragma warning disable CS1591

using System.Linq;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Music
{
    public static class AlbumInfoExtensions
    {
        public static string? GetAlbumArtist(this AlbumInfo info)
        {
            var id = info.SongInfos.SelectMany(i => i.AlbumArtists)
                    .FirstOrDefault(i => !string.IsNullOrEmpty(i));

            if (!string.IsNullOrEmpty(id))
            {
                return id;
            }

            return info.AlbumArtists.Count > 0 ? info.AlbumArtists[0] : default;
        }

        public static string? GetReleaseGroupId(this AlbumInfo info)
        {
            var id = info.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup);

            if (string.IsNullOrEmpty(id))
            {
                return info.SongInfos.Select(i => i.GetProviderId(MetadataProvider.MusicBrainzReleaseGroup))
                    .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            }

            return id;
        }

        public static string? GetReleaseId(this AlbumInfo info)
        {
            var id = info.GetProviderId(MetadataProvider.MusicBrainzAlbum);

            if (string.IsNullOrEmpty(id))
            {
                return info.SongInfos.Select(i => i.GetProviderId(MetadataProvider.MusicBrainzAlbum))
                    .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            }

            return id;
        }

        public static string? GetMusicBrainzArtistId(this AlbumInfo info)
        {
            info.ProviderIds.TryGetValue(MetadataProvider.MusicBrainzAlbumArtist.ToString(), out string? id);

            if (string.IsNullOrEmpty(id))
            {
                info.ArtistProviderIds.TryGetValue(MetadataProvider.MusicBrainzArtist.ToString(), out id);
            }

            if (string.IsNullOrEmpty(id))
            {
                return info.SongInfos.Select(i => i.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist))
                    .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            }

            return id;
        }

        public static string? GetMusicBrainzArtistId(this ArtistInfo info)
        {
            info.ProviderIds.TryGetValue(MetadataProvider.MusicBrainzArtist.ToString(), out var id);

            if (string.IsNullOrEmpty(id))
            {
                return info.SongInfos.Select(i => i.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist))
                    .FirstOrDefault(i => !string.IsNullOrEmpty(i));
            }

            return id;
        }
    }
}
