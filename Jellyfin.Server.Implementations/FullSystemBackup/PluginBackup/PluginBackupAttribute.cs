using System;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// An attribute that marks a plugin loader class that should have a parametherless constructor and needs to be run standalone for restore purposes.
/// </summary>
/// <typeparam name="TPluginDataLoader">The plugin loader class that is responsible for handling serialization of the plugin data.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class PluginBackupAttribute<TPluginDataLoader> : System.Attribute, IPluginBackupAttribute
    where TPluginDataLoader : IPluginBackupService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginBackupAttribute{TPluginDataLoader}"/> class.
    /// </summary>
    /// <param name="id">The plugins ID. Should match with the <see cref="IPlugin.Id"/> value and needs to be an <see cref="Guid"/>.</param>
    /// <param name="name">The plugins Name. Should match with the <see cref="IPlugin.Name"/> value.</param>
    public PluginBackupAttribute(string id, string name)
    {
        Id = Guid.Parse(id);
        Name = name;
    }

    /// <inheritdoc/>
    public Guid Id { get; }

    /// <inheritdoc/>
    public string Name { get; }

    Type IPluginBackupAttribute.LoaderType => typeof(TPluginDataLoader);
}
