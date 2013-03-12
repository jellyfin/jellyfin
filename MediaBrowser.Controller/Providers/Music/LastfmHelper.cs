using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers.Music
{
    public static class LastfmHelper
    {
        public static string LocalArtistMetaFileName = "MBArtist.json";
        public static string LocalAlbumMetaFileName = "MBAlbum.json";

        public static void ProcessArtistData(BaseItem artist, LastfmArtist data)
        {
            var overview = data.bio != null ? data.bio.content : null;

            if (!string.IsNullOrEmpty(overview))
            {
                overview = StripHtml(overview);
            }

            artist.Overview = overview;

            var yearFormed = 0;
            try
            {
                yearFormed = Convert.ToInt32(data.bio.yearformed);
            }
            catch (FormatException)
            {
            }
            catch (NullReferenceException)
            {
            }
            catch (OverflowException)
            {
            }
            artist.PremiereDate = new DateTime(yearFormed, 1,1);
            if (data.tags != null)
            {
                AddGenres(artist, data.tags);
            }
        }

        public static void ProcessAlbumData(BaseItem item, LastfmAlbum data)
        {
            if (!string.IsNullOrWhiteSpace(data.mbid)) item.SetProviderId(MetadataProviders.Musicbrainz, data.mbid);

            var overview = data.wiki != null ? data.wiki.content : null;

            if (!string.IsNullOrEmpty(overview))
            {
                overview = StripHtml(overview);
            }

            item.Overview = overview;

            var release = DateTime.MinValue;
            DateTime.TryParse(data.releasedate, out release);
            item.PremiereDate = release;
            if (data.toptags != null)
            {
                AddGenres(item, data.toptags);
            }
        }

        private static string StripHtml(string htmlString)
        {
            // http://stackoverflow.com/questions/1349023/how-can-i-strip-html-from-text-in-net
            const string pattern = @"<(.|\n)*?>";
            return Regex.Replace(htmlString, pattern, string.Empty);
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
