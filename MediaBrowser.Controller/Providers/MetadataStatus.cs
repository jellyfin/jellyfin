using System;

namespace MediaBrowser.Controller.Providers
{
    public class MetadataStatus
    {
        /// <summary>
        /// Gets or sets the item identifier.
        /// </summary>
        /// <value>The item identifier.</value>
        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the date last metadata refresh.
        /// </summary>
        /// <value>The date last metadata refresh.</value>
        public DateTime? DateLastMetadataRefresh { get; set; }

        /// <summary>
        /// Gets or sets the date last images refresh.
        /// </summary>
        /// <value>The date last images refresh.</value>
        public DateTime? DateLastImagesRefresh { get; set; }

        public DateTime? ItemDateModified { get; set; }

        public bool IsDirty { get; private set; }

        public void SetDateLastMetadataRefresh(DateTime? date)
        {
            if (date != DateLastMetadataRefresh)
            {
                IsDirty = true;
            }

            DateLastMetadataRefresh = date;
        }

        public void SetDateLastImagesRefresh(DateTime? date)
        {
            if (date != DateLastImagesRefresh)
            {
                IsDirty = true;
            }

            DateLastImagesRefresh = date;
        }
    }
}
