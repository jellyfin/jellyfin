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
        services.Configure<ServerConfiguration>(
            configuration.GetSection(JellyfinConfigurationConstants.ServerConfigurationKey));

        services.Configure<EncodingOptions>(
            configuration.GetSection(JellyfinConfigurationConstants.EncodingOptionsKey));

        services.Configure<NetworkConfiguration>(
            configuration.GetSection(JellyfinConfigurationConstants.NetworkConfigurationKey));

        services.Configure<BrandingOptions>(
            configuration.GetSection(JellyfinConfigurationConstants.BrandingOptionsKey));

        // Register the writable variants that allow runtime persistence.
        services.AddSingleton<IWritableOptions<ServerConfiguration>>(sp =>
            new WritableOptions<ServerConfiguration>(
                sp.GetRequiredService<IOptionsMonitor<ServerConfiguration>>(),
                (IConfigurationRoot)sp.GetRequiredService<IConfiguration>(),
                JellyfinConfigurationConstants.ServerConfigurationKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.SystemJsonFile)));

        services.AddSingleton<IWritableOptions<EncodingOptions>>(sp =>
            new WritableOptions<EncodingOptions>(
                sp.GetRequiredService<IOptionsMonitor<EncodingOptions>>(),
                (IConfigurationRoot)sp.GetRequiredService<IConfiguration>(),
                JellyfinConfigurationConstants.EncodingOptionsKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.EncodingJsonFile)));

        services.AddSingleton<IWritableOptions<NetworkConfiguration>>(sp =>
            new WritableOptions<NetworkConfiguration>(
                sp.GetRequiredService<IOptionsMonitor<NetworkConfiguration>>(),
                (IConfigurationRoot)sp.GetRequiredService<IConfiguration>(),
                JellyfinConfigurationConstants.NetworkConfigurationKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.NetworkJsonFile)));

        services.AddSingleton<IWritableOptions<BrandingOptions>>(sp =>
            new WritableOptions<BrandingOptions>(
                sp.GetRequiredService<IOptionsMonitor<BrandingOptions>>(),
                (IConfigurationRoot)sp.GetRequiredService<IConfiguration>(),
                JellyfinConfigurationConstants.BrandingOptionsKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.BrandingJsonFile)));

        // Database configuration options.
        services.Configure<DatabaseConfigurationOptions>(
            configuration.GetSection(JellyfinConfigurationConstants.DatabaseConfigurationKey));

        services.AddSingleton<IWritableOptions<DatabaseConfigurationOptions>>(sp =>
            new WritableOptions<DatabaseConfigurationOptions>(
                sp.GetRequiredService<IOptionsMonitor<DatabaseConfigurationOptions>>(),
                (IConfigurationRoot)sp.GetRequiredService<IConfiguration>(),
                JellyfinConfigurationConstants.DatabaseConfigurationKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.DatabaseJsonFile)));

        // Live TV and NFO metadata configuration options.
        services.Configure<LiveTvOptions>(
            configuration.GetSection(JellyfinConfigurationConstants.LiveTvOptionsKey));

        services.Configure<XbmcMetadataOptions>(
            configuration.GetSection(JellyfinConfigurationConstants.XbmcMetadataOptionsKey));

        services.AddSingleton<IWritableOptions<LiveTvOptions>>(sp =>
            new WritableOptions<LiveTvOptions>(
                sp.GetRequiredService<IOptionsMonitor<LiveTvOptions>>(),
                (IConfigurationRoot)sp.GetRequiredService<IConfiguration>(),
                JellyfinConfigurationConstants.LiveTvOptionsKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.LiveTvJsonFile)));

        services.AddSingleton<IWritableOptions<XbmcMetadataOptions>>(sp =>
            new WritableOptions<XbmcMetadataOptions>(
                sp.GetRequiredService<IOptionsMonitor<XbmcMetadataOptions>>(),
                (IConfigurationRoot)sp.GetRequiredService<IConfiguration>(),
                JellyfinConfigurationConstants.XbmcMetadataOptionsKey,
                Path.Combine(configDir, JellyfinConfigurationConstants.XbmcMetadataJsonFile)));

        return services;
    }
}
