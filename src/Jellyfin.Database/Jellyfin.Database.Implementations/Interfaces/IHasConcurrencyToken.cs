namespace Jellyfin.Data.Interfaces
{
    /// <summary>
    /// An interface abstracting an entity that has a concurrency token.
    /// </summary>
    public interface IHasConcurrencyToken
    {
        /// <summary>
        /// Gets the version of this row. Acts as a concurrency token.
        /// </summary>
        uint RowVersion { get; }

        /// <summary>
        /// Called when saving changes to this entity.
        /// </summary>
        void OnSavingChanges();
    }
}
