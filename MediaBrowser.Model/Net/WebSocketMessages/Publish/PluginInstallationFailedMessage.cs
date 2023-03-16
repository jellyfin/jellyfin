using MediaBrowser.Model.Session;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Plugin installation failed message.
/// </summary>
public class PluginInstallationFailedMessage : WebSocketMessage<InstallationInfo>
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
    public override SessionMessageType MessageType => SessionMessageType.PackageInstallationFailed;
}
