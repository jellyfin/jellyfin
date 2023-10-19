using System.ComponentModel;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Controller.Net.WebSocketMessages.Outbound;

/// <summary>
/// Plugin uninstalled message.
/// </summary>
public class PluginUninstalledMessage : OutboundWebSocketMessage<PluginInfo>
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
    [DefaultValue(SessionMessageType.PackageUninstalled)]
    public override SessionMessageType MessageType => SessionMessageType.PackageUninstalled;
}
