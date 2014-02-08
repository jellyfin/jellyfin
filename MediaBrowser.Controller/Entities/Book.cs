using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public class Book : BaseItem, IHasTags, IHasPreferredMetadataLanguage, IHasLookupInfo<BookInfo>, IHasSeries
    {
        public override string MediaType
        {
            get
            {
                return Model.Entities.MediaType.Book;
            }
        }

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public List<string> Tags { get; set; }

        public string SeriesName { get; set; }

        public string PreferredMetadataLanguage { get; set; }

        /// <summary>
        /// Gets or sets the preferred metadata country code.
        /// </summary>
        /// <value>The preferred metadata country code.</value>
        public string PreferredMetadataCountryCode { get; set; }

        public Book()
        {
            Tags = new List<string>();
        }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedBooks;
        }

        public BookInfo GetLookupInfo()
        {
            return GetItemLookupInfo<BookInfo>();
        }
    }
}
