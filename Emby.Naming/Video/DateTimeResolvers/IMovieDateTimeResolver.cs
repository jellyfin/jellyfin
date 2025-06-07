using Emby.Naming.Common;

namespace Emby.Naming.Video.DateTimeResolvers;

/// <summary>
/// Resolves date and Name for the provided fileName.
/// </summary>
public interface IMovieDateTimeResolver
{
    /// <summary>
    /// Attempts to resolve date and Name from the provided fileName.
    /// </summary>
    /// <param name="fileName">Name of video.</param>
    /// <param name="namingOptions">NamingOptions.</param>
    /// <returns>null if could not resolve. </returns>
    public CleanDateTimeResult? Resolve(string fileName, NamingOptions namingOptions);
}
