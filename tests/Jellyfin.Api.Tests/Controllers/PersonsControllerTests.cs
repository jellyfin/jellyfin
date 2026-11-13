using System;
using System.Security.Claims;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class PersonsControllerTests
{
    private readonly Mock<ILibraryManager> _libraryManager = new();

    public PersonsControllerTests()
    {
        _libraryManager.Setup(m => m.GetPeopleItems(It.IsAny<InternalPeopleQuery>()))
            .Returns(new QueryResult<BaseItem>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetPersons_EnableTotalRecordCount_PassedToQuery(bool enableTotalRecordCount)
    {
        CallGetPersons(enableTotalRecordCount);

        _libraryManager.Verify(m => m.GetPeopleItems(It.Is<InternalPeopleQuery>(q => q.EnableTotalRecordCount == enableTotalRecordCount)));
    }

    private void CallGetPersons(bool enableTotalRecordCount)
    {
        var controller = new PersonsController(_libraryManager.Object, Mock.Of<IDtoService>(), Mock.Of<IUserManager>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        controller.GetPersons(
            startIndex: null,
            limit: 24,
            searchTerm: "seven",
            nameStartsWith: null,
            nameLessThan: null,
            nameStartsWithOrGreater: null,
            fields: [],
            filters: [],
            isFavorite: null,
            enableUserData: null,
            imageTypeLimit: null,
            enableImageTypes: [],
            excludePersonTypes: [],
            personTypes: [],
            parentId: null,
            appearsInItemId: null,
            userId: null,
            enableTotalRecordCount: enableTotalRecordCount);
    }
}
