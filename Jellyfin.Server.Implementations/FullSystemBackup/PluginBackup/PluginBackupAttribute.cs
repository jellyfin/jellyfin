using System;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// An attribute that marks a plugin loader class that should have a parametherless constructor and needs to be run standalone for restore purposes.
/// </summary>
/// <typeparam name="TPluginDataLoader">The plugin loader class that is responsible for handling serialization of the plugin data.</typeparam>
[AttributeUsage(System.AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class PluginBackupAttribute<TPluginDataLoader> : System.Attribute, IPluginBackupAttribute
    where TPluginDataLoader : IPluginBackupService
{
    Type IPluginBackupAttribute.LoaderType => typeof(TPluginDataLoader);
}
