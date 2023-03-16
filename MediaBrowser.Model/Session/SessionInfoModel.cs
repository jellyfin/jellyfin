using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Session;

/// <summary>
/// Session info model.
/// </summary>
public abstract class SessionInfoModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionInfoModel"/> class.
    /// </summary>
    protected SessionInfoModel()
    {
        AdditionalUsers = Array.Empty<SessionUserInfo>();
        PlayState = new PlayerStateInfo();
        NowPlayingQueue = Array.Empty<QueueItem>();
        NowPlayingQueueFullItems = Array.Empty<BaseItemDto>();
    }

    /// <summary>
    /// Gets or sets the current playstate.
    /// </summary>
    public PlayerStateInfo PlayState { get; set; }

    /// <summary>
    /// Gets or sets the additional users in the session.
    /// </summary>
    public SessionUserInfo[] AdditionalUsers { get; set; }

    /// <summary>
    /// Gets or sets the client capabilities.
    /// </summary>
    public ClientCapabilities? Capabilities { get; set; }

    /// <summary>
    /// Gets or sets the remote end point.
    /// </summary>
    /// <value>The remote end point.</value>
    public string? RemoteEndPoint { get; set; }

    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    /// <value>The id.</value>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    /// <value>The user id.</value>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the username.
    /// </summary>
    /// <value>The username.</value>
    public string? UserName { get; set; }

    /// <summary>
    /// Gets or sets the type of the client.
    /// </summary>
    /// <value>The type of the client.</value>
    public string? Client { get; set; }

    /// <summary>
    /// Gets or sets the last activity date.
    /// </summary>
    /// <value>The last activity date.</value>
    public DateTime LastActivityDate { get; set; }

    /// <summary>
    /// Gets or sets the last playback check in.
    /// </summary>
    /// <value>The last playback check in.</value>
    public DateTime LastPlaybackCheckIn { get; set; }

    /// <summary>
    /// Gets or sets the name of the device.
    /// </summary>
    /// <value>The name of the device.</value>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets the type of the device.
    /// </summary>
    /// <value>The type of the device.</value>
    public string? DeviceType { get; set; }

    /// <summary>
    /// Gets or sets the now playing item.
    /// </summary>
    /// <value>The now playing item.</value>
    public BaseItemDto? NowPlayingItem { get; set; }

    // TODO public BaseItem? FullNowPlayingItem { get; set; }

    /// <summary>
    /// Gets or sets the now viewing item.
    /// </summary>
    public BaseItemDto? NowViewingItem { get; set; }

    /// <summary>
    /// Gets or sets the device id.
    /// </summary>
    /// <value>The device id.</value>
    public string DeviceId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    /// <value>The application version.</value>
    public string? ApplicationVersion { get; set; }

    /// <summary>
    /// Gets or sets the transcoding info.
    /// </summary>
    public TranscodingInfo? TranscodingInfo { get; set; }

    /// <summary>
    /// Gets or sets the list of items in the now playing queue.
    /// </summary>
    public IReadOnlyList<QueueItem> NowPlayingQueue { get; set; }

    /// <summary>
    /// Gets or sets the list of full items in the now playing queue.
    /// </summary>
    public IReadOnlyList<BaseItemDto> NowPlayingQueueFullItems { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this session has a custom device name.
    /// </summary>
    public bool HasCustomDeviceName { get; set; }

    /// <summary>
    /// Gets or sets the playlist item id.
    /// </summary>
    public string? PlaylistItemId { get; set; }

    /// <summary>
    /// Gets or sets the server id.
    /// </summary>
    public string? ServerId { get; set; }

    /// <summary>
    /// Gets or sets the user primary image tag.
    /// </summary>
    public string? UserPrimaryImageTag { get; set; }

    /// <summary>
    /// Gets the playable media types.
    /// </summary>
    /// <value>The playable media types.</value>
    public abstract IReadOnlyList<string> PlayableMediaTypes { get; }

    /// <summary>
    /// Gets a value indicating whether this instance is active.
    /// </summary>
    public abstract bool IsActive { get; }

    /// <summary>
    /// Gets a value indicating whether this session supports media control.
    /// </summary>
    public abstract bool SupportsMediaControl { get; }

    /// <summary>
    /// Gets a value indicating whether this session supports remote control.
    /// </summary>
    public abstract bool SupportsRemoteControl { get; }

    /// <summary>
    /// Gets the supported commands.
    /// </summary>
    public abstract IReadOnlyList<GeneralCommandType> SupportedCommands { get; }
}
