using System;

namespace MediaBrowser.Model.SyncPlay;

/// <summary>
/// Snapshot of a SyncPlay group state for fast client recovery.
/// </summary>
public class SyncPlayGroupSnapshotDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlayGroupSnapshotDto"/> class.
    /// </summary>
    /// <param name="groupInfo">The group info.</param>
    /// <param name="playQueue">The play queue snapshot.</param>
    /// <param name="playingCommand">The current playing command, if any.</param>
    /// <param name="revision">The monotonic revision for this snapshot.</param>
    /// <param name="serverUtcNow">The current server UTC time.</param>
    public SyncPlayGroupSnapshotDto(
        GroupInfoDto groupInfo,
        PlayQueueUpdate playQueue,
        SendCommand? playingCommand,
        long revision,
        DateTime serverUtcNow)
    {
        GroupInfo = groupInfo;
        PlayQueue = playQueue;
        PlayingCommand = playingCommand;
        Revision = revision;
        ServerUtcNow = serverUtcNow;
    }

    /// <summary>
    /// Gets the group info.
    /// </summary>
    public GroupInfoDto GroupInfo { get; }

    /// <summary>
    /// Gets the play queue snapshot.
    /// </summary>
    public PlayQueueUpdate PlayQueue { get; }

    /// <summary>
    /// Gets the current playing command, if any.
    /// </summary>
    public SendCommand? PlayingCommand { get; }

    /// <summary>
    /// Gets the monotonic revision for this snapshot.
    /// </summary>
    public long Revision { get; }

    /// <summary>
    /// Gets the current server UTC time.
    /// </summary>
    public DateTime ServerUtcNow { get; }
}
