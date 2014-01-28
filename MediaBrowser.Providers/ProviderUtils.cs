using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Providers
{
    public static class ProviderUtils
    {
        public static void MergeBaseItemData(BaseItem source, BaseItem target, List<MetadataFields> lockedFields, bool replaceData, bool mergeMetadataSettings)
        {
            if (!lockedFields.Contains(MetadataFields.Name))
            {
                if (replaceData || string.IsNullOrEmpty(target.Name))
                {
                    target.Name = source.Name;
                }
            }

            if (replaceData || !target.CommunityRating.HasValue)
            {
                target.CommunityRating = source.CommunityRating;
            }

            if (replaceData || !target.EndDate.HasValue)
            {
                target.EndDate = source.EndDate;
            }

            if (!lockedFields.Contains(MetadataFields.Genres))
            {
                if (replaceData || target.Genres.Count == 0)
                {
                    target.Genres = source.Genres;
                }
            }

            if (replaceData || string.IsNullOrEmpty(target.HomePageUrl))
            {
                target.HomePageUrl = source.HomePageUrl;
            }

            if (replaceData || !target.IndexNumber.HasValue)
            {
                target.IndexNumber = source.IndexNumber;
            }

            if (!lockedFields.Contains(MetadataFields.OfficialRating))
            {
                if (replaceData || string.IsNullOrEmpty(target.OfficialRating))
                {
                    target.OfficialRating = source.OfficialRating;
                }
            }

            if (replaceData || string.IsNullOrEmpty(target.OfficialRatingDescription))
            {
                target.OfficialRatingDescription = source.OfficialRatingDescription;
            }

            if (!lockedFields.Contains(MetadataFields.Overview))
            {
                if (replaceData || string.IsNullOrEmpty(target.Overview))
                {
                    target.Overview = source.Overview;
                }
            }

            if (replaceData || !target.ParentIndexNumber.HasValue)
            {
                target.ParentIndexNumber = source.ParentIndexNumber;
            }

            if (!lockedFields.Contains(MetadataFields.Cast))
            {
                if (replaceData || target.People.Count == 0)
                {
                    target.People = source.People;
                }
            }

            if (replaceData || !target.PremiereDate.HasValue)
            {
                target.PremiereDate = source.PremiereDate;
            }

            if (replaceData || !target.ProductionYear.HasValue)
            {
                target.ProductionYear = source.ProductionYear;
            }

            if (!lockedFields.Contains(MetadataFields.Runtime))
            {
                if (replaceData || !target.RunTimeTicks.HasValue)
                {
                    target.RunTimeTicks = source.RunTimeTicks;
                }
            }

            if (!lockedFields.Contains(MetadataFields.Studios))
            {
                if (replaceData || target.Studios.Count == 0)
                {
                    target.Studios = source.Studios;
                }
            }

            if (replaceData || !target.VoteCount.HasValue)
            {
                target.VoteCount = source.VoteCount;
            }

            foreach (var id in source.ProviderIds)
            {
                target.ProviderIds[id.Key] = id.Value;
            }

            if (mergeMetadataSettings)
            {
                target.ForcedSortName = source.ForcedSortName;
                target.LockedFields = source.LockedFields;
                target.DontFetchMeta = source.DontFetchMeta;
                target.DisplayMediaType = source.DisplayMediaType;
            }
        }
    }
}
