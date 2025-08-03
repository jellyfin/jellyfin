using System;
using System.ComponentModel;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Database.Implementations.Interfaces;

namespace Jellyfin.Data;

/// <summary>
/// Contains extension methods for manipulation of <see cref="User"/> entities.
/// </summary>
public static class UserEntityExtensions
{
    /// <summary>
    /// The values being delimited here are Guids, so commas work as they do not appear in Guids.
    /// </summary>
    private const char Delimiter = ',';

    /// <summary>
    /// Checks whether the user has the specified permission.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="kind">The permission kind.</param>
    /// <returns><c>True</c> if the user has the specified permission.</returns>
    public static bool HasPermission(this IHasPermissions entity, PermissionKind kind)
    {
        return entity.Permissions.FirstOrDefault(p => p.Kind == kind)?.Value ?? false;
    }

    /// <summary>
    /// Sets the given permission kind to the provided value.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="kind">The permission kind.</param>
    /// <param name="value">The value to set.</param>
    public static void SetPermission(this IHasPermissions entity, PermissionKind kind, bool value)
    {
        var currentPermission = entity.Permissions.FirstOrDefault(p => p.Kind == kind);
        if (currentPermission is null)
        {
            entity.Permissions.Add(new Permission(kind, value));
        }
        else
        {
            currentPermission.Value = value;
        }
    }

    /// <summary>
    /// Gets the user's preferences for the given preference kind.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="preference">The preference kind.</param>
    /// <returns>A string array containing the user's preferences.</returns>
    public static string[] GetPreference(this User entity, PreferenceKind preference)
    {
        var val = entity.Preferences.FirstOrDefault(p => p.Kind == preference)?.Value;

        return string.IsNullOrEmpty(val) ? Array.Empty<string>() : val.Split(Delimiter);
    }

    /// <summary>
    /// Gets the user's preferences for the given preference kind.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="preference">The preference kind.</param>
    /// <typeparam name="T">Type of preference.</typeparam>
    /// <returns>A {T} array containing the user's preference.</returns>
    public static T[] GetPreferenceValues<T>(this User entity, PreferenceKind preference)
    {
        var val = entity.Preferences.FirstOrDefault(p => p.Kind == preference)?.Value;
        if (string.IsNullOrEmpty(val))
        {
            return Array.Empty<T>();
        }

        // Convert array of {string} to array of {T}
        var converter = TypeDescriptor.GetConverter(typeof(T));
        var stringValues = val.Split(Delimiter);
        var convertedCount = 0;
        var parsedValues = new T[stringValues.Length];
        for (var i = 0; i < stringValues.Length; i++)
        {
            try
            {
                var parsedValue = converter.ConvertFromString(stringValues[i].Trim());
                if (parsedValue is not null)
                {
                    parsedValues[convertedCount++] = (T)parsedValue;
                }
            }
            catch (FormatException)
            {
                // Unable to convert value
            }
        }

        return parsedValues[..convertedCount];
    }

    /// <summary>
    /// Sets the specified preference to the given value.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="preference">The preference kind.</param>
    /// <param name="values">The values.</param>
    public static void SetPreference(this User entity, PreferenceKind preference, string[] values)
    {
        var value = string.Join(Delimiter, values);
        var currentPreference = entity.Preferences.FirstOrDefault(p => p.Kind == preference);
        if (currentPreference is null)
        {
            entity.Preferences.Add(new Preference(preference, value));
        }
        else
        {
            currentPreference.Value = value;
        }
    }

    /// <summary>
    /// Sets the specified preference to the given value.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="preference">The preference kind.</param>
    /// <param name="values">The values.</param>
    /// <typeparam name="T">The type of value.</typeparam>
    public static void SetPreference<T>(this User entity, PreferenceKind preference, T[] values)
    {
        var value = string.Join(Delimiter, values);
        var currentPreference = entity.Preferences.FirstOrDefault(p => p.Kind == preference);
        if (currentPreference is null)
        {
            entity.Preferences.Add(new Preference(preference, value));
        }
        else
        {
            currentPreference.Value = value;
        }
    }

    /// <summary>
    /// Checks whether this user is currently allowed to use the server.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns><c>True</c> if the current time is within an access schedule, or there are no access schedules.</returns>
    public static bool IsParentalScheduleAllowed(this User entity)
    {
        return entity.AccessSchedules.Count == 0
               || entity.AccessSchedules.Any(i => IsParentalScheduleAllowed(i, DateTime.UtcNow));
    }

    /// <summary>
    /// Checks whether the provided folder is in this user's grouped folders.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="id">The Guid of the folder.</param>
    /// <returns><c>True</c> if the folder is in the user's grouped folders.</returns>
    public static bool IsFolderGrouped(this User entity, Guid id)
    {
        return Array.IndexOf(GetPreferenceValues<Guid>(entity, PreferenceKind.GroupedFolders), id) != -1;
    }

    /// <summary>
    /// Initializes the default permissions for a user. Should only be called on user creation.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    // TODO: make these user configurable?
    public static void AddDefaultPermissions(this User entity)
    {
        entity.Permissions.Add(new Permission(PermissionKind.IsAdministrator, false));
        entity.Permissions.Add(new Permission(PermissionKind.IsDisabled, false));
        entity.Permissions.Add(new Permission(PermissionKind.IsHidden, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableAllChannels, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableAllDevices, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableAllFolders, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableContentDeletion, false));
        entity.Permissions.Add(new Permission(PermissionKind.EnableContentDownloading, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableMediaConversion, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableMediaPlayback, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnablePlaybackRemuxing, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnablePublicSharing, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableRemoteAccess, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableSyncTranscoding, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableAudioPlaybackTranscoding, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableLiveTvAccess, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableLiveTvManagement, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableSharedDeviceControl, true));
        entity.Permissions.Add(new Permission(PermissionKind.EnableVideoPlaybackTranscoding, true));
        entity.Permissions.Add(new Permission(PermissionKind.ForceRemoteSourceTranscoding, false));
        entity.Permissions.Add(new Permission(PermissionKind.EnableRemoteControlOfOtherUsers, false));
        entity.Permissions.Add(new Permission(PermissionKind.EnableCollectionManagement, false));
        entity.Permissions.Add(new Permission(PermissionKind.EnableSubtitleManagement, false));
        entity.Permissions.Add(new Permission(PermissionKind.EnableLyricManagement, false));
    }

    /// <summary>
    /// Initializes the default preferences. Should only be called on user creation.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    public static void AddDefaultPreferences(this User entity)
    {
        foreach (var val in Enum.GetValues<PreferenceKind>())
        {
            entity.Preferences.Add(new Preference(val, string.Empty));
        }
    }

    private static bool IsParentalScheduleAllowed(AccessSchedule schedule, DateTime date)
    {
        var localTime = date.ToLocalTime();
        var hour = localTime.TimeOfDay.TotalHours;
        var currentDayOfWeek = localTime.DayOfWeek;

        return schedule.DayOfWeek.Contains(currentDayOfWeek)
               && hour >= schedule.StartHour
               && hour <= schedule.EndHour;
    }
}
