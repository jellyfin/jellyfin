namespace MediaBrowser.Model.Plugins
{
    /// <summary>
    /// Plugin load status.
    /// </summary>
    public enum PluginStatus
    {
        /// <summary>
        /// This plugin requires a restart in order for it to load. This is a memory only status.
        /// The actual status of the plugin after reload is present in the manifest.
        /// eg. A disabled plugin will still be active until the next restart, and so will have a memory status of Restart,
        /// but a disk manifest status of Disabled.
        /// </summary>
        Restart = 1,

        /// <summary>
        /// This plugin is currently running.
        /// </summary>
        Active = 0,

        /// <summary>
        /// This plugin has been marked as disabled.
        /// </summary>
        Disabled = -1,

        /// <summary>
        /// This plugin does not meet the TargetAbi requirements.
        /// </summary>
        NotSupported = -2,

        /// <summary>
        /// This plugin caused an error when instantiated (either DI loop, or exception).
        /// </summary>
        Malfunctioned = -3,

        /// <summary>
        /// This plugin has been superseded by another version.
        /// </summary>
        Superseded = -4,

        /// <summary>
        /// [DEPRECATED] See Superseded.
        /// </summary>
        Superceded = -4,

        /// <summary>
        /// An attempt to remove this plugin from disk will happen at every restart.
        /// It will not be loaded, if unable to do so.
        /// </summary>
        Deleted = -5
    }
}
