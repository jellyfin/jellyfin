using MediaBrowser.Model.Serialization;
using MediaBrowser.Controller.Entities.Audio;
using System;
using System.Collections.Generic;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Model.Extensions;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Genre
    /// </summary>
    public class Genre : BaseItem, IItemByName
    {
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

        [IgnoreDataMember]
        public override bool IsDisplayedAsFolder
        {
            get
            {
                return true;
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

        [IgnoreDataMember]
        public override bool SupportsExternalTransfer
        {
            get
            {
                return CanDownloadAsFolder();
            }
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        public override bool CanDelete()
        {
            return false;
        }

        public IList<BaseItem> GetTaggedItems(InternalItemsQuery query)
        {
            query.GenreIds = new[] { Id };
            query.ExcludeItemTypes = new[] { typeof(Game).Name, typeof(MusicVideo).Name, typeof(Audio.Audio).Name, typeof(MusicAlbum).Name, typeof(MusicArtist).Name };

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
    }
}
