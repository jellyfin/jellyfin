#pragma warning disable CS1591

using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Server.Integration.Tests
{
    public class TestPluginWithoutPages : BasePlugin<BasePluginConfiguration>
    {
        public TestPluginWithoutPages(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static TestPluginWithoutPages? Instance { get; private set; }

        public override Guid Id => new Guid("ae95cbe6-bd3d-4d73-8596-490db334611e");

        public override string Name => nameof(TestPluginWithoutPages);

        public override string Description => "Server test Plugin without web pages.";
    }
}
