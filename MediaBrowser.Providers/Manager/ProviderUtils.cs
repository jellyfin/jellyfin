using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Providers.Manager
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
                    if (!(target is Audio) && !(target is Video))
                    {
                        target.RunTimeTicks = source.RunTimeTicks;
                    }
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
            MergeBudget(source, target, lockedFields, replaceData);
            MergeMetascore(source, target, lockedFields, replaceData);
            MergeCriticRating(source, target, lockedFields, replaceData);
            MergeAwards(source, target, lockedFields, replaceData);
            MergeTaglines(source, target, lockedFields, replaceData);
            MergeTrailers(source, target, lockedFields, replaceData);

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

        private static void MergeBudget(BaseItem source, BaseItem target, List<MetadataFields> lockedFields, bool replaceData)
        {
            var sourceHasBudget = source as IHasBudget;
            var targetHasBudget = target as IHasBudget;

            if (sourceHasBudget != null && targetHasBudget != null)
            {
                if (replaceData || !targetHasBudget.Budget.HasValue)
                {
                    targetHasBudget.Budget = sourceHasBudget.Budget;
                }

                if (replaceData || !targetHasBudget.Revenue.HasValue)
                {
                    targetHasBudget.Revenue = sourceHasBudget.Revenue;
                }
            }
        }

        private static void MergeMetascore(BaseItem source, BaseItem target, List<MetadataFields> lockedFields, bool replaceData)
        {
            var sourceCast = source as IHasMetascore;
            var targetCast = target as IHasMetascore;

            if (sourceCast != null && targetCast != null)
            {
                if (replaceData || !targetCast.Metascore.HasValue)
                {
                    targetCast.Metascore = sourceCast.Metascore;
                }
            }
        }

        private static void MergeAwards(BaseItem source, BaseItem target, List<MetadataFields> lockedFields, bool replaceData)
        {
            var sourceCast = source as IHasAwards;
            var targetCast = target as IHasAwards;

            if (sourceCast != null && targetCast != null)
            {
                if (replaceData || string.IsNullOrEmpty(targetCast.AwardSummary))
                {
                    targetCast.AwardSummary = sourceCast.AwardSummary;
                }
            }
        }

        private static void MergeCriticRating(BaseItem source, BaseItem target, List<MetadataFields> lockedFields, bool replaceData)
        {
            var sourceCast = source as IHasCriticRating;
            var targetCast = target as IHasCriticRating;

            if (sourceCast != null && targetCast != null)
            {
                if (replaceData || !targetCast.CriticRating.HasValue)
                {
                    targetCast.CriticRating = sourceCast.CriticRating;
                }

                if (replaceData || string.IsNullOrEmpty(targetCast.CriticRatingSummary))
                {
                    targetCast.CriticRatingSummary = sourceCast.CriticRatingSummary;
                }
            }
        }

        private static void MergeTaglines(BaseItem source, BaseItem target, List<MetadataFields> lockedFields, bool replaceData)
        {
            var sourceCast = source as IHasTaglines;
            var targetCast = target as IHasTaglines;

            if (sourceCast != null && targetCast != null)
            {
                if (replaceData || targetCast.Taglines.Count == 0)
                {
                    targetCast.Taglines = sourceCast.Taglines;
                }
            }
        }

        private static void MergeTrailers(BaseItem source, BaseItem target, List<MetadataFields> lockedFields, bool replaceData)
        {
            var sourceCast = source as IHasTrailers;
            var targetCast = target as IHasTrailers;

            if (sourceCast != null && targetCast != null)
            {
                if (replaceData || targetCast.RemoteTrailers.Count == 0)
                {
                    targetCast.RemoteTrailers = sourceCast.RemoteTrailers;
                }
            }
        }
    }
}
