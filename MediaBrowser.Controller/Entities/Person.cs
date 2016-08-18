using MediaBrowser.Controller.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MediaBrowser.Common.Extensions;
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

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            list.Insert(0, GetType().Name + "-" + (Name ?? string.Empty).RemoveDiacritics());
            return list;
        }
        public override string CreatePresentationUniqueKey()
        {
            return GetUserDataKeys()[0];
        }

        public PersonLookupInfo GetLookupInfo()
        {
            return GetItemLookupInfo<PersonLookupInfo>();
        }

        public IEnumerable<BaseItem> GetTaggedItems(InternalItemsQuery query)
        {
            query.Person = Name;

            return LibraryManager.GetItemList(query);
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

        public static string GetPath(string name, bool normalizeName = true)
        {
            // Trim the period at the end because windows will have a hard time with that
            var validFilename = normalizeName ?
                FileSystem.GetValidFilename(name).Trim().TrimEnd('.') :
                name;

            string subFolderPrefix = null;

            foreach (char c in validFilename)
            {
                if (char.IsLetterOrDigit(c))
                {
                    subFolderPrefix = c.ToString();
                    break;
                }
            }

            var path = ConfigurationManager.ApplicationPaths.PeoplePath;

            return string.IsNullOrEmpty(subFolderPrefix) ?
                System.IO.Path.Combine(path, validFilename) :
                System.IO.Path.Combine(path, subFolderPrefix, validFilename);
        }

        private string GetRebasedPath()
        {
            return GetPath(System.IO.Path.GetFileName(Path), false);
        }

        public override bool RequiresRefresh()
        {
            var newPath = GetRebasedPath();
            if (!string.Equals(Path, newPath, StringComparison.Ordinal))
            {
                Logger.Debug("{0} path has changed from {1} to {2}", GetType().Name, Path, newPath);
                return true;
            }
            return base.RequiresRefresh();
        }

        /// <summary>
        /// This is called before any metadata refresh and returns true or false indicating if changes were made
        /// </summary>
        public override bool BeforeMetadataRefresh()
        {
            var hasChanges = base.BeforeMetadataRefresh();

            var newPath = GetRebasedPath();
            if (!string.Equals(Path, newPath, StringComparison.Ordinal))
            {
                Path = newPath;
                hasChanges = true;
            }

            return hasChanges;
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
