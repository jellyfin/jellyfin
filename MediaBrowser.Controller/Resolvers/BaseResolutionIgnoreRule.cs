using MediaBrowser.Controller.Library;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// Provides a base "rule" that anyone can use to have paths ignored by the resolver
    /// </summary>
    public abstract class BaseResolutionIgnoreRule
    {
        public abstract bool ShouldIgnore(ItemResolveArgs args);
    }
}
