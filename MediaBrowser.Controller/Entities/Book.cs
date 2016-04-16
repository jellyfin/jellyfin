using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using System.Linq;
using System.Runtime.Serialization;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    public class Book : BaseItem, IHasTags, IHasLookupInfo<BookInfo>, IHasSeries
    {
        [IgnoreDataMember]
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
