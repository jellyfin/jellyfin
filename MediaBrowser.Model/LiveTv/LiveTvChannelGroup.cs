using System;

namespace MediaBrowser.Model.LiveTv;

/// <summary>
/// A group of live TV channels, as declared by the tuner (for an M3U tuner: the <c>group-title</c> attribute).
/// </summary>
public class LiveTvChannelGroup
{
    /// <summary>
    /// Gets the stable id of the group. It is derived from the normalized name, so it is the same across
    /// guide refreshes and servers, and two groups whose names differ only in case or diacritics share it.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the display name of the group.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the number of channels in the group that are visible to the requesting user.
    /// </summary>
    public required int ChannelCount { get; init; }
}
