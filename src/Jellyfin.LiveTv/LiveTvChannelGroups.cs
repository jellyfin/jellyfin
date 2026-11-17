using System;
using Jellyfin.Extensions;
using MediaBrowser.Common.Extensions;

namespace Jellyfin.LiveTv;

/// <summary>
/// Identity of live TV channel groups. A group is a name on the tuner side (an M3U <c>group-title</c>),
/// not a library item, so its id is derived from that name.
/// </summary>
public static class LiveTvChannelGroups
{
    private const string IdPrefix = "LiveTvChannelGroup|";

    /// <summary>
    /// Gets the id of the group with the given name.
    /// </summary>
    /// <param name="name">The group name as declared by the tuner.</param>
    /// <returns>A stable id: the same for every spelling of the name that normalizes to the same clean value.</returns>
    public static Guid GetId(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return (IdPrefix + name.Trim().GetCleanValue()).GetMD5();
    }
}
