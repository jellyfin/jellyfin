#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.AudioBooks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Books
{
    public class AudioBookMetadataService : MetadataService<AudioBook, AudioBookFolderInfo>
    {
        public AudioBookMetadataService(
            IServerConfigurationManager serverConfigurationManager,
            ILogger<AudioBookMetadataService> logger,
            IProviderManager providerManager,
            IFileSystem fileSystem,
            ILibraryManager libraryManager)
            : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager)
        {
        }

        /// <inheritdoc />
        protected override bool EnableUpdatingPremiereDateFromChildren => true;

        /// <inheritdoc />
        protected override bool EnableUpdatingGenresFromChildren => true;

        /// <inheritdoc />
        protected override bool EnableUpdatingStudiosFromChildren => true;

        /// <inheritdoc />
        protected override IList<Controller.Entities.BaseItem> GetChildrenForMetadataUpdates(AudioBook item)
            => item.GetRecursiveChildren(i => i is AudioBookFile);

        /// <inheritdoc />
        protected override ItemUpdateType UpdateMetadataFromChildren(AudioBook item, IList<Controller.Entities.BaseItem> children, bool isFullRefresh, ItemUpdateType currentUpdateType)
        {
            var updateType = base.UpdateMetadataFromChildren(item, children, isFullRefresh, currentUpdateType);

            // don't update user-changeable metadata for locked items
            if (item.IsLocked)
            {
                return updateType;
            }

            if (isFullRefresh || currentUpdateType > ItemUpdateType.None)
            {
                if (!item.LockedFields.Contains(MetadataField.Name))
                {
                    var name = children.Select(i => i.Album).FirstOrDefault(i => !string.IsNullOrEmpty(i));

                    if (!string.IsNullOrEmpty(name)
                        && !string.Equals(item.Name, name, StringComparison.Ordinal))
                    {
                        item.Name = name;
                        updateType |= ItemUpdateType.MetadataEdit;
                    }
                }

                var files = children.Cast<AudioBookFile>().ToArray();

                updateType |= SetAuthorsFromFile(item, files);
                updateType |= SetPeople(item);
            }

            return updateType;
        }

        private ItemUpdateType SetAuthorsFromFile(AudioBook item, IReadOnlyList<AudioBookFile> files)
        {
            var updateType = ItemUpdateType.None;

            // var authors = files
            // .SelectMany(i => i.Authors)
            // .Where(i => i.Length != 0)
            // .GroupBy(i => i)
            // .OrderByDescending(g => g.Count())
            // .Select(g => g.Key)
            // .ToArray();

            // if (!item.Authors.SequenceEqual(authors, StringComparer.OrdinalIgnoreCase))
            // {
            // item.Authors = authors;
            // updateType |= ItemUpdateType.MetadataEdit;
            // }

            return updateType;
        }

        private ItemUpdateType SetPeople(AudioBook item)
        {
            var updateType = ItemUpdateType.None;

            if (item.Authors.Any())
            {
                var people = new List<Controller.Entities.PersonInfo>();

                foreach (var author in item.Authors)
                {
                    Controller.Entities.PeopleHelper.AddPerson(people, new Controller.Entities.PersonInfo
                    {
                        Name = author,
                        Type = PersonKind.Author
                    });
                }

                // TODO: Add narrator

                LibraryManager.UpdatePeople(item, people);
                updateType |= ItemUpdateType.MetadataEdit;
            }

            return updateType;
        }

        /// <inheritdoc />
        protected override void MergeData(
            MetadataResult<AudioBook> source,
            MetadataResult<AudioBook> target,
            MetadataField[] lockedFields,
            bool replaceData,
            bool mergeMetadataSettings)
        {
            base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

            var sourceItem = source.Item;
            var targetItem = target.Item;

            if (replaceData || targetItem.Authors.Count == 0)
            {
                targetItem.Authors = sourceItem.Authors;
            }
        }
    }
}
