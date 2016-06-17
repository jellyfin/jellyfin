using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MediaBrowser.Common.Extensions;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Studio
    /// </summary>
    public class Studio : BaseItem, IItemByName
    {
        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            list.Insert(0, GetType().Name + "-" + (Name ?? string.Empty).RemoveDiacritics());
            return list;
        }

        public override string PresentationUniqueKey
        {
            get
            {
                return GetUserDataKeys()[0];
            }
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

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
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

        public IEnumerable<BaseItem> GetTaggedItems(IEnumerable<BaseItem> inputItems)
        {
            return inputItems.Where(GetItemFilter());
        }

        public Func<BaseItem, bool> GetItemFilter()
        {
            return i => i.Studios.Contains(Name, StringComparer.OrdinalIgnoreCase);
        }

        public IEnumerable<BaseItem> GetTaggedItems(InternalItemsQuery query)
        {
            query.Studios = new[] { Name };

            return LibraryManager.GetItemList(query);
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
