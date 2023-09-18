using System.Text.Json.Serialization;
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
    /// Gets or sets the server id.
    /// </summary>
    [JsonIgnore]
    public string? ServerId { get; set; }
}
