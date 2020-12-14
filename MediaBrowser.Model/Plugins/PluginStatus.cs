#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1602 // Enumeration items should be documented
namespace MediaBrowser.Model.Plugins
{
    /// <summary>
    /// Plugin load status.
    /// </summary>
    public enum PluginStatus
    {
        RestartRequired = 1,
        Active = 0,
        Disabled = -1,
        NotSupported = -2,
        Malfunction = -3,
        Superceded = -4,
        DeleteOnStartup = -5
    }
}
