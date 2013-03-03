namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Provides a base "rule" that anyone can use to have paths ignored by the resolver
    /// </summary>
    public interface IResolverIgnoreRule
    {
        bool ShouldIgnore(ItemResolveArgs args);
    }
}
