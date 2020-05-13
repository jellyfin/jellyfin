#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using Emby.Server.Implementations.Data;
using Emby.Server.Implementations.Serialization;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Users;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SQLitePCL.pretty;

namespace Jellyfin.Server.Migrations.Routines
{
    public class MigrateUserDb : IMigrationRoutine
    {
        private readonly ILogger<MigrateUserDb> _logger;

        private readonly IServerApplicationPaths _paths;

        private readonly JellyfinDbProvider _provider;

        private readonly MyXmlSerializer _xmlSerializer;

        public MigrateUserDb(ILogger<MigrateUserDb> logger, IServerApplicationPaths paths, JellyfinDbProvider provider, MyXmlSerializer xmlSerializer)
        {
            _logger = logger;
            _paths = paths;
            _provider = provider;
            _xmlSerializer = xmlSerializer;
        }

        public Guid Id => Guid.Parse("5C4B82A2-F053-4009-BD05-B6FCAD82F14C");

        public string Name => "MigrateUserDb";

        public void Perform()
        {
            var dataPath = _paths.DataPath;
            _logger.LogInformation("Migrating the user database may take a while, do not stop Jellyfin.");

            using (var connection = SQLite3.Open(Path.Combine(dataPath, "users.db"), ConnectionFlags.ReadOnly, null))
            {
                using var dbContext = _provider.CreateContext();

                var queryResult = connection.Query("SELECT * FROM LocalUsersv2");

                dbContext.RemoveRange(dbContext.Users);
                dbContext.SaveChanges();

                foreach (var entry in queryResult)
                {
                    var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(entry[2].ToString());
                    var userDataDir = Path.Combine(_paths.UserConfigurationDirectoryPath, json["Name"]);

                    var config = (UserConfiguration)_xmlSerializer.DeserializeFromFile(typeof(UserConfiguration), Path.Combine(userDataDir, "config.xml"));
                    var policy = (UserPolicy)_xmlSerializer.DeserializeFromFile(typeof(UserPolicy), Path.Combine(userDataDir, "policy.xml"));

                    var user = new User(
                        json["Name"],
                        false,
                        policy.AuthenticatioIsnProviderId,
                        policy.InvalidLoginAttemptCount,
                        config.SubtitleMode,
                        config.PlayDefaultAudioTrack)
                    {
                        Id = entry[1].ReadGuidFromBlob(),
                        InternalId = entry[0].ToInt64(),
                        MaxParentalAgeRating = policy.MaxParentalRating,
                        EnableUserPreferenceAccess = policy.EnableUserPreferenceAccess,
                        RemoteClientBitrateLimit = policy.RemoteClientBitrateLimit,
                        AuthenticationProviderId = policy.AuthenticatioIsnProviderId,
                        PasswordResetProviderId = policy.PasswordResetProviderId,
                        InvalidLoginAttemptCount = policy.InvalidLoginAttemptCount,
                        LoginAttemptsBeforeLockout = policy.LoginAttemptsBeforeLockout == -1 ? null : new int?(policy.LoginAttemptsBeforeLockout),
                        SubtitleMode = config.SubtitleMode,
                        HidePlayedInLatest = config.HidePlayedInLatest,
                        EnableLocalPassword = config.EnableLocalPassword,
                        PlayDefaultAudioTrack = config.PlayDefaultAudioTrack,
                        DisplayCollectionsView = config.DisplayCollectionsView,
                        DisplayMissingEpisodes = config.DisplayMissingEpisodes,
                        AudioLanguagePreference = config.AudioLanguagePreference,
                        RememberAudioSelections = config.RememberAudioSelections,
                        EnableNextEpisodeAutoPlay = config.EnableNextEpisodeAutoPlay,
                        RememberSubtitleSelections = config.RememberSubtitleSelections,
                        SubtitleLanguagePreference = config.SubtitleLanguagePreference,
                    };

                    user.SetPermission(PermissionKind.IsAdministrator, policy.IsAdministrator);
                    user.SetPermission(PermissionKind.IsHidden, policy.IsHidden);
                    user.SetPermission(PermissionKind.IsDisabled, policy.IsDisabled);
                    user.SetPermission(PermissionKind.EnableSharedDeviceControl, policy.EnableSharedDeviceControl);
                    user.SetPermission(PermissionKind.EnableRemoteAccess, policy.EnableRemoteAccess);
                    user.SetPermission(PermissionKind.EnableLiveTvManagement, policy.EnableLiveTvManagement);
                    user.SetPermission(PermissionKind.EnableLiveTvAccess, policy.EnableLiveTvAccess);
                    user.SetPermission(PermissionKind.EnableMediaPlayback, policy.EnableMediaPlayback);
                    user.SetPermission(PermissionKind.EnableAudioPlaybackTranscoding, policy.EnableAudioPlaybackTranscoding);
                    user.SetPermission(PermissionKind.EnableVideoPlaybackTranscoding, policy.EnableVideoPlaybackTranscoding);
                    user.SetPermission(PermissionKind.EnableContentDeletion, policy.EnableContentDeletion);
                    user.SetPermission(PermissionKind.EnableContentDownloading, policy.EnableContentDownloading);
                    user.SetPermission(PermissionKind.EnableSyncTranscoding, policy.EnableSyncTranscoding);
                    user.SetPermission(PermissionKind.EnableMediaConversion, policy.EnableMediaConversion);
                    user.SetPermission(PermissionKind.EnableAllChannels, policy.EnableAllChannels);
                    user.SetPermission(PermissionKind.EnableAllDevices, policy.EnableAllDevices);
                    user.SetPermission(PermissionKind.EnableAllFolders, policy.EnableAllFolders);
                    user.SetPermission(PermissionKind.EnableRemoteControlOfOtherUsers, policy.EnableRemoteControlOfOtherUsers);
                    user.SetPermission(PermissionKind.EnablePlaybackRemuxing, policy.EnablePlaybackRemuxing);
                    user.SetPermission(PermissionKind.ForceRemoteSourceTranscoding, policy.ForceRemoteSourceTranscoding);
                    user.SetPermission(PermissionKind.EnablePublicSharing, policy.EnablePublicSharing);
                    foreach (var policyAccessSchedule in policy.AccessSchedules)
                    {
                        user.AccessSchedules.Add(policyAccessSchedule);
                    }

                    user.SetPreference(PreferenceKind.BlockedTags, policy.BlockedTags);
                    user.SetPreference(PreferenceKind.EnabledChannels, policy.EnabledChannels);
                    user.SetPreference(PreferenceKind.EnabledDevices, policy.EnabledDevices);
                    user.SetPreference(PreferenceKind.EnabledFolders, policy.EnabledFolders);
                    user.SetPreference(PreferenceKind.EnableContentDeletionFromFolders, policy.EnableContentDeletionFromFolders);
                    user.SetPreference(PreferenceKind.OrderedViews, config.OrderedViews);
                    user.SetPreference(PreferenceKind.GroupedFolders, config.GroupedFolders);
                    user.SetPreference(PreferenceKind.MyMediaExcludes, config.MyMediaExcludes);
                    user.SetPreference(PreferenceKind.LatestItemExcludes, config.LatestItemsExcludes);
                }

                dbContext.SaveChanges();
            }

            try
            {
                File.Move(Path.Combine(dataPath, "users.db"), Path.Combine(dataPath, "users.db" + ".old"));
            }
            catch (IOException e)
            {
                _logger.LogError(e, "Error renaming legacy user database to 'users.db.old'");
            }
        }
    }
}
