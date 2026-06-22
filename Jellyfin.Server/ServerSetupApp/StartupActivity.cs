using System.Globalization;

namespace Jellyfin.Server.ServerSetupApp;

/// <summary>
/// A curated vocabulary of generic, non-identifying descriptions of what the server is doing during startup.
/// These are shown in the always-visible header of the startup UI to <b>unauthenticated</b> clients, so every
/// value must stay generic and must never contain server specific details (paths, names, plugin or migration ids, counts of items, etc.).
/// </summary>
public static class StartupActivity
{
    /// <summary>The default state before any work has been reported.</summary>
    public const string Starting = "Starting up";

    /// <summary>Validating that the configured storage locations are usable.</summary>
    public const string CheckingStorage = "Checking storage";

    /// <summary>Bringing up the migration subsystem and running early startup checks.</summary>
    public const string Initializing = "Initializing server";

    /// <summary>Preparing the system for migrations (e.g. taking safety backups).</summary>
    public const string PreparingMigrations = "Preparing migrations";

    /// <summary>Applying database/system migrations without a known count.</summary>
    public const string ApplyingMigrations = "Applying migrations";

    /// <summary>Restoring from a backup.</summary>
    public const string RestoringBackup = "Restoring backup";

    /// <summary>Bringing up core services and plugins.</summary>
    public const string InitializingServices = "Initializing services";

    /// <summary>Running the final startup tasks.</summary>
    public const string FinishingStartup = "Finishing startup";

    /// <summary>
    /// Builds a generic "Running migration X of Y" description. Only the numeric position and total are exposed.
    /// </summary>
    /// <param name="current">The 1-based index of the migration currently running.</param>
    /// <param name="total">The total number of migrations in this batch.</param>
    /// <returns>A generic progress description.</returns>
    public static string Migration(int current, int total)
        => string.Format(CultureInfo.InvariantCulture, "Running migration {0} of {1}", current, total);
}
