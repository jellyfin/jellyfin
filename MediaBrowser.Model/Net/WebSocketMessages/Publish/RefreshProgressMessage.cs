using System.Collections.Generic;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Refresh progress message.
/// </summary>
public class RefreshProgressMessage : WebSocketMessage<Dictionary<string, string>>
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
    public override SessionMessageType MessageType => SessionMessageType.RefreshProgress;
}
