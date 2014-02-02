using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
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

            if (!lockedFields.Contains(MetadataFields.Tags))
            {
                var sourceHasTags = source as IHasTags;
                var targetHasTags = target as IHasTags;

                if (sourceHasTags != null && targetHasTags != null)
                {
                    if (replaceData || targetHasTags.Tags.Count == 0)
                    {
                        targetHasTags.Tags = sourceHasTags.Tags;
                    }
                }
            }

            if (!lockedFields.Contains(MetadataFields.Keywords))
            {
                var sourceHasKeywords = source as IHasKeywords;
                var targetHasKeywords = target as IHasKeywords;

                if (sourceHasKeywords != null && targetHasKeywords != null)
                {
                    if (replaceData || targetHasKeywords.Keywords.Count == 0)
                    {
                        targetHasKeywords.Keywords = sourceHasKeywords.Keywords;
                    }
                }
            }

            if (!lockedFields.Contains(MetadataFields.ProductionLocations))
            {
                var sourceHasProductionLocations = source as IHasProductionLocations;
                var targetHasProductionLocations = target as IHasProductionLocations;

                if (sourceHasProductionLocations != null && targetHasProductionLocations != null)
                {
                    if (replaceData || targetHasProductionLocations.ProductionLocations.Count == 0)
                    {
                        targetHasProductionLocations.ProductionLocations = sourceHasProductionLocations.ProductionLocations;
                    }
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

            MergeAlbumArtist(source, target, lockedFields, replaceData);

            if (mergeMetadataSettings)
            {
                target.ForcedSortName = source.ForcedSortName;
                target.LockedFields = source.LockedFields;
                target.DontFetchMeta = source.DontFetchMeta;
                target.DisplayMediaType = source.DisplayMediaType;

                var sourceHasLanguageSettings = source as IHasPreferredMetadataLanguage;
                var targetHasLanguageSettings = target as IHasPreferredMetadataLanguage;

                if (sourceHasLanguageSettings != null && targetHasLanguageSettings != null)
                {
                    targetHasLanguageSettings.PreferredMetadataCountryCode = sourceHasLanguageSettings.PreferredMetadataCountryCode;
                    targetHasLanguageSettings.PreferredMetadataLanguage = sourceHasLanguageSettings.PreferredMetadataLanguage;
                }

                var sourceHasDisplayOrder = source as IHasDisplayOrder;
                var targetHasDisplayOrder = target as IHasDisplayOrder;

                if (sourceHasDisplayOrder != null && targetHasDisplayOrder != null)
                {
                    targetHasDisplayOrder.DisplayOrder = sourceHasDisplayOrder.DisplayOrder;
                }
            }
        }

        private static void MergeAlbumArtist(BaseItem source, BaseItem target, List<MetadataFields> lockedFields, bool replaceData)
        {
            var sourceHasAlbumArtist = source as IHasAlbumArtist;
            var targetHasAlbumArtist = target as IHasAlbumArtist;

            if (sourceHasAlbumArtist != null && targetHasAlbumArtist != null)
            {
                if (replaceData || string.IsNullOrEmpty(targetHasAlbumArtist.AlbumArtist))
                {
                    targetHasAlbumArtist.AlbumArtist = sourceHasAlbumArtist.AlbumArtist;
                }
            }
        }
    }
}
