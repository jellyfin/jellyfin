#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Playlists
{
    public class PlaylistCreationRequest
    {
        public string Name { get; set; }

        public IReadOnlyList<Guid> ItemIdList { get; set; } = Array.Empty<Guid>();

        public string MediaType { get; set; }

        public Guid UserId { get; set; }
    }
}
