using System;
using System.Linq;
using System.Text;
using J2N.Text;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Providers.Manager;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.TV;

/// <summary>
/// Service to manage episode metadata.
/// </summary>
public class EpisodeMetadataService : MetadataService<Episode, EpisodeInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EpisodeMetadataService"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    /// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of the <see cref="IFileSystem"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="externalDataManager">Instance of the <see cref="IExternalDataManager"/> interface.</param>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    public EpisodeMetadataService(
        IServerConfigurationManager serverConfigurationManager,
        ILogger<EpisodeMetadataService> logger,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        ILibraryManager libraryManager,
        IExternalDataManager externalDataManager,
        IItemRepository itemRepository)
        : base(serverConfigurationManager, logger, providerManager, fileSystem, libraryManager, externalDataManager, itemRepository)
    {
    }

    /// <inheritdoc />
    protected override ItemUpdateType BeforeSaveInternal(Episode item, bool isFullRefresh, ItemUpdateType updateType)
    {
        var updatedType = base.BeforeSaveInternal(item, isFullRefresh, updateType);

        // An episode cannot end before it starts.
        if (item.IndexNumberEnd < item.IndexNumber)
        {
            Logger.LogWarning(
                "Discarding episode range end {IndexNumberEnd} preceding episode number {IndexNumber} for {Path}",
                item.IndexNumberEnd,
                item.IndexNumber,
                item.Path);

            item.IndexNumberEnd = null;
            updatedType |= ItemUpdateType.MetadataImport;
        }
        else if (item.IndexNumberEnd.HasValue && !item.IndexNumber.HasValue)
        {
            // Without a first episode the end does not describe a range. Promoting it to the episode number
            // would invent an identity the metadata never supplied, so drop the orphaned value instead.
            Logger.LogWarning(
                "Discarding episode range end {IndexNumberEnd} without an episode number for {Path}",
                item.IndexNumberEnd,
                item.Path);

            item.IndexNumberEnd = null;
            updatedType |= ItemUpdateType.MetadataImport;
        }

        var seriesName = item.FindSeriesName();
        if (!string.Equals(item.SeriesName, seriesName, StringComparison.Ordinal))
        {
            item.SeriesName = seriesName;
            updatedType |= ItemUpdateType.MetadataImport;
        }

        var seasonName = item.FindSeasonName();
        if (!string.Equals(item.SeasonName, seasonName, StringComparison.Ordinal))
        {
            item.SeasonName = seasonName;
            updatedType |= ItemUpdateType.MetadataImport;
        }

        var seriesId = item.FindSeriesId();
        if (!item.SeriesId.Equals(seriesId))
        {
            item.SeriesId = seriesId;
            updatedType |= ItemUpdateType.MetadataImport;
        }

        var seasonId = item.FindSeasonId();
        if (!item.SeasonId.Equals(seasonId))
        {
            item.SeasonId = seasonId;
            updatedType |= ItemUpdateType.MetadataImport;
        }

        var seriesPresentationUniqueKey = item.FindSeriesPresentationUniqueKey();
        if (!string.Equals(item.SeriesPresentationUniqueKey, seriesPresentationUniqueKey, StringComparison.Ordinal))
        {
            item.SeriesPresentationUniqueKey = seriesPresentationUniqueKey;
            updatedType |= ItemUpdateType.MetadataImport;
        }

        return updatedType;
    }

    /// <inheritdoc />
    protected override void MergeData(MetadataResult<Episode> source, MetadataResult<Episode> target, MetadataField[] lockedFields, bool replaceData, bool mergeMetadataSettings)
    {
        base.MergeData(source, target, lockedFields, replaceData, mergeMetadataSettings);

        var sourceItem = source.Item;
        var targetItem = target.Item;

        if (replaceData || !targetItem.AirsBeforeSeasonNumber.HasValue)
        {
            targetItem.AirsBeforeSeasonNumber = sourceItem.AirsBeforeSeasonNumber;
        }

        if (replaceData || !targetItem.AirsAfterSeasonNumber.HasValue)
        {
            targetItem.AirsAfterSeasonNumber = sourceItem.AirsAfterSeasonNumber;
        }

        if (replaceData || !targetItem.AirsBeforeEpisodeNumber.HasValue)
        {
            targetItem.AirsBeforeEpisodeNumber = sourceItem.AirsBeforeEpisodeNumber;
        }

        if (replaceData || !targetItem.IndexNumberEnd.HasValue)
        {
            targetItem.IndexNumberEnd = sourceItem.IndexNumberEnd;
        }

        // merging data for multi-episode files
        // if replaceData flag is set - do it anyway just to remove empty parts if set so
        if (sourceItem.IndexNumberEnd.HasValue || targetItem.IndexNumberEnd.HasValue)
        {
            if (!lockedFields.Contains(MetadataField.Name))
            {
                targetItem.Name = MergeMultiParts(sourceItem.Name, replaceData ? string.Empty : targetItem.Name, Episode.MultiPartSeparator);
            }

            if (!lockedFields.Contains(MetadataField.Overview))
            {
                targetItem.Overview = MergeMultiParts(sourceItem.Overview, replaceData ? string.Empty : targetItem.Overview, Episode.MultiPartSeparator);
            }
        }

        // Episode season numbers can be set from path parsing before local metadata is merged.
        // When a provider supplies an explicit season, prefer it during provider->temp and temp->item merges,
        // but avoid clobbering provider data when existing metadata is backfilled into temp.
        if (mergeMetadataSettings
            && sourceItem.ParentIndexNumber.HasValue
            && targetItem.ParentIndexNumber != sourceItem.ParentIndexNumber)
        {
            targetItem.ParentIndexNumber = sourceItem.ParentIndexNumber;
        }
    }

    private static string MergeMultiParts(string source, string target, string separator, bool removeEmptyParts = false, bool removeTrailingEmptyParts = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(separator);

        if (!removeEmptyParts && !removeTrailingEmptyParts)
        {
            if (string.IsNullOrEmpty(source))
            {
                return target;
            }

            if (string.IsNullOrEmpty(target))
            {
                return source;
            }
        }

        var sb = new StringBuilder();

        var targetSpan = target.AsSpan();
        var sourceSpan = source.AsSpan();

        var targetEnumerator = targetSpan.Split(separator);
        var sourceEnumerator = sourceSpan.Split(separator);

        var targetHasNext = targetEnumerator.MoveNext();
        var sourceHasNext = sourceEnumerator.MoveNext();

        var firstPart = true;
        var separatorCnt = 0;

        // maintain same position index
        while (targetHasNext || sourceHasNext)
        {
            // select a part we want to keep
            var selected = ReadOnlySpan<char>.Empty;
            if (targetHasNext && targetSpan[targetEnumerator.Current].Length > 0)
            {
                selected = targetSpan[targetEnumerator.Current];
            }
            else if (sourceHasNext && sourceSpan[sourceEnumerator.Current].Length > 0)
            {
                selected = sourceSpan[sourceEnumerator.Current];
            }

            if (!removeEmptyParts || !selected.IsEmpty)
            {
                // no separator before the first part
                if (firstPart)
                {
                    firstPart = false;
                }
                else
                {
                    separatorCnt++;
                }
            }

            // append accumulated separators and part
            if (!selected.IsEmpty)
            {
                for (; separatorCnt > 0; separatorCnt--)
                {
                    sb.Append(separator);
                }

                sb.Append(selected);
            }

            targetHasNext = targetEnumerator.MoveNext();
            sourceHasNext = sourceEnumerator.MoveNext();
        }

        // add trailing separators if not removing empty parts
        if (!removeTrailingEmptyParts && !removeEmptyParts)
        {
            for (; separatorCnt > 0; separatorCnt--)
            {
                sb.Append(separator);
            }
        }

        return sb.ToString();
    }
}
