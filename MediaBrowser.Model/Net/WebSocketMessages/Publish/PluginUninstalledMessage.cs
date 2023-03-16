using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Plugin uninstalled message.
/// </summary>
public class PluginUninstalledMessage : WebSocketMessage<PluginInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginUninstalledMessage"/> class.
    /// </summary>
    /// <param name="data">Plugin info.</param>
    public PluginUninstalledMessage(PluginInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.PackageUninstalled;
}
