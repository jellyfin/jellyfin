#nullable disable

#pragma warning disable CS1591

using System.Collections.Generic;

namespace Jellyfin.Api.Models.LookupDtos
{
    public class MusicVideoInfo : ItemLookupInfo
    {
        public IReadOnlyList<string> Artists { get; set; }
    }
}
