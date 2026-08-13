using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Configuration;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class ConfigurationControllerTests
{
    [Fact]
    public void GetConfiguration_ReturnsCollectionsPath()
    {
        var configuration = new ServerConfiguration
        {
            CollectionsPath = @"D:\Media\Collections"
        };

        var controller = new ConfigurationController(
            Mock.Of<IServerConfigurationManager>(manager => manager.Configuration == configuration),
            Mock.Of<IMediaEncoder>());

        var result = controller.GetConfiguration();

        Assert.Equal(configuration.CollectionsPath, result.Value!.CollectionsPath);
    }

    [Fact]
    public void UpdateConfiguration_ForwardsCollectionsPath()
    {
        ServerConfiguration? updatedConfiguration = null;
        var configurationManager = new Mock<IServerConfigurationManager>();
        configurationManager.SetupGet(manager => manager.Configuration).Returns(new ServerConfiguration());
        configurationManager
            .Setup(manager => manager.ReplaceConfiguration(It.IsAny<BaseApplicationConfiguration>()))
            .Callback<BaseApplicationConfiguration>(configuration => updatedConfiguration = (ServerConfiguration)configuration);

        var controller = new ConfigurationController(configurationManager.Object, Mock.Of<IMediaEncoder>());

        var result = controller.UpdateConfiguration(new ServerConfiguration
        {
            CollectionsPath = @"D:\Media\Collections"
        });

        Assert.IsType<NoContentResult>(result);
        Assert.NotNull(updatedConfiguration);
        Assert.Equal(@"D:\Media\Collections", updatedConfiguration!.CollectionsPath);
    }
}
