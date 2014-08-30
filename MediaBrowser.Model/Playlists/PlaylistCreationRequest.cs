using System.Collections.Generic;

namespace MediaBrowser.Model.Playlists
{
    public class PlaylistCreationRequest
    {
         public string Name { get; set; }

        public List<string> ItemIdList { get; set; }

        public string MediaType { get; set; }

        public string UserId { get; set; }

        public PlaylistCreationRequest()
        {
            ItemIdList = new List<string>();
        }
   }
}
