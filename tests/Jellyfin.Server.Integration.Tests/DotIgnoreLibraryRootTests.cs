using System;
using System.IO;
using Emby.Server.Implementations.Library;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jellyfin.Server.Integration.Tests;

public sealed class DotIgnoreLibraryRootTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;

    public DotIgnoreLibraryRootTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void IgnoreFile_IgnoreFileAboveLibraryRoot_IsNotConsulted()
    {
        _ = _factory.CreateClient();

        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var libraryRoot = Path.Combine(tempDir, "Movies");
        var movieDir = Path.Combine(libraryRoot, "Movie (2020)");
        Directory.CreateDirectory(movieDir);
        var libraryDir = Path.Combine(_factory.Services.GetRequiredService<IServerApplicationPaths>().DefaultUserViewsPath, "dotignore-root-test");
        Directory.CreateDirectory(libraryDir);

        try
        {
            File.WriteAllText(Path.Combine(tempDir, ".ignore"), string.Empty);
            File.WriteAllText(Path.Combine(libraryDir, "Movies.mblink"), libraryRoot);

            // The rule the resolver evaluates must be the singleton LibraryManager hands the roots to
            _factory.Services.GetRequiredService<DotIgnoreIgnoreRule>().ClearDirectoryCache();
            var fileInfo = new FileSystemMetadata
            {
                FullName = Path.Combine(movieDir, "Movie (2020).mkv"),
                IsDirectory = false
            };

            Assert.False(_factory.Services.GetRequiredService<ILibraryManager>().IgnoreFile(fileInfo, new Folder { Path = movieDir }));
        }
        finally
        {
            Directory.Delete(libraryDir, true);
            Directory.Delete(tempDir, true);
        }
    }
}
