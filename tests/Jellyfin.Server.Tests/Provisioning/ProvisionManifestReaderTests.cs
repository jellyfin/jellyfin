using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Server.Provisioning;
using MediaBrowser.Model.Entities;
using Xunit;

namespace Jellyfin.Server.Tests.Provisioning;

public sealed class ProvisionManifestReaderTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("provision-tests").FullName;

    public void Dispose()
    {
        Directory.Delete(_directory, true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ReadAsync_FullManifest_ParsesEveryMember()
    {
        var manifest = await ReadAsync(
            """
            {
              "ServerName": "provisioned",
              "UICulture": "en-GB",
              "MetadataCountryCode": "DK",
              "PreferredMetadataLanguage": "da",
              "EnableRemoteAccess": true,
              "Administrator": { "Name": "admin", "Password": "hunter2" },
              "Libraries": [
                { "Name": "Movies", "CollectionType": "movies", "Paths": ["/media/movies"] }
              ]
            }
            """);

        Assert.Equal("provisioned", manifest.ServerName);
        Assert.Equal("en-GB", manifest.UICulture);
        Assert.Equal("DK", manifest.MetadataCountryCode);
        Assert.Equal("da", manifest.PreferredMetadataLanguage);
        Assert.True(manifest.EnableRemoteAccess);
        Assert.Equal("admin", manifest.Administrator.Name);
        Assert.Equal("hunter2", manifest.Administrator.Password);

        var library = Assert.Single(manifest.Libraries);
        Assert.Equal("Movies", library.Name);
        Assert.Equal(CollectionTypeOptions.movies, library.CollectionType);
        Assert.Equal(["/media/movies"], library.Paths);
    }

    [Fact]
    public async Task ReadAsync_MinimalManifest_LeavesEverythingElseUnset()
    {
        var manifest = await ReadAsync(
            """
            { "Administrator": { "Name": "admin", "Password": "hunter2" } }
            """);

        Assert.Null(manifest.ServerName);
        Assert.Null(manifest.UICulture);
        Assert.Null(manifest.MetadataCountryCode);
        Assert.Null(manifest.PreferredMetadataLanguage);
        Assert.Null(manifest.EnableRemoteAccess);
        Assert.Empty(manifest.Libraries);
    }

    [Fact]
    public async Task ReadAsync_CamelCaseCommentsAndTrailingCommas_AreAccepted()
    {
        var manifest = await ReadAsync(
            """
            {
              // the account the operator logs in with
              "administrator": { "name": "admin", "password": "hunter2" },
              "serverName": "provisioned",
            }
            """);

        Assert.Equal("admin", manifest.Administrator.Name);
        Assert.Equal("provisioned", manifest.ServerName);
    }

    [Fact]
    public async Task ReadAsync_LibraryWithoutCollectionType_IsUntyped()
    {
        var manifest = await ReadAsync(
            """
            {
              "Administrator": { "Name": "admin", "Password": "hunter2" },
              "Libraries": [ { "Name": "Other", "Paths": ["/media/other"] } ]
            }
            """);

        Assert.Null(Assert.Single(manifest.Libraries).CollectionType);
    }

    [Fact]
    public async Task ReadAsync_MissingFile_Throws()
    {
        var path = Path.Combine(_directory, "absent.json");
        await Assert.ThrowsAsync<FileNotFoundException>(() => ProvisionManifestReader.ReadAsync(path, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("{}")] // no administrator at all
    [InlineData("""{ "Administrator": { "Name": "admin" } }""")] // no password
    [InlineData("""{ "Administrator": { "Password": "hunter2" } }""")] // no name
    [InlineData("not json")]
    public async Task ReadAsync_UnusableJson_ThrowsJsonException(string content)
    {
        await Assert.ThrowsAsync<JsonException>(() => ReadAsync(content));
    }

    [Theory]
    [InlineData("null", "empty")]
    [InlineData("""{ "Administrator": { "Name": " ", "Password": "hunter2" } }""", "Administrator.Name")]
    [InlineData("""{ "Administrator": { "Name": "admin", "Password": "" } }""", "Administrator.Password")]
    [InlineData("""{ "Administrator": { "Name": "a", "Password": "b" }, "Libraries": [ { "Name": " ", "Paths": ["/m"] } ] }""", "Libraries[0].Name")]
    [InlineData("""{ "Administrator": { "Name": "a", "Password": "b" }, "Libraries": [ { "Name": "Movies", "Paths": [] } ] }""", "at least one path")]
    [InlineData("""{ "Administrator": { "Name": "a", "Password": "b" }, "Libraries": [ { "Name": "Movies", "Paths": [" "] } ] }""", "empty path")]
    public async Task ReadAsync_UnusableManifest_ThrowsWithActionableMessage(string content, string expectedInMessage)
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAsync(content));
        Assert.Contains(expectedInMessage, exception.Message, StringComparison.Ordinal);
    }

    private async Task<ProvisionManifest> ReadAsync(string content)
    {
        var path = Path.Combine(_directory, "provision.json");
        await File.WriteAllTextAsync(path, content, TestContext.Current.CancellationToken);
        return await ProvisionManifestReader.ReadAsync(path, TestContext.Current.CancellationToken);
    }
}
