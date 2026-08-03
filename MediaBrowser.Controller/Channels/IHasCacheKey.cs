namespace MediaBrowser.Controller.Channels
{
    /// <summary>
    /// Interface for channels that provide a cache key.
    /// </summary>
    public interface IHasCacheKey
    {
        /// <summary>
        /// Gets the cache key.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The cache key.</returns>
        string? GetCacheKey(string? userId);
    }
}
