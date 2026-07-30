#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.CustomNetflix;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixProfileSelectionPolicy
{
    public static CustomNetflixProfileDto? SelectProfile(IReadOnlyList<CustomNetflixProfileDto> profiles, Guid? preferredProfileId)
    {
        if (profiles.Count == 0)
        {
            return null;
        }

        if (preferredProfileId.HasValue)
        {
            var preferred = profiles.FirstOrDefault(profile => profile.Id.Equals(preferredProfileId.Value));
            if (preferred is not null)
            {
                return preferred;
            }
        }

        return profiles.FirstOrDefault(profile => profile.IsDefault) ?? profiles[0];
    }
}
