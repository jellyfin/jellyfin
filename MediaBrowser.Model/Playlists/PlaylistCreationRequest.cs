#pragma warning disable CS1591
#pragma warning disable CA1819 // Properties should not return arrays

using System;

namespace MediaBrowser.Model.Playlists
{
    public class PlaylistCreationRequest
    {
        public PlaylistCreationRequest()
        {
            ItemIdList = Array.Empty<Guid>();
        }

        public string Name { get; set; }

        public Guid[] ItemIdList { get; set; }

        public string MediaType { get; set; }

        public Guid UserId { get; set; }
    }
}
