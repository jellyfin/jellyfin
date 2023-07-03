using System.ComponentModel;
using MediaBrowser.Model.Session;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Plugin installation failed message.
/// </summary>
public class PluginInstallationFailedMessage : OutboundWebSocketMessage<InstallationInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstallationFailedMessage"/> class.
    /// </summary>
    /// <param name="data">Installation info.</param>
    public PluginInstallationFailedMessage(InstallationInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    [DefaultValue(SessionMessageType.PackageInstallationFailed)]
    public override SessionMessageType MessageType => SessionMessageType.PackageInstallationFailed;
}
