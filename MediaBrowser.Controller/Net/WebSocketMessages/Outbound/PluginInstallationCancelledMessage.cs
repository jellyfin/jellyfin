using System.ComponentModel;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Plugin installation cancelled message.
/// </summary>
public class PluginInstallationCancelledMessage : OutboundWebSocketMessage<InstallationInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstallationCancelledMessage"/> class.
    /// </summary>
    /// <param name="data">Installation info.</param>
    public PluginInstallationCancelledMessage(InstallationInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.PackageInstallationCancelled)]
    public override SessionMessageType MessageType => SessionMessageType.PackageInstallationCancelled;
}
