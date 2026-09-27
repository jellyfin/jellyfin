namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Marker interface for custom metadata providers that run before the regular metadata refresh.
    /// </summary>
    public interface IPreRefreshProvider : ICustomMetadataProvider
    {
    }
}
