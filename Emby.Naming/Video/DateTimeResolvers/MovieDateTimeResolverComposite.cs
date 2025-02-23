using Emby.Naming.Common;

namespace Emby.Naming.Video.DateTimeResolvers;

/// <summary>
/// A composite that holds all resolvers and calls them.
/// It is recommended to always use the composite instead of calling individual resolvers.
/// </summary>
public class MovieDateTimeResolverComposite : IMovieDateTimeResolver
{
    private static readonly IMovieDateTimeResolver[] _components =
    [
        // The Resolver listed on the highest position will match if multiple could match so the order is important
        new TimeExplicitlyInBracketsMovieDateTimeResolver(),
        new SelfShotMoviesMovieDateTimeResolver(),
        new LatestPlausibleDateMovieDateTimeResolver(),
    ];

    /// <summary>
    /// Attempts to resolve date and Name from the provided fileName.
    /// </summary>
    /// <param name="fileName">Name of video.</param>
    /// <param name="namingOptions">NamingOptions.</param>
    /// <returns>null if could not resolve.</returns>
    public CleanDateTimeResult? Resolve(string fileName, NamingOptions namingOptions)
    {
        foreach (var component in _components)
        {
            var result = component.Resolve(fileName, namingOptions);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
