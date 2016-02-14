using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Entities
{
    public class Book : BaseItem, IHasTags, IHasLookupInfo<BookInfo>, IHasSeries
    {
        public override string MediaType
        {
            get
            {
                return Model.Entities.MediaType.Book;
            }
        }

        public string SeriesName { get; set; }

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
