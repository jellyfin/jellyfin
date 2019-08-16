using System;
using IsoMounter.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace IsoMounter
{
    /// <summary>
    /// The LinuxMount plugin class.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
        }

        /// <inheritdoc />
        public override Guid Id { get; } = new Guid("4682DD4C-A675-4F1B-8E7C-79ADF137A8F8");

        /// <inheritdoc />
        public override string Name => "Iso Mounter";

        /// <inheritdoc />
        public override string Description => "Mount and stream ISO contents";
    }
}
