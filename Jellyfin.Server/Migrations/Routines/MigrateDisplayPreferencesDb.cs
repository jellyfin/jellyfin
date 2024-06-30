using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Emby.Server.Implementations.Data;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// The migration routine for migrating the display preferences database to EF Core.
    /// </summary>
    public class MigrateDisplayPreferencesDb : IMigrationRoutine
    {
        private const string DbFilename = "displaypreferences.db";

        private readonly ILogger<MigrateDisplayPreferencesDb> _logger;
        private readonly IServerApplicationPaths _paths;
        private readonly IDbContextFactory<JellyfinDbContext> _provider;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IUserManager _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrateDisplayPreferencesDb"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="paths">The server application paths.</param>
        /// <param name="provider">The database provider.</param>
        /// <param name="userManager">The user manager.</param>
        public MigrateDisplayPreferencesDb(
            ILogger<MigrateDisplayPreferencesDb> logger,
            IServerApplicationPaths paths,
            IDbContextFactory<JellyfinDbContext> provider,
            IUserManager userManager)
        {
            _logger = logger;
            _paths = paths;
            _provider = provider;
            _userManager = userManager;
            _jsonOptions = new JsonSerializerOptions();
            _jsonOptions.Converters.Add(new JsonStringEnumConverter());
        }

        /// <inheritdoc />
        public Guid Id => Guid.Parse("06387815-C3CC-421F-A888-FB5F9992BEA8");

        /// <inheritdoc />
        public string Name => "MigrateDisplayPreferencesDatabase";

        /// <inheritdoc />
        public bool PerformOnNewInstall => false;

        /// <inheritdoc />
        public void Perform()
        {
            HomeSectionType[] defaults =
            {
                HomeSectionType.SmallLibraryTiles,
                HomeSectionType.Resume,
                HomeSectionType.ResumeAudio,
                HomeSectionType.LiveTv,
                HomeSectionType.NextUp,
                HomeSectionType.LatestMedia,
                HomeSectionType.None,
            };

            var chromecastDict = new Dictionary<string, ChromecastVersion>(StringComparer.OrdinalIgnoreCase)
            {
                { "stable", ChromecastVersion.Stable },
                { "nightly", ChromecastVersion.Unstable },
                { "unstable", ChromecastVersion.Unstable }
            };

            var displayPrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var customDisplayPrefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dbFilePath = Path.Combine(_paths.DataPath, DbFilename);
            using (var connection = new SqliteConnection($"Filename={dbFilePath}"))
            {
                connection.Open();
                using var dbContext = _provider.CreateDbContext();

                var results = connection.Query("SELECT * FROM userdisplaypreferences");
                foreach (var result in results)
                {
                    var dto = JsonSerializer.Deserialize<DisplayPreferencesDto>(result.GetStream(3), _jsonOptions);
                    if (dto is null)
                    {
                        continue;
                    }

                    var itemId = result.GetGuid(1);
                    var dtoUserId = itemId;
                    var client = result.GetString(2);
                    var displayPreferencesKey = $"{dtoUserId}|{itemId}|{client}";
                    if (displayPrefs.Contains(displayPreferencesKey))
                    {
                        // Duplicate display preference.
                        continue;
                    }

                    displayPrefs.Add(displayPreferencesKey);
                    var existingUser = _userManager.GetUserById(dtoUserId);
                    if (existingUser is null)
                    {
                        _logger.LogWarning("User with ID {UserId} does not exist in the database, skipping migration.", dtoUserId);
                        continue;
                    }

                    var chromecastVersion = dto.CustomPrefs.TryGetValue("chromecastVersion", out var version)
                                            && !string.IsNullOrEmpty(version)
                        ? chromecastDict[version]
                        : ChromecastVersion.Stable;
                    dto.CustomPrefs.Remove("chromecastVersion");

                    var displayPreferences = new DisplayPreferences(dtoUserId, itemId, client)
                    {
                        IndexBy = Enum.TryParse<IndexingKind>(dto.IndexBy, true, out var indexBy) ? indexBy : null,
                        ShowBackdrop = dto.ShowBackdrop,
                        ShowSidebar = dto.ShowSidebar,
                        ScrollDirection = dto.ScrollDirection,
                        ChromecastVersion = chromecastVersion,
                        SkipForwardLength = dto.CustomPrefs.TryGetValue("skipForwardLength", out var length) && int.TryParse(length, out var skipForwardLength)
                            ? skipForwardLength
                            : 30000,
                        SkipBackwardLength = dto.CustomPrefs.TryGetValue("skipBackLength", out length) && int.TryParse(length, out var skipBackwardLength)
                            ? skipBackwardLength
                            : 10000,
                        EnableNextVideoInfoOverlay = !dto.CustomPrefs.TryGetValue("enableNextVideoInfoOverlay", out var enabled) || string.IsNullOrEmpty(enabled) || bool.Parse(enabled),
                        DashboardTheme = dto.CustomPrefs.TryGetValue("dashboardtheme", out var theme) ? theme : string.Empty,
                        TvHome = dto.CustomPrefs.TryGetValue("tvhome", out var home) ? home : string.Empty
                    };

                    dto.CustomPrefs.Remove("skipForwardLength");
                    dto.CustomPrefs.Remove("skipBackLength");
                    dto.CustomPrefs.Remove("enableNextVideoInfoOverlay");
                    dto.CustomPrefs.Remove("dashboardtheme");
                    dto.CustomPrefs.Remove("tvhome");

                    for (int i = 0; i < 7; i++)
                    {
                        var key = "homesection" + i;
                        dto.CustomPrefs.TryGetValue(key, out var homeSection);

                        displayPreferences.HomeSections.Add(new HomeSection
                        {
                            Order = i,
                            Type = Enum.TryParse<HomeSectionType>(homeSection, true, out var type) ? type : defaults[i]
                        });

                        dto.CustomPrefs.Remove(key);
                    }

                    var defaultLibraryPrefs = new ItemDisplayPreferences(displayPreferences.UserId, Guid.Empty, displayPreferences.Client)
                    {
                        SortBy = dto.SortBy ?? "SortName",
                        SortOrder = dto.SortOrder,
                        RememberIndexing = dto.RememberIndexing,
                        RememberSorting = dto.RememberSorting,
                    };

                    dbContext.Add(defaultLibraryPrefs);

                    foreach (var key in dto.CustomPrefs.Keys.Where(key => key.StartsWith("landing-", StringComparison.Ordinal)))
                    {
                        if (!Guid.TryParse(key.AsSpan().Slice("landing-".Length), out var landingItemId))
                        {
                            continue;
                        }

                        var libraryDisplayPreferences = new ItemDisplayPreferences(displayPreferences.UserId, landingItemId, displayPreferences.Client)
                        {
                            SortBy = dto.SortBy ?? "SortName",
                            SortOrder = dto.SortOrder,
                            RememberIndexing = dto.RememberIndexing,
                            RememberSorting = dto.RememberSorting,
                        };

                        if (Enum.TryParse<ViewType>(dto.ViewType, true, out var viewType))
                        {
                            libraryDisplayPreferences.ViewType = viewType;
                        }

                        dto.CustomPrefs.Remove(key);
                        dbContext.ItemDisplayPreferences.Add(libraryDisplayPreferences);
                    }

                    foreach (var (key, value) in dto.CustomPrefs)
                    {
                        // Custom display preferences can have a key collision.
                        var indexKey = $"{displayPreferences.UserId}|{itemId}|{displayPreferences.Client}|{key}";
                        if (!customDisplayPrefs.Contains(indexKey))
                        {
                            dbContext.Add(new CustomItemDisplayPreferences(displayPreferences.UserId, itemId, displayPreferences.Client, key, value));
                            customDisplayPrefs.Add(indexKey);
                        }
                    }

                    dbContext.Add(displayPreferences);
                }

                dbContext.SaveChanges();
            }

            try
            {
                File.Move(dbFilePath, dbFilePath + ".old");

                var journalPath = dbFilePath + "-journal";
                if (File.Exists(journalPath))
                {
                    File.Move(journalPath, dbFilePath + ".old-journal");
                }
            }
            catch (IOException e)
            {
                _logger.LogError(e, "Error renaming legacy display preferences database to 'displaypreferences.db.old'");
            }
        }
    }
}
