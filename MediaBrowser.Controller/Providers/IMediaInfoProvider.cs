namespace MediaBrowser.Controller.Providers;

/// <summary>
/// A provider that supports technical discovery without modifying descriptive metadata
/// when <see cref="MetadataRefreshOptions.RefreshMediaInfoOnly"/> is enabled.
/// </summary>
public interface IMediaInfoProvider : ICustomMetadataProvider
{
}
