#pragma warning disable CS1591

namespace MediaBrowser.Controller.Entities
{
    public class TaggedItemCounts
    {
        public int? AlbumCount { get; set; }

        public int? ArtistCount { get; set; }

        public int? EpisodeCount { get; set; }

        public int? MovieCount { get; set; }

        public int? MusicVideoCount { get; set; }

        public int? ProgramCount { get; set; }

        public int? SeriesCount { get; set; }

        public int? SongCount { get; set; }

        public int? TrailerCount { get; set; }

        public int ChildCount => (AlbumCount ?? 0) + (ArtistCount ?? 0) + (EpisodeCount ?? 0) + (MovieCount ?? 0) + (MusicVideoCount ?? 0) + (ProgramCount ?? 0) + (SeriesCount ?? 0) + (SongCount ?? 0) + (TrailerCount ?? 0);
    }
}
