using System;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    public class AudioBook : Audio.Audio, IHasSeries
    {
        [IgnoreDataMember]
        public override bool SupportsPositionTicksResume
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsPlayedStatus
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public string SeriesPresentationUniqueKey { get; set; }
        [IgnoreDataMember]
        public string SeriesName { get; set; }
        [IgnoreDataMember]
        public Guid? SeriesId { get; set; }

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

        public override double? GetDefaultPrimaryImageAspectRatio()
        {
            return null;
        }

        [IgnoreDataMember]
        public override bool EnableRefreshOnDateModifiedChange
        {
            get { return true; }
        }

        public Guid? FindSeriesId()
        {
            return SeriesId;
        }

        public override bool CanDownload()
        {
            var locationType = LocationType;
            return locationType != LocationType.Remote &&
                   locationType != LocationType.Virtual;
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Book;
        }
    }
}
