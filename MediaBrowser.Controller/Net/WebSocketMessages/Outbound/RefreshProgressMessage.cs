using System.Collections.Generic;
using System.ComponentModel;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Refresh progress message.
/// </summary>
public class RefreshProgressMessage : OutboundWebSocketMessage<Dictionary<string, string>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshProgressMessage"/> class.
    /// </summary>
    /// <param name="data">Refresh progress data.</param>
    public RefreshProgressMessage(Dictionary<string, string> data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.RefreshProgress)]
    public override SessionMessageType MessageType => SessionMessageType.RefreshProgress;
}
