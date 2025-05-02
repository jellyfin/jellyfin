using System;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Database.Implementations.Locking;

/// <summary>
/// Defines a jellyfin locking behavior that can be configured.
/// </summary>
public interface IEntityFrameworkCoreLockingBehavior
{
    /// <summary>
    /// Provides access to the builder to setup any connection related locking behavior.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    void Initialise(DbContextOptionsBuilder optionsBuilder);

    /// <summary>
    /// Will be invoked when changes should be saved in the current locking behavior.
    /// </summary>
    /// <param name="context">The database context invoking the action.</param>
    /// <param name="saveChanges">Callback for performing the actual save changes.</param>
    void OnSaveChanges(JellyfinDbContext context, Action saveChanges);
}
