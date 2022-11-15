namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Marker interface to denote a class that supports being hidden underneath it's boxset.
    /// Just about anything can be placed into a boxset,
    /// but movies should also only appear underneath and not outside separately (subject to configuration).
    /// </summary>
    public interface ISupportsBoxSetGrouping
    {
    }
}
