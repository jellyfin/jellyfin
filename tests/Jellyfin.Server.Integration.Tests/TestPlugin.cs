#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Server.Integration.Tests
{
    public class TestPlugin : BasePlugin<BasePluginConfiguration>, IHasWebPages
    {
        public TestPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static TestPlugin? Instance { get; private set; }

        public override Guid Id => new Guid("2d350a13-0bf7-4b61-859c-d5e601b5facf");

        public override string Name => nameof(TestPlugin);

        public override string Description => "Server test Plugin.";

        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = GetType().Namespace + ".TestPage.html"
            };

            yield return new PluginPageInfo
            {
                Name = "BrokenPage",
                EmbeddedResourcePath = GetType().Namespace + ".foobar"
            };
        }
    }
}
