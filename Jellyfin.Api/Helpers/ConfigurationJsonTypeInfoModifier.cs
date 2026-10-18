using System.Text.Json.Serialization.Metadata;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Keeps config defaults out of the DTOs.
/// </summary>
public static class ConfigurationJsonTypeInfoModifier
{
    /// <summary>
    /// Fills in defaults from the internal config before reading the request.
    /// </summary>
    /// <remarks>
    /// Missing fields keep their defaults. Values sent by the client, including null, replace them.
    /// Use a new config each time so requests don't share arrays or other objects.
    /// </remarks>
    /// <param name="typeInfo">The JSON type information.</param>
    public static void Modify(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Type == typeof(ServerConfigurationDto))
        {
            typeInfo.CreateObject = static () => ConfigurationMapper.MapToDto(new ServerConfiguration());
        }
        else if (typeInfo.Type == typeof(EncodingOptionsDto))
        {
            typeInfo.CreateObject = static () => ConfigurationMapper.MapToDto(new EncodingOptions());
        }
        else if (typeInfo.Type == typeof(NetworkConfigurationDto))
        {
            typeInfo.CreateObject = static () => ConfigurationMapper.MapToDto(new NetworkConfiguration());
        }
        else if (typeInfo.Type == typeof(MetadataConfigurationDto))
        {
            typeInfo.CreateObject = static () => ConfigurationMapper.MapToDto(new MetadataConfiguration());
        }
        else if (typeInfo.Type == typeof(XbmcMetadataOptionsDto))
        {
            typeInfo.CreateObject = static () => ConfigurationMapper.MapToDto(new XbmcMetadataOptions());
        }
        else if (typeInfo.Type == typeof(LiveTvOptionsDto))
        {
            typeInfo.CreateObject = static () => ConfigurationMapper.MapToDto(new LiveTvOptions());
        }
    }
}
