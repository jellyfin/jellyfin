namespace Jellyfin.Database.Implementations.ModelConfiguration;

/// <summary>
/// The maximum lengths of the strings that are part of a key or an index.
/// </summary>
/// <remarks>
/// Each length is used by the model configuration and by the code that has to make a value fit its column, so that
/// the two cannot drift apart.
/// </remarks>
internal static class KeyLengths
{
    internal const int UserDataKey = 256;

    internal const int PersonRole = 400;

    internal const int ProviderId = 128;

    internal const int ItemValue = 512;

    internal const int DisplayPreferencesKey = 256;

    internal const int AccessToken = 128;

    internal const int DeviceId = 256;

    internal const int ItemType = 256;

    internal const int MediaType = 64;

    internal const int Username = 255;

    internal const int DisplayPreferencesClient = 32;
}
