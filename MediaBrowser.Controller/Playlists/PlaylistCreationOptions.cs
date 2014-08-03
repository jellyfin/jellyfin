using System.Collections.Generic;

namespace MediaBrowser.Controller.Playlists
{
    public class PlaylistCreationOptions
    {
        public string Name { get; set; }

        public List<string> ItemIdList { get; set; }

        public string MediaType { get; set; }

        public string UserId { get; set; }

        public PlaylistCreationOptions()
        {
            ItemIdList = new List<string>();
        }
    }
}
