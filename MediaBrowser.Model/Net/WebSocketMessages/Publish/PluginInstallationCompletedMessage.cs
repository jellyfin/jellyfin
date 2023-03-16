using MediaBrowser.Model.Session;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Plugin installation completed message.
/// </summary>
public class PluginInstallationCompletedMessage : WebSocketMessage<InstallationInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstallationCompletedMessage"/> class.
    /// </summary>
    /// <param name="data">Installation info.</param>
    public PluginInstallationCompletedMessage(InstallationInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.PackageInstallationCompleted;
}
