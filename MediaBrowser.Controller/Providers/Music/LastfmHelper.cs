using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using System;
using System.Linq;

namespace MediaBrowser.Controller.Providers.Music
{
    public static class LastfmHelper
    {
        public static string LocalArtistMetaFileName = "mbartist.js";
        public static string LocalAlbumMetaFileName = "mbalbum.js";

        public static void ProcessArtistData(BaseItem artist, LastfmArtist data)
        {
            var yearFormed = 0;

            if (data.bio != null)
            {
                Int32.TryParse(data.bio.yearformed, out yearFormed);

                artist.Overview = data.bio.content;

                if (!string.IsNullOrEmpty(data.bio.placeformed))
                {
                    artist.AddProductionLocation(data.bio.placeformed);
                }
            }

            artist.PremiereDate = yearFormed > 0 ? new DateTime(yearFormed, 1, 1, 0, 0, 0, DateTimeKind.Utc) : (DateTime?)null;
            artist.ProductionYear = yearFormed;
            if (data.tags != null)
            {
                AddTags(artist, data.tags);
            }

            var entity = artist as Artist;

            if (entity != null)
            {
                entity.IsOnTour = string.Equals(data.ontour, "1");
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
                AddTags(item, data.toptags);
            }
        }

        private static void AddTags(BaseItem item, LastfmTags tags)
        {
            var itemTags = (from tag in tags.tag where !string.IsNullOrEmpty(tag.name) select tag.name).ToList();

            item.Tags = itemTags;
        }
    }
}
