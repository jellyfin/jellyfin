using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        public Plugin(IKernel kernel, IXmlSerializer xmlSerializer) : base(kernel, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Web Api"; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a core plugin.
        /// </summary>
        /// <value><c>true</c> if this instance is a core plugin; otherwise, <c>false</c>.</value>
        public override bool IsCorePlugin
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }
    }
}
