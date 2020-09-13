#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;

namespace Emby.Server.Implementations.Plugins
{
    /// <summary>
    /// Defines a Plugin manifest file.
    /// </summary>
    public class PlugInManifest
    {
        public string Category { get; set; }

        public string Changelog { get; set; }

        public string Description { get; set; }

        public Guid Guid { get; set; }

        public string Name { get; set; }

        public string Overview { get; set; }

        public string Owner { get; set; }

        public string TargetAbi { get; set; }

        public DateTime Timestamp { get; set; }

        public string Version { get; set; }
}
}
