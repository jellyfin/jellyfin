#pragma warning disable CS1591

namespace MediaBrowser.Controller.Channels
{
    public interface IHasCacheKey
    {
        /// <summary>
        /// Gets the cache key.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>System.String.</returns>
        string? GetCacheKey(string? userId);
    }
}
