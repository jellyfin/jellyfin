using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Database.Providers.MySql.Migrations;

/// <summary>
/// Generates the id of a new migration.
/// A migration that has the name of a migration in the reference directory takes the id of that migration,
/// so that the migration histories of the providers line up.
/// When the reference directory is missing or does not give a single answer, a new id is generated and a warning is logged,
/// because that id will not line up with the one of the reference provider.
/// A name that simply has no migration in the reference directory is not reported.
/// </summary>
/// <remarks>
/// This mirrors the generator of the PostgreSQL provider. It is a copy and not a shared file, because both provider assemblies
/// make their internals visible to the same test project, where a shared type would exist twice.
/// </remarks>
internal sealed class PairedMigrationsIdGenerator : IMigrationsIdGenerator
{
    private const string TimestampFormat = "yyyyMMddHHmmss";
    private const char Separator = '_';
    private const string MigrationFileExtension = ".cs";

    // The file names are matched case sensitively, so that the result does not depend on the case rules of the file system of the platform.
    private static readonly EnumerationOptions CaseSensitive = new() { MatchCasing = MatchCasing.CaseSensitive };

    private readonly string? _referenceDirectory;
    private readonly ILogger _logger;
    private readonly Lock _lock = new();
    private DateTime _lastTimestamp = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="PairedMigrationsIdGenerator"/> class.
    /// </summary>
    /// <param name="referenceDirectory">The directory with the migration files whose ids are reused, or <c>null</c> for none.</param>
    /// <param name="logger">The logger for the warnings. The design-time tools show its warnings to the person running them.</param>
    public PairedMigrationsIdGenerator(string? referenceDirectory, ILogger logger)
    {
        _referenceDirectory = referenceDirectory;
        _logger = logger;
    }

    /// <inheritdoc />
    public string GenerateId(string name)
    {
        return FindReferenceId(name) ?? GenerateFreshId(name);
    }

    /// <inheritdoc />
    public string GetName(string id)
    {
        return id[(TimestampFormat.Length + 1)..];
    }

    /// <inheritdoc />
    public bool IsValidId(string value)
    {
        return value.Length > TimestampFormat.Length + 1
            && value[TimestampFormat.Length] == Separator
            && IsTimestamp(value.AsSpan(0, TimestampFormat.Length));
    }

    private static bool IsTimestamp(ReadOnlySpan<char> value)
    {
        foreach (var character in value)
        {
            if (!char.IsAsciiDigit(character))
            {
                return false;
            }
        }

        return true;
    }

    private string? FindReferenceId(string name)
    {
        if (_referenceDirectory is null || !Directory.Exists(_referenceDirectory))
        {
            _logger.LogWarning(
                "The migrations of the SQLite provider were not found, so the migration '{Name}' gets a new id and will not share an id with the SQLite migration. "
                + "Create the migrations of a change from a checkout of the repository.",
                name);
            return null;
        }

        var suffix = Separator + name + MigrationFileExtension;
        string? id = null;
        foreach (var path in Directory.EnumerateFiles(_referenceDirectory, "*" + suffix, CaseSensitive))
        {
            var fileName = Path.GetFileName(path);
            if (fileName.Length != TimestampFormat.Length + suffix.Length
                || !IsTimestamp(fileName.AsSpan(0, TimestampFormat.Length)))
            {
                continue;
            }

            if (id is not null)
            {
                _logger.LogWarning(
                    "More than one migration named '{Name}' exists in '{Directory}', so the migration gets a new id and will not share an id with a SQLite migration.",
                    name,
                    _referenceDirectory);
                return null;
            }

            id = fileName[..^MigrationFileExtension.Length];
        }

        return id;
    }

    private string GenerateFreshId(string name)
    {
        var now = DateTime.UtcNow;
        var timestamp = new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
        lock (_lock)
        {
            if (timestamp <= _lastTimestamp)
            {
                timestamp = _lastTimestamp.AddSeconds(1);
            }

            _lastTimestamp = timestamp;
        }

        return timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture) + Separator + name;
    }
}
