using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Controller.Providers.Music
{
    public static class LastfmHelper
    {
        public static string LocalArtistMetaFileName = "MBArtist.json";
        public static string LocalAlbumMetaFileName = "MBAlbum.json";

        public static void ProcessArtistData(BaseItem artist, LastfmArtist data)
        {
            var overview = data.bio != null ? data.bio.content : null;

            artist.Overview = overview;

            var yearFormed = 0;

            if (data.bio != null)
            {
                Int32.TryParse(data.bio.yearformed, out yearFormed);
            }

            artist.PremiereDate = yearFormed > 0 ? new DateTime(yearFormed, 1,1) : DateTime.MinValue;
            artist.ProductionYear = yearFormed;
            if (data.tags != null)
            {
                AddGenres(artist, data.tags);
            }
        }

        public static void ProcessAlbumData(BaseItem item, LastfmAlbum data)
        {
            if (!string.IsNullOrWhiteSpace(data.mbid)) item.SetProviderId(MetadataProviders.Musicbrainz, data.mbid);

            var overview = data.wiki != null ? data.wiki.content : null;

            item.Overview = overview;

            DateTime release;
            DateTime.TryParse(data.releasedate, out release);
            item.PremiereDate = release;
            item.ProductionYear = release.Year;
            if (data.toptags != null)
            {
                AddGenres(item, data.toptags);
            }
        }

        private static void AddGenres(BaseItem item, LastfmTags tags)
        {
            foreach (var tag in tags.tag)
            {
                item.AddGenre(tag.name);
            }
        }
    }
}
