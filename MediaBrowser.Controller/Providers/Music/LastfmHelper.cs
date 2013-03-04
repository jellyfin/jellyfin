using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Providers.Music
{
    public static class LastfmHelper
    {
        public static string LocalArtistMetaFileName = "MBArtist.json";

        public static void ProcessArtistData(BaseItem artist, LastfmArtist data)
        {
            artist.Overview = data.bio != null ? data.bio.content : null;
            if (data.tags != null)
            {
                foreach (var tag in data.tags.tag)
                {
                    artist.AddGenre(tag.name);
                }

            }
        }

    }
}
