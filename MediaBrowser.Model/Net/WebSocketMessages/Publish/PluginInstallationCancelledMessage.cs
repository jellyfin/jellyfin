using MediaBrowser.Model.Session;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Plugin installation cancelled message.
/// </summary>
public class PluginInstallationCancelledMessage : WebSocketMessage<InstallationInfo>
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
    public override SessionMessageType MessageType => SessionMessageType.PackageInstallationCancelled;
}
