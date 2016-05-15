using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Year
    /// </summary>
    public class Year : BaseItem, IItemByName
    {
        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            list.Insert(0, "Year-" + Name);
            return list;
        }

        /// <summary>
        /// Returns the folder containing the item.
        /// If the item is a folder, it returns the folder itself
        /// </summary>
        /// <value>The containing folder path.</value>
        [IgnoreDataMember]
        public override string ContainingFolderPath
        {
            get
            {
                return Path;
            }
        }

        public override bool CanDelete()
        {
            return false;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is owned item.
        /// </summary>
        /// <value><c>true</c> if this instance is owned item; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public override bool IsOwnedItem
        {
            get
            {
                return false;
            }
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        public IEnumerable<BaseItem> GetTaggedItems(IEnumerable<BaseItem> inputItems)
        {
            int year;

            var usCulture = new CultureInfo("en-US");

            if (!int.TryParse(Name, NumberStyles.Integer, usCulture, out year))
            {
                return inputItems;
            }

            return inputItems.Where(i => i.ProductionYear.HasValue && i.ProductionYear.Value == year);
        }

        public IEnumerable<BaseItem> GetTaggedItems(InternalItemsQuery query)
        {
            int year;

            var usCulture = new CultureInfo("en-US");

            if (!int.TryParse(Name, NumberStyles.Integer, usCulture, out year))
            {
                return new List<BaseItem>();
            }

            query.Years = new[] { year };

            return LibraryManager.GetItemList(query);
        }

        public int? GetYearValue()
        {
            int i;

            if (int.TryParse(Name, NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
            {
                return i;
            }

            return null;
        }

        public Func<BaseItem, bool> GetItemFilter()
        {
            var val = GetYearValue();
            return i => i.ProductionYear.HasValue && val.HasValue && i.ProductionYear.Value == val.Value;
        }

        [IgnoreDataMember]
        public override bool SupportsPeople
        {
            get
            {
                return false;
            }
        }
    }
}
