using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using System;
using System.Linq;

namespace MediaBrowser.Providers.Music
{
    public static class LastfmHelper
    {
        public static string GetImageUrl(IHasLastFmImages data, out string size)
        {
            size = null;

            if (data.image == null)
            {
                return null;
            }

            var validImages = data.image
                .Where(i => !string.IsNullOrWhiteSpace(i.url))
                .ToList();

            var img = validImages
                .FirstOrDefault(i => string.Equals(i.size, "mega", StringComparison.OrdinalIgnoreCase)) ??
                data.image.FirstOrDefault(i => string.Equals(i.size, "extralarge", StringComparison.OrdinalIgnoreCase)) ??
                data.image.FirstOrDefault(i => string.Equals(i.size, "large", StringComparison.OrdinalIgnoreCase)) ??
                data.image.FirstOrDefault(i => string.Equals(i.size, "medium", StringComparison.OrdinalIgnoreCase)) ??
                data.image.FirstOrDefault();

            if (img != null)
            {
                size = img.size;
                return img.url;
            }

            return null;
        }

        public static void ProcessAlbumData(BaseItem item, LastfmAlbum data)
        {
            var overview = data.wiki != null ? data.wiki.content : null;

            if (!item.LockedFields.Contains(MetadataFields.Overview))
            {
                item.Overview = overview;
            }

            // Only grab the date here if the album doesn't already have one, since id3 tags are preferred
            DateTime release;

            if (DateTime.TryParse(data.releasedate, out release))
            {
                // Lastfm sends back null as sometimes 1901, other times 0
                if (release.Year > 1901)
                {
                    if (!item.PremiereDate.HasValue)
                    {
                        item.PremiereDate = release;
                    }

                    if (!item.ProductionYear.HasValue)
                    {
                        item.ProductionYear = release.Year;
                    }
                }
            }

            var album = (MusicAlbum)item;

            string imageSize;

            album.LastFmImageUrl = GetImageUrl(data, out imageSize);
            album.LastFmImageSize = imageSize;
        }
    }
}
