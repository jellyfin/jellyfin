using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jellyfin.Server.Implementations.Tests.EfMigrations;

/// <summary>
/// The differences between migrations that share an id which were reviewed and accepted, one entry for each, with the reason.
/// </summary>
internal static class MigrationContentsAllowlist
{
    /// <summary>
    /// The path of the allowlist in the repository.
    /// </summary>
    public const string RepositoryPath = "tests/Jellyfin.Server.Implementations.Tests/EfMigrations/MigrationContentsAllowlist.json";

    private const string MigrationNamePlaceholder = "<Name>";

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter<MigrationContentsCategory>(allowIntegerValues: false) }
    };

    /// <summary>
    /// Reads the allowlist that the build copies next to the tests.
    /// </summary>
    /// <returns>The entries.</returns>
    /// <exception cref="InvalidDataException">The allowlist is not a list of valid entries.</exception>
    public static IReadOnlyList<MigrationContentsAllowlistEntry> Load()
    {
        return Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "EfMigrations", "MigrationContentsAllowlist.json")));
    }

    /// <summary>
    /// Reads an allowlist.
    /// </summary>
    /// <param name="json">The allowlist.</param>
    /// <returns>The entries.</returns>
    /// <exception cref="InvalidDataException">The allowlist is not a list of valid entries, for example because an entry has
    /// an unknown category or property, or no reason, or still has the placeholders of a proposed entry.</exception>
    public static IReadOnlyList<MigrationContentsAllowlistEntry> Parse(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<MigrationContentsAllowlistEntry[]>(json, Options)
                ?? throw new JsonException("The allowlist is null instead of a list of entries.");
        }
        catch (JsonException exception)
        {
            var lines = Header();
            lines.Add(string.Empty);
            lines.Add("The allowlist cannot be read: " + exception.Message);
            lines.Add(
                "  Every entry needs a migration, a direction, a provider and an operation, exactly as the check prints them, one of the categories "
                + string.Join(", ", Enum.GetNames<MigrationContentsCategory>())
                + ", and a reason. Replace the placeholders of a proposed entry before adding it.");
            throw new InvalidDataException(string.Join(Environment.NewLine, lines), exception);
        }
    }

    /// <summary>
    /// Matches the differences with the entries. Every difference needs exactly one entry, and every entry exactly one difference.
    /// </summary>
    /// <param name="differences">The differences between the migrations.</param>
    /// <param name="entries">The entries of the allowlist.</param>
    /// <param name="providers">The names of the providers whose migrations are compared. The first one is the reference provider.</param>
    /// <returns>A description of every problem, or <c>null</c> when there is none.</returns>
    public static string? Check(IReadOnlyList<MigrationDifference> differences, IReadOnlyList<MigrationContentsAllowlistEntry> entries, IReadOnlyList<string> providers)
    {
        var invalid = entries.Select(e => (Entry: e, Problem: Validate(e, providers))).Where(e => e.Problem is not null).ToList();
        var byKey = entries.ToLookup(e => e.Key);
        var keys = differences.Select(e => e.Key).ToHashSet();
        var ambiguous = byKey.Where(e => e.Count() > 1).ToList();
        var missing = differences.Where(e => !byKey.Contains(e.Key)).ToList();
        var stale = entries.Where(e => !keys.Contains(e.Key)).ToList();
        if (invalid.Count == 0 && ambiguous.Count == 0 && missing.Count == 0 && stale.Count == 0)
        {
            return null;
        }

        var lines = Header();

        if (missing.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add($"Missing: {missing.Count} difference(s) without an entry.");
            lines.Add(
                "  Make the same change in every provider's migration that has the id, or take the hand edit out. "
                + "A fix for a bug in current code has to reach every provider and cannot be listed here. "
                + "Only a fix for data that only old SQLite databases can hold, SQL that does the same as the other providers' operations, "
                + "or an engine-specific step may be listed, with its category and reason. The entries would be:");
            lines.AddRange(JsonSerializer.Serialize(missing.Select(Template), Options).Split('\n').Select(e => "  " + e.TrimEnd()));
        }

        if (stale.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add($"Stale: {stale.Count} entr(y/ies) that match no difference: the migration changed, or the entry is wrong.");
            lines.Add($"  Delete or correct the entry. If the migration was generated again with '{MigrationProvider.Command} {MigrationNamePlaceholder}', review what differs now; the check reports it as missing.");
            lines.AddRange(stale.Select(e => "  " + e.Describe()));
        }

        if (ambiguous.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add($"Ambiguous: {ambiguous.Count} difference(s) with more than one entry. Keep one entry for each difference.");
            lines.AddRange(ambiguous.Select(e => $"  {e.Count()} entries for {e.First().Describe()}"));
        }

        if (invalid.Count > 0)
        {
            lines.Add(string.Empty);
            lines.Add($"Invalid: {invalid.Count} entr(y/ies).");
            lines.AddRange(invalid.Select(e => $"  {e.Problem}: {e.Entry.Describe()}"));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static List<string> Header() =>
    [
        "The operations of the migrations that share an id differ from what " + RepositoryPath + " records.",
        $"Every provider's migration of a change is generated from the model by '{MigrationProvider.Command} {MigrationNamePlaceholder}', "
        + "so a difference comes from a hand edit or from a migration that the command did not generate."
    ];

    private static string? Validate(MigrationContentsAllowlistEntry entry, IReadOnlyList<string> providers)
    {
        if (entry.Direction is not (MigrationContents.Up or MigrationContents.Down))
        {
            return $"The direction is not {MigrationContents.Up} or {MigrationContents.Down}";
        }

        var owners = entry.Provider.Split(", ");
        if (owners.Any(e => !providers.Contains(e)))
        {
            return "The provider is not one of " + string.Join(", ", providers);
        }

        // Reachable for entries that are not read from the file, whose converter already rejects an unknown name.
        if (!Enum.IsDefined(entry.Category))
        {
            return "The category is not known";
        }

        // Only the reference provider has migrations from before the other providers existed.
        if (entry.Category == MigrationContentsCategory.LegacySqliteData && !(owners.Length == 1 && owners[0] == providers[0]))
        {
            return $"Only the reference provider {providers[0]} can have a {nameof(MigrationContentsCategory.LegacySqliteData)} difference";
        }

        return string.IsNullOrWhiteSpace(entry.Reason) ? "The reason is missing" : null;
    }

    /// <summary>
    /// Proposes an entry for a difference. Its category and reason are placeholders that do not read as an entry,
    /// so that an entry is not added without them.
    /// </summary>
    private static Dictionary<string, string> Template(MigrationDifference difference) => new()
    {
        ["migration"] = difference.MigrationId,
        ["direction"] = difference.Direction,
        ["provider"] = difference.Provider,
        ["operation"] = difference.Operation,
        ["category"] = "<" + string.Join(" or ", Enum.GetNames<MigrationContentsCategory>()) + ">",
        ["reason"] = "<why this difference is correct>"
    };
}
