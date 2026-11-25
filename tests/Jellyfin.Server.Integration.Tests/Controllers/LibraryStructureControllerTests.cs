using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.LibraryStructureDto;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.v3.Priority;

namespace Jellyfin.Server.Integration.Tests.Controllers;

[TestCaseOrderer(typeof(PriorityOrderer))]
public sealed class LibraryStructureControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
    private static string? _accessToken;

    public LibraryStructureControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    [Priority(-3)]
    public async Task AddVirtualFolder_WithWarmDirectoryServiceCache_InvalidatesTheParentListing()
    {
        const string Name = "stale-cache-test";

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var directoryService = _factory.Services.GetRequiredService<IDirectoryService>();
        var rootFolderPath = _factory.Services.GetRequiredService<IServerApplicationPaths>().DefaultUserViewsPath;

        // Cache a listing of the libraries root taken before the new folder exists. Everything
        // resolving through this DirectoryService keeps reading that listing until it is dropped,
        // so the library stays invisible. Making the caches shared once turned this into a real
        // test failure, see UpdateLibraryOptions_Valid_Success.
        Assert.DoesNotContain(
            directoryService.GetFileSystemEntries(rootFolderPath),
            x => string.Equals(x.Name, Name, StringComparison.Ordinal));

        var body = new AddVirtualFolderDto()
        {
            LibraryOptions = new LibraryOptions()
            {
                Enabled = false
            }
        };

        using var response = await client.PostAsJsonAsync($"Library/VirtualFolders?name={Name}&refreshLibrary=false", body, _jsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Assert.Contains(
            directoryService.GetFileSystemEntries(rootFolderPath),
            x => string.Equals(x.Name, Name, StringComparison.Ordinal));

        using var cleanup = await client.DeleteAsync($"Library/VirtualFolders?name={Name}&refreshLibrary=false", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, cleanup.StatusCode);
    }

    [Fact]
    [Priority(-1)]
    public async Task Post_NewVirtualFolder_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var body = new AddVirtualFolderDto()
        {
            LibraryOptions = new LibraryOptions()
            {
                Enabled = false
            }
        };

        using var response = await client.PostAsJsonAsync("Library/VirtualFolders?name=test&refreshLibrary=true", body, _jsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    [Priority(-2)]
    public async Task UpdateLibraryOptions_Invalid_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var body = new UpdateLibraryOptionsDto()
        {
            Id = Guid.NewGuid(),
            LibraryOptions = new LibraryOptions()
        };

        using var response = await client.PostAsJsonAsync("Library/VirtualFolders/LibraryOptions", body, _jsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Priority(-2)]
    public async Task UpdateLibraryOptions_Valid_Success()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var createBody = new AddVirtualFolderDto()
        {
            LibraryOptions = new LibraryOptions()
            {
                Enabled = false
            }
        };

        using var createResponse = await client.PostAsJsonAsync("Library/VirtualFolders?name=test&refreshLibrary=true", createBody, _jsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);

        await Task.Delay(2000, TestContext.Current.CancellationToken).ConfigureAwait(true);

        using var response = await client.GetAsync("Library/VirtualFolders", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var library = await response.Content.ReadFromJsonAsAsyncEnumerable<VirtualFolderInfo>(_jsonOptions, TestContext.Current.CancellationToken)
            .FirstOrDefaultAsync(x => string.Equals(x?.Name, "test", StringComparison.Ordinal), TestContext.Current.CancellationToken);
        Assert.NotNull(library);

        var options = library.LibraryOptions;
        Assert.NotNull(options);
        Assert.False(options.Enabled);
        options.Enabled = true;

        var body = new UpdateLibraryOptionsDto()
        {
            Id = Guid.Parse(library.ItemId),
            LibraryOptions = options
        };

        using var response2 = await client.PostAsJsonAsync("Library/VirtualFolders/LibraryOptions", body, _jsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response2.StatusCode);
    }

    [Fact]
    [Priority(1)]
    public async Task DeleteLibrary_Invalid_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var response = await client.DeleteAsync("Library/VirtualFolders?name=doesntExist", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [Priority(1)]
    [InlineData("..")]
    [InlineData("../..")]
    [InlineData(".")]
    [InlineData("test/../..")]
    [InlineData("/var/lib/jellyfin/data")]
    public async Task DeleteLibrary_PathTraversal_NotFound(string name)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var response = await client.DeleteAsync($"Library/VirtualFolders?name={Uri.EscapeDataString(name)}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [Priority(1)]
    [InlineData("..")]
    [InlineData("../..")]
    [InlineData(".")]
    [InlineData("test/../..")]
    [InlineData("/var/lib/jellyfin/data")]
    public async Task RenameLibrary_PathTraversalNewName_BadRequest(string newName)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var response = await client.PostAsync(
            $"Library/VirtualFolders/Name?name=test&newName={Uri.EscapeDataString(newName)}",
            null,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [Priority(1)]
    [InlineData("..")]
    [InlineData("../..")]
    [InlineData("/var/lib/jellyfin/data")]
    public async Task RenameLibrary_PathTraversalName_NotFound(string name)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var response = await client.PostAsync(
            $"Library/VirtualFolders/Name?name={Uri.EscapeDataString(name)}&newName=renamed",
            null,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Priority(1)]
    public async Task DeleteLibrary_Valid_Success()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var response = await client.DeleteAsync("Library/VirtualFolders?name=test&refreshLibrary=true", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    [Priority(1)]
    public async Task RenameLibrary_UserHasLibraryPreferences_RemapsLibraryId()
    {
        const string Name = "rename-prefs-old";
        const string NewName = "rename-prefs-new";

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var libraryManager = _factory.Services.GetRequiredService<ILibraryManager>();
        var userManager = _factory.Services.GetRequiredService<IUserManager>();
        var rootFolderPath = _factory.Services.GetRequiredService<IServerApplicationPaths>().DefaultUserViewsPath;

        var body = new AddVirtualFolderDto() { LibraryOptions = new LibraryOptions() };
        using var createResponse = await client.PostAsJsonAsync($"Library/VirtualFolders?name={Name}&collectionType=movies&refreshLibrary=true", body, _jsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);

        var oldId = libraryManager.GetNewItemId(Path.Combine(rootFolderPath, Name), typeof(CollectionFolder));
        var newId = libraryManager.GetNewItemId(Path.Combine(rootFolderPath, NewName), typeof(CollectionFolder));
        Assert.NotEqual(oldId, newId);

        var oldLibrary = libraryManager.GetUserRootFolder().Children.OfType<CollectionFolder>().Single(f => f.Id.Equals(oldId));

        var otherId = Guid.NewGuid();
        var user = await userManager.CreateUserAsync("rename-prefs-user");
        user.SetPermission(PermissionKind.EnableAllFolders, false);
        user.SetPreference(PreferenceKind.EnabledFolders, [otherId, oldId, newId]);
        user.SetPreference(PreferenceKind.BlockedMediaFolders, [oldId]);
        user.SetPreference(PreferenceKind.EnableContentDeletionFromFolders, [oldId, otherId]);
        user.SetPreference(PreferenceKind.LatestItemExcludes, [oldId]);

        // The views built from the library take its id and name as input, so they move with it.
        var oldShadowId = libraryManager.GetShadowView(oldLibrary, oldLibrary.CollectionType, string.Empty).Id;
        var oldUserViewId = libraryManager.GetNamedView(user, oldLibrary.Name, oldId, oldLibrary.CollectionType, string.Empty).Id;
        user.SetPreference(PreferenceKind.MyMediaExcludes, [otherId.ToString(), oldId.ToString(), oldShadowId.ToString("N", CultureInfo.InvariantCulture)]);
        user.SetPreference(PreferenceKind.GroupedFolders, [oldId]);
        user.SetPreference(PreferenceKind.OrderedViews, [otherId.ToString(), oldUserViewId.ToString(), oldShadowId.ToString(), oldId.ToString()]);
        user.SetPreference(PreferenceKind.BlockedTags, ["keep"]);
        await userManager.UpdateUserAsync(user);

        using var renameResponse = await client.PostAsync(
            $"Library/VirtualFolders/Name?name={Name}&newName={NewName}&refreshLibrary=true",
            null,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, renameResponse.StatusCode);

        var reloaded = userManager.GetUserById(user.Id)!;
        Assert.Equal([otherId, newId], reloaded.GetPreferenceValues<Guid>(PreferenceKind.EnabledFolders));
        Assert.Equal([newId], reloaded.GetPreferenceValues<Guid>(PreferenceKind.BlockedMediaFolders));
        Assert.Equal([newId, otherId], reloaded.GetPreferenceValues<Guid>(PreferenceKind.EnableContentDeletionFromFolders));
        Assert.Equal([newId], reloaded.GetPreferenceValues<Guid>(PreferenceKind.LatestItemExcludes));
        // The endpoint refreshes in the background; make sure the renamed library has been picked up.
        await libraryManager.ValidateTopLibraryFolders(CancellationToken.None, true);
        var newLibrary = libraryManager.GetUserRootFolder().Children.OfType<CollectionFolder>().Single(f => f.Id.Equals(newId));
        var newShadowId = libraryManager.GetShadowView(newLibrary, newLibrary.CollectionType, string.Empty).Id;
        var newUserViewId = libraryManager.GetNamedView(reloaded, newLibrary.Name, newId, newLibrary.CollectionType, string.Empty).Id;
        Assert.NotEqual(oldShadowId, newShadowId);
        Assert.NotEqual(oldUserViewId, newUserViewId);
        Assert.Equal(
            [otherId.ToString(), newId.ToString(), newShadowId.ToString("N", CultureInfo.InvariantCulture)],
            reloaded.GetPreference(PreferenceKind.MyMediaExcludes));
        Assert.Equal([newId], reloaded.GetPreferenceValues<Guid>(PreferenceKind.GroupedFolders));
        Assert.Equal(
            [otherId.ToString(), newUserViewId.ToString(), newShadowId.ToString(), newId.ToString()],
            reloaded.GetPreference(PreferenceKind.OrderedViews));
        Assert.Equal(["keep"], reloaded.GetPreference(PreferenceKind.BlockedTags));

        // With the block list cleared, the enabled list alone must let the user see the renamed library.
        reloaded.SetPreference(PreferenceKind.BlockedMediaFolders, Array.Empty<Guid>());
        await userManager.UpdateUserAsync(reloaded);
        Assert.True(newLibrary.IsVisible(userManager.GetUserById(user.Id)!));

        using var cleanup = await client.DeleteAsync($"Library/VirtualFolders?name={NewName}&refreshLibrary=false", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, cleanup.StatusCode);
    }

    [Fact]
    [Priority(1)]
    public async Task RenameLibrary_CapitalizationOnly_LeavesPreferencesAlone()
    {
        const string Name = "rename-case-test";

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var libraryManager = _factory.Services.GetRequiredService<ILibraryManager>();
        var userManager = _factory.Services.GetRequiredService<IUserManager>();
        var rootFolderPath = _factory.Services.GetRequiredService<IServerApplicationPaths>().DefaultUserViewsPath;

        var body = new AddVirtualFolderDto() { LibraryOptions = new LibraryOptions() };
        using var createResponse = await client.PostAsJsonAsync($"Library/VirtualFolders?name={Name}&refreshLibrary=false", body, _jsonOptions, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);

        var oldId = libraryManager.GetNewItemId(Path.Combine(rootFolderPath, Name), typeof(CollectionFolder));
        var newId = libraryManager.GetNewItemId(Path.Combine(rootFolderPath, Name.ToUpperInvariant()), typeof(CollectionFolder));
        var oldLibrary = libraryManager.GetUserRootFolder().Children.OfType<CollectionFolder>().Single(f => f.Id.Equals(oldId));
        var oldShadowId = libraryManager.GetShadowView(oldLibrary, oldLibrary.CollectionType, string.Empty).Id;
        var newShadowId = libraryManager.GetShadowViewId(Name.ToUpperInvariant(), newId, oldLibrary.CollectionType);

        var user = await userManager.CreateUserAsync("rename-case-user");
        user.SetPreference(PreferenceKind.EnabledFolders, [oldId]);
        user.SetPreference(PreferenceKind.MyMediaExcludes, [oldShadowId]);
        await userManager.UpdateUserAsync(user);

        using var renameResponse = await client.PostAsync(
            $"Library/VirtualFolders/Name?name={Name}&newName={Name.ToUpperInvariant()}&refreshLibrary=false",
            null,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, renameResponse.StatusCode);

        Assert.Equal([newId], userManager.GetUserById(user.Id)!.GetPreferenceValues<Guid>(PreferenceKind.EnabledFolders));
        Assert.Equal([newShadowId], userManager.GetUserById(user.Id)!.GetPreferenceValues<Guid>(PreferenceKind.MyMediaExcludes));

        using var cleanup = await client.DeleteAsync($"Library/VirtualFolders?name={Name.ToUpperInvariant()}&refreshLibrary=false", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, cleanup.StatusCode);
    }
}
