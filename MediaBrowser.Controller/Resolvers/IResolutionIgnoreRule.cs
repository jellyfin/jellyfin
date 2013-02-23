using MediaBrowser.Controller.Library;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// Provides a base "rule" that anyone can use to have paths ignored by the resolver
    /// </summary>
    public interface IResolutionIgnoreRule
    {
        bool ShouldIgnore(ItemResolveArgs args);
    }
}
