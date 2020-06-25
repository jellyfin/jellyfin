using System;
using System.Linq;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Providers;

namespace MediaBrowser.Controller.Entities
{
    public class Book : BaseItem, IHasLookupInfo<BookInfo>, IHasSeries
    {
        [JsonIgnore]
        public override string MediaType => Model.Entities.MediaType.Book;

        public override bool SupportsPlayedStatus => true;

        public override bool SupportsPositionTicksResume => true;

        [JsonIgnore]
        public string SeriesPresentationUniqueKey { get; set; }

        [JsonIgnore]
        public string SeriesName { get; set; }

        [JsonIgnore]
        public Guid SeriesId { get; set; }

        public Book()
        {
            this.RunTimeTicks = TimeSpan.TicksPerSecond;
        }

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

        public BookInfo GetLookupInfo()
        {
            var info = GetItemLookupInfo<BookInfo>();

            if (string.IsNullOrEmpty(SeriesName))
            {
                info.SeriesName = GetParents().Select(i => i.Name).FirstOrDefault();
            }
            else
            {
                info.SeriesName = SeriesName;
            }

            return info;
        }
    }
}
