using System;
using System.Text.Json.Serialization;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Entities
{
    public class AudioBook : Audio.Audio, IHasSeries, IHasLookupInfo<SongInfo>
    {
        [JsonIgnore]
        public override bool SupportsPositionTicksResume => true;

        [JsonIgnore]
        public override bool SupportsPlayedStatus => true;

        [JsonIgnore]
        public string SeriesPresentationUniqueKey { get; set; }
        [JsonIgnore]
        public string SeriesName { get; set; }
        [JsonIgnore]
        public Guid SeriesId { get; set; }

        public string FindSeriesSortName()
        {
            return SeriesName;
        }
        public string FindSeriesName()
        {
            return SeriesName;
        }
        public string FindSeriesPresentationUniqueKey()
        {
            return SeriesPresentationUniqueKey;
        }

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            return 0;
        }

        public Guid FindSeriesId()
        {
            return SeriesId;
        }

        public override bool CanDownload()
        {
            return IsFileProtocol;
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Book;
        }
    }
}
