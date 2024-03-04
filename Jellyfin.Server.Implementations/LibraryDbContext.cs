using System;
using System.Linq;
using System.Reflection;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Entities.Libraries;
using Jellyfin.Data.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Server.Implementations;

/// <inheritdoc/>
public class LibraryDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryDbContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the user and BaseItem Pivot Table.
    /// </summary>
    public DbSet<UserItemData> UserDatas => Set<UserItemData>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the Chapters info.
    /// </summary>
    public DbSet<ChapterInfo> ChapterInfos => Set<ChapterInfo>();

    /// <summary>
    /// Gets the <see cref="DbSet{TEntity}"/> containing the Genres info.
    /// </summary>
    public DbSet<Genre> Genres => Set<Genre>();

    /// <inheritdoc/>
    public override int SaveChanges()
    {
        foreach (var saveEntity in ChangeTracker.Entries()
                     .Where(e => e.State == EntityState.Modified)
                     .Select(entry => entry.Entity)
                     .OfType<IHasConcurrencyToken>())
        {
            saveEntity.OnSavingChanges();
        }

        return base.SaveChanges();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.SetDefaultDateTimeKind(DateTimeKind.Utc);
        base.OnModelCreating(modelBuilder);

        // Configuration for each entity is in it's own class inside 'ModelConfiguration'.
        // modelBuilder.ApplyConfigurationsForEntitiesInContext();
        modelBuilder.ApplyConfigurationsFromAssembly(
            Assembly.GetExecutingAssembly(),
            t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>) &&
                typeof(ILibraryModel).IsAssignableFrom(i.GenericTypeArguments[0])));
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDbContext).Assembly);
    }
}
