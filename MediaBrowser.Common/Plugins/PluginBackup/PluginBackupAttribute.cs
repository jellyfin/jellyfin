using System;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Plugins.Backup;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// An attribute that marks a plugin loader class that should have a parametherless constructor and needs to be run standalone for restore purposes.
/// </summary>
/// <typeparam name="TPluginDataLoader">The plugin loader class that is responsible for handling serialization of the plugin data.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class PluginBackupAttribute<TPluginDataLoader> : System.Attribute, IPluginBackupInfoData
    where TPluginDataLoader : IPluginBackupService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginBackupAttribute{TPluginDataLoader}"/> class.
    /// </summary>
    /// <param name="pluginId">The plugins ID. Should match with the <see cref="IPlugin.Id"/> value and needs to be an <see cref="Guid"/>.</param>
    /// <param name="name">The plugins Name. Should match with the <see cref="IPlugin.Name"/> value.</param>
#pragma warning disable CA1019 // Define accessors for attribute arguments
    public PluginBackupAttribute(string pluginId, string name)
#pragma warning restore CA1019 // Define accessors for attribute arguments
    {
        Id = Guid.Parse(pluginId);
        Name = name;
    }

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    Type IPluginBackupInfoData.LoaderType => typeof(TPluginDataLoader);
}
