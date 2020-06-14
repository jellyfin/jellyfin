#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Providers
{
    public class RemoteSubtitleInfo
    {
        public string ThreeLetterISOLanguageName { get; set; }
        public string Id { get; set; }
        public string ProviderName { get; set; }
        public string Name { get; set; }
        public string Format { get; set; }
        public string Author { get; set; }
        public string Comment { get; set; }
        public DateTime? DateCreated { get; set; }
        public float? CommunityRating { get; set; }
        public int? DownloadCount { get; set; }
        public bool? IsHashMatch { get; set; }
    }
}
