#pragma warning disable CA1019 // Define accessors for attribute arguments

using System;
using System.Globalization;
using Jellyfin.Server.Migrations.Stages;

namespace Jellyfin.Server.Migrations;

/// <summary>
/// Declares an class as an migration with its set metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class JellyfinMigrationAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMigrationAttribute"/> class.
    /// </summary>
    /// <param name="order">The ordering this migration should be applied to. Must be a valid DateTime ISO8601 formatted string.</param>
    /// <param name="name">The name of this Migration.</param>
#pragma warning disable CS0618 // Type or member is obsolete
    public JellyfinMigrationAttribute(string order, string name) : this(order, name, null)
#pragma warning restore CS0618 // Type or member is obsolete
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMigrationAttribute"/> class for legacy migrations.
    /// </summary>
    /// <param name="order">The ordering this migration should be applied to. Must be a valid DateTime ISO8601 formatted string.</param>
    /// <param name="name">The name of this Migration.</param>
    /// <param name="key">[ONLY FOR LEGACY MIGRATIONS]The unique key of this migration. Must be a valid Guid formatted string.</param>
    [Obsolete("This Constructor should only be used for Legacy migrations. Use the (Order,Name) one for all new ones instead.")]
    public JellyfinMigrationAttribute(string order, string name, string? key)
    {
        Order = DateTime.Parse(order, CultureInfo.InvariantCulture);
        Name = name;
        Stage = JellyfinMigrationStageTypes.AppInitialisation;
        if (key is not null)
        {
            Key = Guid.Parse(key);
        }
    }

    /// <summary>
    /// Gets or Sets a value indicating whether the annoated migration should be executed on a fresh install.
    /// </summary>
    public bool RunMigrationOnSetup { get; set; }

    /// <summary>
    /// Gets or Sets the stage the annoated migration should be executed at. Defaults to <see cref="JellyfinMigrationStageTypes.CoreInitialisation"/>.
    /// </summary>
    public JellyfinMigrationStageTypes Stage { get; set; } = JellyfinMigrationStageTypes.CoreInitialisation;

    /// <summary>
    /// Gets the ordering of the migration.
    /// </summary>
    public DateTime Order { get; }

    /// <summary>
    /// Gets the name of the migration.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the Legacy Key of the migration. Not required for new Migrations.
    /// </summary>
    public Guid? Key { get; }
}
