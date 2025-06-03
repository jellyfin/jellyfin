using System;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using Microsoft.Extensions.DependencyInjection;
using MediaBrowser.Controller.Plugins;

namespace Jellyfin.Plugin.TestPlugin
{
    public class TestPlugin : BasePlugin, IPluginServiceRegistrator
    {
        public override string Name => "Test Plugin";
        public override Guid Id => Guid.Parse("00000000-0000-0000-0000-000000000001");

        public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
        {
            serviceCollection.AddTransient<ITestService, TestService>();
        }
    }

    public interface ITestService
    {
        string GetMessage();
    }

    public class TestService : ITestService
    {
        public string GetMessage()
        {
            return "Hello from TestService!";
        }
    }
}
