using System;
using System.Collections.Generic;
using System.Reflection;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

/// <summary>
/// Guards how the share-link marker is detected during authentication.
/// </summary>
/// <remarks>
/// The attribute is applied to controller actions, so at runtime it is surfaced through the
/// <see cref="ControllerActionDescriptor"/> rather than as a standalone metadata item. The
/// handler must find it there: when it only looked for a standalone item, every share link
/// failed with a bare 401 and no <c>X-ShareLink-Error</c> header.
/// </remarks>
public sealed class ShareLinkAccessAttributeTests
{
    [Fact]
    public void IsMarked_FindsAttributeThroughControllerActionDescriptor()
    {
        var method = typeof(VideosController).GetMethod(nameof(VideosController.GetVideoStream));

        Assert.NotNull(method);
        var endpoint = CreateEndpoint(method!);

        Assert.True(ShareLinkAccessAttribute.IsMarked(endpoint));
    }

    [Fact]
    public void IsMarked_ReturnsFalseForUnmarkedAction()
    {
        var method = typeof(UserController).GetMethod(nameof(UserController.GetCurrentUser));

        Assert.NotNull(method);
        var endpoint = CreateEndpoint(method!);

        Assert.False(ShareLinkAccessAttribute.IsMarked(endpoint));
    }

    [Fact]
    public void IsMarked_ReturnsFalseWhenNoEndpoint()
    {
        Assert.False(ShareLinkAccessAttribute.IsMarked(null));
    }

    private static Endpoint CreateEndpoint(MethodInfo method)
    {
        var action = new ControllerActionDescriptor
        {
            MethodInfo = method,
            ControllerTypeInfo = method.DeclaringType!.GetTypeInfo()
        };

        var metadata = new List<object> { action };
        return new Endpoint(_ => System.Threading.Tasks.Task.CompletedTask, new EndpointMetadataCollection(metadata), method.Name);
    }
}
