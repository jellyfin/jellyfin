using System;
using System.IO;
using Emby.Server.Implementations;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Jellyfin.Server.Tests;

public class ProgramConfigurationTests
{
    [Fact]
    public void CreateAppConfiguration_LoadsValuesFromAppsettingsJson()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), "jellyfin-program-config-tests", Guid.NewGuid().ToString("N"));
        var configPath = Path.Combine(rootPath, "config");
        var logsPath = Path.Combine(rootPath, "logs");
        var cachePath = Path.Combine(rootPath, "cache");
        var webPath = Path.Combine(rootPath, "web");

        Directory.CreateDirectory(configPath);
        Directory.CreateDirectory(logsPath);
        Directory.CreateDirectory(cachePath);
        Directory.CreateDirectory(webPath);

        try
        {
            var appsettingsPath = Path.Combine(configPath, Program.JellyfinConfigFileDefault);
            var loggingConfigPath = Path.Combine(configPath, Program.LoggingConfigFileDefault);
            File.WriteAllText(appsettingsPath, "{ \"PublishedServerUrl\": \"http://example.test:1234\" }");
            File.WriteAllText(loggingConfigPath, "{}");

            var appPaths = new ServerApplicationPaths(rootPath, logsPath, configPath, cachePath, webPath);
            IConfiguration config = Program.CreateAppConfiguration(new StartupOptions(), appPaths);

            Assert.Equal("http://example.test:1234", config["PublishedServerUrl"]);
        }
        finally
        {
            if (Directory.Exists(rootPath))
            {
                Directory.Delete(rootPath, recursive: true);
            }
        }
    }
}
