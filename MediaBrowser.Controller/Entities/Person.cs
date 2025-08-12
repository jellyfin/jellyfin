#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// This is the full Person object that can be retrieved with all of it's data.
    /// </summary>
    [Common.RequiresSourceSerialisation]
    public class Person : BaseItem, IItemByName, IHasLookupInfo<PersonLookupInfo>
    {
        /// <summary>
        /// Gets the folder containing the item.
        /// If the item is a folder, it returns the folder itself.
        /// </summary>
        /// <value>The containing folder path.</value>
        [JsonIgnore]
        public override string ContainingFolderPath => Path;

        /// <summary>
        /// Gets a value indicating whether to enable alpha numeric sorting.
        /// </summary>
        [JsonIgnore]
        public override bool EnableAlphaNumericSorting => false;

        [JsonIgnore]
        public override bool SupportsPeople => false;

        [JsonIgnore]
        public override bool SupportsAncestors => false;

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

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            double value = 2;
            value /= 3;

            return value;
        }

        public IReadOnlyList<BaseItem> GetTaggedItems(InternalItemsQuery query)
        {
            query.PersonIds = new[] { Id };

            return LibraryManager.GetItemList(query);
        }

        public override bool CanDelete()
        {
            return false;
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        public static string GetPath(string name)
        {
            return GetPath(name, true);
        }

        public static string GetPath(string name, bool normalizeName)
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
                Logger.LogDebug("{0} path has changed from {1} to {2}", GetType().Name, Path, newPath);
                return true;
            }

            return base.RequiresRefresh();
        }

        /// <summary>
        /// This is called before any metadata refresh and returns true or false indicating if changes were made.
        /// </summary>
        /// <param name="replaceAllMetadata"><c>true</c> to replace all metadata, <c>false</c> to not.</param>
        /// <returns><c>true</c> if changes were made, <c>false</c> if not.</returns>
        public override bool BeforeMetadataRefresh(bool replaceAllMetadata)
        {
            var hasChanges = base.BeforeMetadataRefresh(replaceAllMetadata);

            var newPath = GetRebasedPath();
            if (!string.Equals(Path, newPath, StringComparison.Ordinal))
            {
                Path = newPath;
                hasChanges = true;
            }

            return hasChanges;
        }
    }
}
