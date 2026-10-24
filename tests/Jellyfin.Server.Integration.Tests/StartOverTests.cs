using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Api.Models.StartupDtos;
using Jellyfin.Database.Implementations;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jellyfin.Server.Integration.Tests;

/// <summary>
/// Covers starting a server that has been set up before after its database has been deleted.
/// </summary>
public sealed class StartOverTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "jellyfin-test-data", "start-over-" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

    [Fact]
    public async Task DeletedDatabase_WizardReset_StartsAsNewServer()
    {
        var paths = await SetUpServerAndDeleteDatabaseAsync();
        SetWizardCompleted(paths, false);

        using var factory = new SameRootApplicationFactory(_root);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/Startup/Configuration", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var dbContextFactory = factory.Services.GetRequiredService<IDbContextFactory<JellyfinDbContext>>();
        await using var context = await dbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var applied = (await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken)).ToHashSet(StringComparer.Ordinal);
        Assert.All(context.GetService<IMigrationsAssembly>().Migrations.Keys, id => Assert.Contains(id, applied));
    }

    [Fact]
    public async Task DeletedDatabase_WizardStillCompleted_RefusesToStart()
    {
        var paths = await SetUpServerAndDeleteDatabaseAsync();
        var systemConfiguration = await File.ReadAllBytesAsync(paths.SystemConfigurationFilePath, TestContext.Current.CancellationToken);

        using var factory = new SameRootApplicationFactory(_root);
        var exception = Assert.ThrowsAny<Exception>(() => factory.CreateClient());

        var guard = exception as InvalidOperationException ?? exception.InnerException as InvalidOperationException;
        Assert.NotNull(guard);
        Assert.Contains("the database does not exist", guard.Message, StringComparison.Ordinal);
        Assert.Empty(Directory.GetFiles(paths.DataPath, "jellyfin.db*"));
        Assert.Equal(systemConfiguration, await File.ReadAllBytesAsync(paths.SystemConfigurationFilePath, TestContext.Current.CancellationToken));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
            // Best effort, a locked file must not fail the test.
        }
    }

    private static void SetWizardCompleted(IApplicationPaths paths, bool completed)
    {
        var serializer = new MyXmlSerializer();
        var configuration = (ServerConfiguration)serializer.DeserializeFromFile(typeof(ServerConfiguration), paths.SystemConfigurationFilePath)!;
        configuration.IsStartupWizardCompleted = completed;
        serializer.SerializeToFile(configuration, paths.SystemConfigurationFilePath);
    }

    private async Task<IApplicationPaths> SetUpServerAndDeleteDatabaseAsync()
    {
        IApplicationPaths paths;
        using (var factory = new SameRootApplicationFactory(_root))
        {
            using var client = factory.CreateClient();
            paths = factory.Services.GetRequiredService<IApplicationPaths>();

            using (var response = await client.GetAsync("/Startup/User", TestContext.Current.CancellationToken))
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            var user = new StartupUserDto { Name = "StartOver", Password = "StartOver" };
            using (var response = await client.PostAsJsonAsync("/Startup/User", user, _jsonOptions, TestContext.Current.CancellationToken))
            {
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
        }

        // Completing the wizard through the API would queue a library scan that can still hold the database file when
        // it is deleted below, so the flag is set on the stopped server instead.
        SetWizardCompleted(paths, true);
        SqliteConnection.ClearAllPools();
        foreach (var file in Directory.GetFiles(paths.DataPath, "jellyfin.db*"))
        {
            File.Delete(file);
        }

        return paths;
    }

    private sealed class SameRootApplicationFactory(string webHostPathRoot) : JellyfinApplicationFactory(webHostPathRoot);
}
