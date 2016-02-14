using MediaBrowser.Controller.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// This is the full Person object that can be retrieved with all of it's data.
    /// </summary>
    public class Person : BaseItem, IItemByName, IHasLookupInfo<PersonLookupInfo>
    {
        /// <summary>
        /// Gets or sets the place of birth.
        /// </summary>
        /// <value>The place of birth.</value>
        public string PlaceOfBirth { get; set; }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string CreateUserDataKey()
        {
            return "Person-" + Name;
        }

        public PersonLookupInfo GetLookupInfo()
        {
            return GetItemLookupInfo<PersonLookupInfo>();
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

        [IgnoreDataMember]
        public override bool EnableAlphaNumericSorting
        {
            get
            {
                return false;
            }
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
            var itemsWithPerson = LibraryManager.GetItemIds(new InternalItemsQuery
            {
                Person = Name
            });

            return inputItems.Where(i => itemsWithPerson.Contains(i.Id));
        }


        public Func<BaseItem, bool> GetItemFilter()
        {
            return i => LibraryManager.GetPeople(i).Any(p => string.Equals(p.Name, Name, StringComparison.OrdinalIgnoreCase));
        }

        [IgnoreDataMember]
        public override bool SupportsPeople
        {
            get
            {
                return false;
            }
        }

        [IgnoreDataMember]
        public override bool SupportsAncestors
        {
            get
            {
                return false;
            }
        }
    }

    /// <summary>
    /// This is the small Person stub that is attached to BaseItems
    /// </summary>
    public class PersonInfo : IHasProviderIds
    {
        public PersonInfo()
        {
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public Guid ItemId { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the role.
        /// </summary>
        /// <value>The role.</value>
        public string Role { get; set; }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the sort order - ascending
        /// </summary>
        /// <value>The sort order.</value>
        public int? SortOrder { get; set; }

        public string ImageUrl { get; set; }

        public Dictionary<string, string> ProviderIds { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Name;
        }

        public bool IsType(string type)
        {
            return string.Equals(Type, type, StringComparison.OrdinalIgnoreCase) || string.Equals(Role, type, StringComparison.OrdinalIgnoreCase);
        }
    }
}
