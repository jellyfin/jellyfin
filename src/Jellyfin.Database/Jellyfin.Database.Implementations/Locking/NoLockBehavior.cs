using System;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Database.Implementations.Locking;

/// <summary>
/// Default lock behavior. Defines no explicit application locking behavior.
/// </summary>
public class NoLockBehavior : IEntityFrameworkCoreLockingBehavior
{
    /// <inheritdoc/>
    public void OnSaveChanges(JellyfinDbContext context, Action saveChanges)
    {
        saveChanges();
    }

    /// <inheritdoc/>
    public void Initialise(DbContextOptionsBuilder optionsBuilder)
    {
    }
}
