using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Extensions;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Manager
{
    public static class ProviderUtils
    {
        public static void MergeBaseItemData<T>(
            MetadataResult<T> sourceResult,
            MetadataResult<T> targetResult,
            MetadataFields[] lockedFields,
            bool replaceData,
            bool mergeMetadataSettings)
            where T : BaseItem
        {
            var source = sourceResult.Item;
            var target = targetResult.Item;

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (!lockedFields.Contains(MetadataFields.Name))
            {
                if (replaceData || string.IsNullOrEmpty(target.Name))
                {
                    // Safeguard against incoming data having an emtpy name
                    if (!string.IsNullOrWhiteSpace(source.Name))
                    {
                        target.Name = source.Name;
                    }
                }
            }

            if (replaceData || string.IsNullOrEmpty(target.OriginalTitle))
            {
                // Safeguard against incoming data having an emtpy name
                if (!string.IsNullOrWhiteSpace(source.OriginalTitle))
                {
                    target.OriginalTitle = source.OriginalTitle;
                }
            }

            if (replaceData || !target.CommunityRating.HasValue || (source.CommunityRating.HasValue && string.Equals(sourceResult.Provider, "The Open Movie Database", StringComparison.OrdinalIgnoreCase)))
            {
                target.CommunityRating = source.CommunityRating;
            }

            if (replaceData || !target.EndDate.HasValue)
            {
                target.EndDate = source.EndDate;
            }

            if (!lockedFields.Contains(MetadataFields.Genres))
            {
                if (replaceData || target.Genres.Length == 0)
                {
                    target.Genres = source.Genres;
                }
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

            if (replaceData || string.IsNullOrEmpty(target.CustomRating))
            {
                target.CustomRating = source.CustomRating;
            }

            if (replaceData || string.IsNullOrEmpty(target.Tagline))
            {
                target.Tagline = source.Tagline;
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
                if (replaceData || targetResult.People == null || targetResult.People.Count == 0)
                {
                    targetResult.People = sourceResult.People;

                }
                else if (targetResult.People != null && sourceResult.People != null)
                {
                    MergePeople(sourceResult.People, targetResult.People);
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
                if (replaceData || target.Studios.Length == 0)
                {
                    target.Studios = source.Studios;
                }
            }

            if (!lockedFields.Contains(MetadataFields.Tags))
            {
                if (replaceData || target.Tags.Length == 0)
                {
                    target.Tags = source.Tags;
                }
            }

            if (!lockedFields.Contains(MetadataFields.ProductionLocations))
            {
                if (replaceData || target.ProductionLocations.Length == 0)
                {
                    target.ProductionLocations = source.ProductionLocations;
                }
            }

            foreach (var id in source.ProviderIds)
            {
                var key = id.Key;

                // Don't replace existing Id's.
                if (replaceData || !target.ProviderIds.ContainsKey(key))
                {
                    target.ProviderIds[key] = id.Value;
                }
            }

            MergeAlbumArtist(source, target, replaceData);
            MergeCriticRating(source, target, replaceData);
            MergeTrailers(source, target, replaceData);
            MergeVideoInfo(source, target, replaceData);
            MergeDisplayOrder(source, target, replaceData);

            if (replaceData || string.IsNullOrEmpty(target.ForcedSortName))
            {
                var forcedSortName = source.ForcedSortName;

                if (!string.IsNullOrWhiteSpace(forcedSortName))
                {
                    target.ForcedSortName = forcedSortName;
                }
            }

            if (mergeMetadataSettings)
            {
                target.LockedFields = source.LockedFields;
                target.IsLocked = source.IsLocked;

                // Grab the value if it's there, but if not then don't overwrite the default
                if (source.DateCreated != default)
                {
                    target.DateCreated = source.DateCreated;
                }

                target.PreferredMetadataCountryCode = source.PreferredMetadataCountryCode;
                target.PreferredMetadataLanguage = source.PreferredMetadataLanguage;
            }
        }

        private static void MergePeople(List<PersonInfo> source, List<PersonInfo> target)
        {
            foreach (var person in target)
            {
                var normalizedName = person.Name.RemoveDiacritics();
                var personInSource = source.FirstOrDefault(i => string.Equals(i.Name.RemoveDiacritics(), normalizedName, StringComparison.OrdinalIgnoreCase));

                if (personInSource != null)
                {
                    foreach (var providerId in personInSource.ProviderIds)
                    {
                        if (!person.ProviderIds.ContainsKey(providerId.Key))
                        {
                            person.ProviderIds[providerId.Key] = providerId.Value;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(person.ImageUrl))
                    {
                        person.ImageUrl = personInSource.ImageUrl;
                    }
                }
            }
        }

        private static void MergeDisplayOrder(BaseItem source, BaseItem target, bool replaceData)
        {
            if (source is IHasDisplayOrder sourceHasDisplayOrder
                && target is IHasDisplayOrder targetHasDisplayOrder)
            {
                if (replaceData || string.IsNullOrEmpty(targetHasDisplayOrder.DisplayOrder))
                {
                    var displayOrder = sourceHasDisplayOrder.DisplayOrder;

                    if (!string.IsNullOrWhiteSpace(displayOrder))
                    {
                        targetHasDisplayOrder.DisplayOrder = displayOrder;
                    }
                }
            }
        }

        private static void MergeAlbumArtist(BaseItem source, BaseItem target, bool replaceData)
        {
            if (source is IHasAlbumArtist sourceHasAlbumArtist
                && target is IHasAlbumArtist targetHasAlbumArtist)
            {
                if (replaceData || targetHasAlbumArtist.AlbumArtists.Count == 0)
                {
                    targetHasAlbumArtist.AlbumArtists = sourceHasAlbumArtist.AlbumArtists;
                }
            }
        }

        private static void MergeCriticRating(BaseItem source, BaseItem target, bool replaceData)
        {
            if (replaceData || !target.CriticRating.HasValue)
            {
                target.CriticRating = source.CriticRating;
            }
        }

        private static void MergeTrailers(BaseItem source, BaseItem target, bool replaceData)
        {
            if (replaceData || target.RemoteTrailers.Count == 0)
            {
                target.RemoteTrailers = source.RemoteTrailers;
            }
        }

        private static void MergeVideoInfo(BaseItem source, BaseItem target, bool replaceData)
        {
            if (source is Video sourceCast && target is Video targetCast)
            {
                if (replaceData || targetCast.Video3DFormat == null)
                {
                    targetCast.Video3DFormat = sourceCast.Video3DFormat;
                }
            }
        }
    }
}
