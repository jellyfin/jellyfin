using MediaBrowser.Model.Session;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Package installing message.
/// </summary>
public class PluginInstallingMessage : WebSocketMessage<InstallationInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginInstallingMessage"/> class.
    /// </summary>
    /// <param name="data">Installation info.</param>
    public PluginInstallingMessage(InstallationInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.PackageInstalling;
}
