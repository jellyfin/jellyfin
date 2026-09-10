using System.Reflection;
using Jellyfin.Api.Controllers;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class ConfigurationControllerTests
{
    [Fact]
    public void Controller_RequiresElevation()
    {
        Assert.Contains(
            typeof(ConfigurationController).GetCustomAttributes<AuthorizeAttribute>(),
            attribute => attribute.Policy == Policies.RequiresElevation);
    }
}
