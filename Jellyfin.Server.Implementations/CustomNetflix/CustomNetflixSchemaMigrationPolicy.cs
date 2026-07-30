#pragma warning disable CS1591

using System.Collections.Generic;
using System.Linq;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixSchemaMigrationPolicy
{
    public const int BaseSchemaVersion = 1;
    public const string BaseSchemaMigrationName = "base_customnetflix_schema";
    public const int MyListSchemaVersion = 2;
    public const string MyListSchemaMigrationName = "profile_my_list";
    public const int ActiveProfilesByTokenSchemaVersion = 3;
    public const string ActiveProfilesByTokenSchemaMigrationName = "active_profiles_by_token";
    public const int DisableUnsafeChildProfilesSchemaVersion = 4;
    public const string DisableUnsafeChildProfilesSchemaMigrationName = "disable_unsafe_child_profiles";
    public const int PlaybackProfilePreferencesSchemaVersion = 5;
    public const string PlaybackProfilePreferencesSchemaMigrationName = "playback_profile_preferences";
    public const int ProfileItemFeedbackSchemaVersion = 6;
    public const string ProfileItemFeedbackSchemaMigrationName = "profile_item_feedback";
    public const int CurrentSchemaVersion = ProfileItemFeedbackSchemaVersion;

    public static IReadOnlyList<int> KnownVersions { get; } =
    [
        BaseSchemaVersion,
        MyListSchemaVersion,
        ActiveProfilesByTokenSchemaVersion,
        DisableUnsafeChildProfilesSchemaVersion,
        PlaybackProfilePreferencesSchemaVersion,
        ProfileItemFeedbackSchemaVersion
    ];

    public static IReadOnlyList<int> GetPendingVersions(IReadOnlySet<int> appliedVersions, IReadOnlyList<int> knownVersions)
        => knownVersions
            .Where(version => !appliedVersions.Contains(version))
            .OrderBy(version => version)
            .ToArray();

    public static bool IsCurrent(int appliedVersion)
        => appliedVersion >= CurrentSchemaVersion;
}
