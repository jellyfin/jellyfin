#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Playlists
{
    public class PlaylistCreationRequest
    {
        public string Name { get; set; }

        public Guid[] ItemIdList { get; set; }

        public string MediaType { get; set; }

        public Guid UserId { get; set; }

        public PlaylistCreationRequest()
        {
            ItemIdList = Array.Empty<Guid>();
        }
    }
}
