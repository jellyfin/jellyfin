using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using SQLitePCL.pretty;

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
        private readonly JellyfinDbProvider _provider;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MigrateDisplayPreferencesDb"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="paths">The server application paths.</param>
        /// <param name="provider">The database provider.</param>
        public MigrateDisplayPreferencesDb(ILogger<MigrateDisplayPreferencesDb> logger, IServerApplicationPaths paths, JellyfinDbProvider provider)
        {
            _logger = logger;
            _paths = paths;
            _provider = provider;
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

            var dbFilePath = Path.Combine(_paths.DataPath, DbFilename);
            using (var connection = SQLite3.Open(dbFilePath, ConnectionFlags.ReadOnly, null))
            {
                using var dbContext = _provider.CreateContext();

                var results = connection.Query("SELECT * FROM userdisplaypreferences");
                foreach (var result in results)
                {
                    var dto = JsonSerializer.Deserialize<DisplayPreferencesDto>(result[3].ToString(), _jsonOptions);
                    if (dto == null)
                    {
                        continue;
                    }

                    var chromecastVersion = dto.CustomPrefs.TryGetValue("chromecastVersion", out var version)
                        ? chromecastDict[version]
                        : ChromecastVersion.Stable;

                    var displayPreferences = new DisplayPreferences(new Guid(result[1].ToBlob()), result[2].ToString())
                    {
                        IndexBy = Enum.TryParse<IndexingKind>(dto.IndexBy, true, out var indexBy) ? indexBy : (IndexingKind?)null,
                        ShowBackdrop = dto.ShowBackdrop,
                        ShowSidebar = dto.ShowSidebar,
                        ScrollDirection = dto.ScrollDirection,
                        ChromecastVersion = chromecastVersion,
                        SkipForwardLength = dto.CustomPrefs.TryGetValue("skipForwardLength", out var length)
                            ? int.Parse(length, CultureInfo.InvariantCulture)
                            : 30000,
                        SkipBackwardLength = dto.CustomPrefs.TryGetValue("skipBackLength", out length)
                            ? int.Parse(length, CultureInfo.InvariantCulture)
                            : 10000,
                        EnableNextVideoInfoOverlay = dto.CustomPrefs.TryGetValue("enableNextVideoInfoOverlay", out var enabled)
                            ? bool.Parse(enabled)
                            : true,
                        DashboardTheme = dto.CustomPrefs.TryGetValue("dashboardtheme", out var theme) ? theme : string.Empty,
                        TvHome = dto.CustomPrefs.TryGetValue("tvhome", out var home) ? home : string.Empty
                    };

                    for (int i = 0; i < 7; i++)
                    {
                        dto.CustomPrefs.TryGetValue("homesection" + i, out var homeSection);

                        displayPreferences.HomeSections.Add(new HomeSection
                        {
                            Order = i,
                            Type = Enum.TryParse<HomeSectionType>(homeSection, true, out var type) ? type : defaults[i]
                        });
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
                        if (!Guid.TryParse(key.AsSpan().Slice("landing-".Length), out var itemId))
                        {
                            continue;
                        }

                        var libraryDisplayPreferences = new ItemDisplayPreferences(displayPreferences.UserId, itemId, displayPreferences.Client)
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

                        dbContext.ItemDisplayPreferences.Add(libraryDisplayPreferences);
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
