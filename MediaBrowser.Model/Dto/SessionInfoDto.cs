using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Dto;

/// <summary>
/// Session info DTO.
/// </summary>
public class SessionInfoDto
{
    /// <summary>
    /// Gets or sets the play state.
    /// </summary>
    /// <value>The play state.</value>
    public PlayerStateInfo? PlayState { get; set; }

    /// <summary>
    /// Gets or sets the additional users.
    /// </summary>
    /// <value>The additional users.</value>
    public IReadOnlyList<SessionUserInfo>? AdditionalUsers { get; set; }

    /// <summary>
    /// Gets or sets the client capabilities.
    /// </summary>
    /// <value>The client capabilities.</value>
    public ClientCapabilitiesDto? Capabilities { get; set; }

    /// <summary>
    /// Gets or sets the remote end point.
    /// </summary>
    /// <value>The remote end point.</value>
    public string? RemoteEndPoint { get; set; }

    /// <summary>
    /// Gets or sets the playable media types.
    /// </summary>
    /// <value>The playable media types.</value>
    public IReadOnlyList<MediaType> PlayableMediaTypes { get; set; } = [];

    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    /// <value>The id.</value>
    public string? Id { get; set; }

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
    /// Gets or sets the last paused date.
    /// </summary>
    /// <value>The last paused date.</value>
    public DateTime? LastPausedDate { get; set; }

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

    /// <summary>
    /// Gets or sets the now viewing item.
    /// </summary>
    /// <value>The now viewing item.</value>
    public BaseItemDto? NowViewingItem { get; set; }

    /// <summary>
    /// Gets or sets the device id.
    /// </summary>
    /// <value>The device id.</value>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the application version.
    /// </summary>
    /// <value>The application version.</value>
    public string? ApplicationVersion { get; set; }

    /// <summary>
    /// Gets or sets the transcoding info.
    /// </summary>
    /// <value>The transcoding info.</value>
    public TranscodingInfo? TranscodingInfo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this session is active.
    /// </summary>
    /// <value><c>true</c> if this session is active; otherwise, <c>false</c>.</value>
    public bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the session supports media control.
    /// </summary>
    /// <value><c>true</c> if this session supports media control; otherwise, <c>false</c>.</value>
    public bool SupportsMediaControl { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the session supports remote control.
    /// </summary>
    /// <value><c>true</c> if this session supports remote control; otherwise, <c>false</c>.</value>
    public bool SupportsRemoteControl { get; set; }

    /// <summary>
    /// Gets or sets the now playing queue.
    /// </summary>
    /// <value>The now playing queue.</value>
    public IReadOnlyList<QueueItem>? NowPlayingQueue { get; set; }

    /// <summary>
    /// Gets or sets the now playing queue full items.
    /// </summary>
    /// <value>The now playing queue full items.</value>
    public IReadOnlyList<BaseItemDto>? NowPlayingQueueFullItems { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the session has a custom device name.
    /// </summary>
    /// <value><c>true</c> if this session has a custom device name; otherwise, <c>false</c>.</value>
    public bool HasCustomDeviceName { get; set; }

    /// <summary>
    /// Gets or sets the playlist item id.
    /// </summary>
    /// <value>The playlist item id.</value>
    public string? PlaylistItemId { get; set; }

    /// <summary>
    /// Gets or sets the server id.
    /// </summary>
    /// <value>The server id.</value>
    public string? ServerId { get; set; }

    /// <summary>
    /// Gets or sets the user primary image tag.
    /// </summary>
    /// <value>The user primary image tag.</value>
    public string? UserPrimaryImageTag { get; set; }

    /// <summary>
    /// Gets or sets the supported commands.
    /// </summary>
    /// <value>The supported commands.</value>
    public IReadOnlyList<GeneralCommandType> SupportedCommands { get; set; } = [];
}
