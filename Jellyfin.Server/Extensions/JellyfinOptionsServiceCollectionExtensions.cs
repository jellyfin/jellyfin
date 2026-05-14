using System.IO;
using Jellyfin.Database.Implementations.DbConfiguration;
using Jellyfin.Server.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Branding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jellyfin.Server.Extensions;

/// <summary>
/// Extension methods for registering the JSON-backed <see cref="IWritableOptions{T}"/>
/// services for the four core Jellyfin configuration types.
/// </summary>
public static class JellyfinOptionsServiceCollectionExtensions
{
    private static IServiceCollection AddWritableConfigEntry<TModel>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName,
        string filePath)
        where TModel : class, new()
    {
        return services.Configure<TModel>(
                configuration.GetSection(sectionName))
                .AddSingleton<IWritableOptions<TModel>>(sp =>
            new WritableOptions<TModel>(
                sp.GetRequiredService<IOptionsMonitor<TModel>>(),
                (IConfigurationRoot)sp.GetRequiredService<IConfiguration>(),
                sectionName,
                filePath));
    }

    /// <summary>
    /// Registers <see cref="IOptions{TOptions}"/>, <see cref="IOptionsMonitor{TOptions}"/> and
    /// <see cref="IWritableOptions{T}"/> for the four core configuration types
    /// (<see cref="ServerConfiguration"/>, <see cref="EncodingOptions"/>,
    /// <see cref="NetworkConfiguration"/>, <see cref="BrandingOptions"/>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The root <see cref="IConfiguration"/> used for section binding.</param>
    /// <param name="configDir">The server configuration directory where JSON files are stored.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddJellyfinOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        string configDir)
    {
        // Bind the four core config sections from the JSON files.
        return services
            .AddWritableConfigEntry<ServerConfiguration>(
                configuration,
                JellyfinConfigurationConstants.ServerConfigurationKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.SystemJsonFile))
            .AddWritableConfigEntry<EncodingOptions>(
                configuration,
                JellyfinConfigurationConstants.EncodingOptionsKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.EncodingJsonFile))
            .AddWritableConfigEntry<NetworkConfiguration>(
                configuration,
                JellyfinConfigurationConstants.NetworkConfigurationKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.NetworkJsonFile))
            .AddWritableConfigEntry<BrandingOptions>(
                configuration,
                JellyfinConfigurationConstants.BrandingOptionsKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.BrandingJsonFile))
            // Database configuration options.
            .AddWritableConfigEntry<DatabaseConfigurationOptions>(
                configuration,
                JellyfinConfigurationConstants.DatabaseConfigurationKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.DatabaseJsonFile))
            // Live TV and NFO metadata configuration options.
            .AddWritableConfigEntry<LiveTvOptions>(
                configuration,
                JellyfinConfigurationConstants.LiveTvOptionsKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.LiveTvJsonFile))
            .AddWritableConfigEntry<XbmcMetadataOptions>(
                configuration,
                JellyfinConfigurationConstants.XbmcMetadataOptionsKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.XbmcMetadataJsonFile));
    }
}
