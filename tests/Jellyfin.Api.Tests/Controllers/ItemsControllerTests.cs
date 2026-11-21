using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class ItemsControllerTests
{
    private readonly Mock<ILibraryManager> _libraryManager = new();
    private readonly Mock<ISearchManager> _searchManager = new();
    private readonly RecordingFolder _folder = new();

    public ItemsControllerTests()
    {
        _libraryManager.Setup(m => m.GetParentItem(It.IsAny<Guid?>(), It.IsAny<Guid?>())).Returns(_folder);
    }

    [Fact]
    public async Task GetItems_SearchFindsNothing_ReturnsEmptyWithoutQueryingTheLibrary()
    {
        _searchManager.Setup(m => m.GetSearchResultsAsync(It.IsAny<SearchProviderQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CallGetItems(searchTerm: "zzqqxx");

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalRecordCount);
        Assert.Empty(_folder.Queries);
    }

    private async Task<QueryResult<BaseItemDto>> CallGetItems(string searchTerm)
    {
        var controller = new ItemsController(
            Mock.Of<IUserManager>(),
            _libraryManager.Object,
            Mock.Of<ILocalizationManager>(),
            Mock.Of<IDtoService>(),
            NullLogger<ItemsController>.Instance,
            Mock.Of<ISessionManager>(),
            Mock.Of<IUserDataManager>(),
            _searchManager.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(InternalClaimTypes.IsApiKey, bool.TrueString)]))
                }
            }
        };

        // GetItems takes one argument per query parameter. Pass the search term and defaults for the rest.
        var method = typeof(ItemsController).GetMethod(nameof(ItemsController.GetItems))!;
        var arguments = method.GetParameters()
            .Select(parameter => parameter.Name switch
            {
                "searchTerm" => searchTerm,
                "recursive" => true,
                _ => DefaultArgument(parameter.ParameterType, parameter.HasDefaultValue ? parameter.DefaultValue : null)
            })
            .ToArray();

        var response = await (Task<ActionResult<QueryResult<BaseItemDto>>>)method.Invoke(controller, arguments)!;
        return response.Value!;
    }

    private static object? DefaultArgument(Type type, object? defaultValue)
    {
        if (defaultValue is not null)
        {
            return defaultValue;
        }

        return type.IsArray ? Array.CreateInstance(type.GetElementType()!, 0) : null;
    }

    private sealed class RecordingFolder : Folder
    {
        public List<InternalItemsQuery> Queries { get; } = [];

        protected override QueryResult<BaseItem> GetItemsInternal(InternalItemsQuery query)
        {
            Queries.Add(query);
            return new QueryResult<BaseItem>();
        }
    }
}
