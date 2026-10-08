#nullable disable

#pragma warning disable CS1591

using System;
using System.Linq;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Providers;

namespace MediaBrowser.Controller.Entities
{
    [Common.RequiresSourceSerialisation]
    public class Book : BaseItem, IHasLookupInfo<BookInfo>, IHasSeries
    {
        [JsonIgnore]
        public override MediaType MediaType => MediaType.Book;

        public override bool SupportsPlayedStatus => true;

        public override bool SupportsPositionTicksResume => true;

        [JsonIgnore]
        public override bool SupportsPeople => true;

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

        public Guid FindSeriesId()
        {
            return SeriesId;
        }

        /// <inheritdoc />
        public override bool CanDownload()
        {
            return IsFileProtocol;
        }

        /// <inheritdoc />
        public override bool IsAuthorizedToDownload(User user)
        {
            // Clients read books by fetching the file through the download endpoint,
            // so books must stay accessible to users without the download permission.
            return true;
        }

        /// <inheritdoc />
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
