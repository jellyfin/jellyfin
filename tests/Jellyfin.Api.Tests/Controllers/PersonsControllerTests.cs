using System;
using System.Collections.Generic;
using System.Security.Claims;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Controllers;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers;

public class PersonsControllerTests
{
    private readonly Mock<ILibraryManager> _mockLibraryManager;
    private readonly Mock<IDtoService> _mockDtoService;
    private readonly Mock<IUserManager> _mockUserManager;
    private readonly PersonsController _subject;

    public PersonsControllerTests()
    {
        _mockLibraryManager = new Mock<ILibraryManager>();
        _mockDtoService = new Mock<IDtoService>();
        _mockUserManager = new Mock<IUserManager>();

        _mockLibraryManager
            .Setup(m => m.GetPeopleItems(It.IsAny<InternalPeopleQuery>()))
            .Returns(new List<Person>());

        _subject = new PersonsController(
            _mockLibraryManager.Object,
            _mockDtoService.Object,
            _mockUserManager.Object);

        // Wire up a ClaimsPrincipal so RequestHelpers.GetUserId doesn't throw.
        var userId = Guid.NewGuid();
        var claims = new[] { new Claim(InternalClaimTypes.UserId, userId.ToString("N")) };
        var identity = new ClaimsIdentity(claims);
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _subject.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public void GetPersons_WithoutParentId_QueryHasEmptyAncestorId()
    {
        _subject.GetPersons(
            limit: null,
            searchTerm: null,
            fields: Array.Empty<ItemFields>(),
            filters: Array.Empty<ItemFilter>(),
            isFavorite: null,
            enableUserData: null,
            imageTypeLimit: null,
            enableImageTypes: Array.Empty<ImageType>(),
            excludePersonTypes: Array.Empty<string>(),
            personTypes: Array.Empty<string>(),
            appearsInItemId: null,
            userId: null,
            enableImages: null,
            parentId: null);

        _mockLibraryManager.Verify(
            m => m.GetPeopleItems(It.Is<InternalPeopleQuery>(q => q.AncestorId.Equals(Guid.Empty))),
            Times.Once);
    }

    [Fact]
    public void GetPersons_WithParentId_QueryHasMatchingAncestorId()
    {
        var parentId = Guid.NewGuid();

        _subject.GetPersons(
            limit: null,
            searchTerm: null,
            fields: Array.Empty<ItemFields>(),
            filters: Array.Empty<ItemFilter>(),
            isFavorite: null,
            enableUserData: null,
            imageTypeLimit: null,
            enableImageTypes: Array.Empty<ImageType>(),
            excludePersonTypes: Array.Empty<string>(),
            personTypes: Array.Empty<string>(),
            appearsInItemId: null,
            userId: null,
            enableImages: null,
            parentId: parentId);

        _mockLibraryManager.Verify(
            m => m.GetPeopleItems(It.Is<InternalPeopleQuery>(q => q.AncestorId.Equals(parentId))),
            Times.Once);
    }

    [Fact]
    public void GetPersons_WithActorPersonType_QueryHasMatchingPersonTypes()
    {
        var personTypes = new[] { "Actor" };

        _subject.GetPersons(
            limit: null,
            searchTerm: null,
            fields: Array.Empty<ItemFields>(),
            filters: Array.Empty<ItemFilter>(),
            isFavorite: null,
            enableUserData: null,
            imageTypeLimit: null,
            enableImageTypes: Array.Empty<ImageType>(),
            excludePersonTypes: Array.Empty<string>(),
            personTypes: personTypes,
            appearsInItemId: null,
            userId: null,
            enableImages: null,
            parentId: null);

        _mockLibraryManager.Verify(
            m => m.GetPeopleItems(It.Is<InternalPeopleQuery>(q =>
                q.PersonTypes.Count.Equals(1) && q.PersonTypes[0].Equals("Actor", StringComparison.Ordinal))),
            Times.Once);
    }

    [Fact]
    public void GetPersons_WithParentIdAndPersonType_BothSetOnQuery()
    {
        var parentId = Guid.NewGuid();
        var personTypes = new[] { "Actor" };

        _subject.GetPersons(
            limit: null,
            searchTerm: null,
            fields: Array.Empty<ItemFields>(),
            filters: Array.Empty<ItemFilter>(),
            isFavorite: null,
            enableUserData: null,
            imageTypeLimit: null,
            enableImageTypes: Array.Empty<ImageType>(),
            excludePersonTypes: Array.Empty<string>(),
            personTypes: personTypes,
            appearsInItemId: null,
            userId: null,
            enableImages: null,
            parentId: parentId);

        _mockLibraryManager.Verify(
            m => m.GetPeopleItems(It.Is<InternalPeopleQuery>(q =>
                q.AncestorId.Equals(parentId) &&
                q.PersonTypes.Count.Equals(1) &&
                q.PersonTypes[0].Equals("Actor", StringComparison.Ordinal))),
            Times.Once);
    }
}
