#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Genre.
    /// </summary>
    [Common.RequiresSourceSerialisation]
    public class Genre : BaseItem, IItemByName
    {
        /// <summary>
        /// Gets the folder containing the item.
        /// If the item is a folder, it returns the folder itself.
        /// </summary>
        /// <value>The containing folder path.</value>
        [JsonIgnore]
        public override string ContainingFolderPath => Path;

        [JsonIgnore]
        public override bool IsDisplayedAsFolder => true;

        [JsonIgnore]
        public override bool SupportsAncestors => false;

        [JsonIgnore]
        public override bool SupportsPeople => false;

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

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            return 1;
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        public override bool CanDelete()
        {
            return false;
        }

        public IReadOnlyList<BaseItem> GetTaggedItems(InternalItemsQuery query)
        {
            query.GenreIds = new[] { Id };
            query.ExcludeItemTypes = new[]
            {
                BaseItemKind.MusicVideo,
                BaseItemKind.Audio,
                BaseItemKind.MusicAlbum,
                BaseItemKind.MusicArtist
            };

            return LibraryManager.GetItemList(query);
        }

        public static string GetPath(string name)
        {
            return GetPath(name, true);
        }

        public static string GetPath(string name, bool normalizeName)
        {
            // Trim the period at the end because windows will have a hard time with that
            var validName = normalizeName ?
                FileSystem.GetValidFilename(name).Trim().TrimEnd('.') :
                name;

            return System.IO.Path.Combine(ConfigurationManager.ApplicationPaths.GenrePath, validName);
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
        /// This is called before any metadata refresh and returns true if changes were made.
        /// </summary>
        /// <param name="replaceAllMetadata">Whether to replace all metadata.</param>
        /// <returns>true if the item has change, else false.</returns>
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
