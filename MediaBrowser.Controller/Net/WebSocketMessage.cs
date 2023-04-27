using System;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net;

/// <summary>
/// Websocket message without data.
/// </summary>
public abstract class WebSocketMessage
{
    /// <summary>
    /// Gets or sets the type of the message.
    /// TODO make this abstract and get only.
    /// </summary>
    public virtual SessionMessageType MessageType { get; set; }

    /// <summary>
    /// Gets or sets the message id.
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Gets or sets the server id.
    /// </summary>
    public string? ServerId { get; set; }
}
