using MediaBrowser.Model.Configuration;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public class Book : BaseItem, IHasTags, IHasPreferredMetadataLanguage
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
        /// 
        /// </summary>
        public override string MetaLocation
        {
            get
            {
                return System.IO.Path.GetDirectoryName(Path);
            }
        }

        protected override bool UseParentPathToCreateResolveArgs
        {
            get
            {
                return !IsInMixedFolder;
            }
        }

        public Book()
        {
            Tags = new List<string>();
        }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            return config.BlockUnratedBooks;
        }
    }
}
