using System;
using Jellyfin.Plugin.PlaybackReporter.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.PlaybackReporter;

/// <summary>
/// The Playback Reporter plugin entry point.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>
{
    /// <summary>
    /// The stable GUID that uniquely identifies this plugin.
    /// </summary>
    public static readonly Guid PluginGuid = new("d3a7a3b2-4f1e-4c8a-9b5d-1f2e3a4b5c6d");

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the running plugin instance. Set during construction; safe to use from services
    /// after DI has finished resolving dependencies.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "Playback Reporter";

    /// <inheritdoc />
    public override Guid Id => PluginGuid;

    /// <inheritdoc />
    public override string Description =>
        "Tracks playback failures, format/codec mismatches, item mismatches, and HDR/Dolby Vision "
        + "compatibility issues. Recorded events can be exported to GitHub Issues for easier bug reporting.";
}
