using System;
using System.IO;
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

            var dbFilePath = Path.Combine(_paths.DataPath, DbFilename);
            using (var connection = SQLite3.Open(dbFilePath, ConnectionFlags.ReadOnly, null))
            {
                var dbContext = _provider.CreateContext();

                var results = connection.Query("SELECT * FROM userdisplaypreferences");
                foreach (var result in results)
                {
                    var dto = JsonSerializer.Deserialize<DisplayPreferencesDto>(result[3].ToString(), _jsonOptions);
                    var chromecastVersion = dto.CustomPrefs.TryGetValue("chromecastVersion", out var version)
                        ? Enum.TryParse<ChromecastVersion>(version, true, out var parsed)
                            ? parsed
                            : ChromecastVersion.Stable
                        : ChromecastVersion.Stable;

                    var displayPreferences = new DisplayPreferences(result[2].ToString(), new Guid(result[1].ToBlob()))
                    {
                        ViewType = Enum.TryParse<ViewType>(dto.ViewType, true, out var viewType) ? viewType : (ViewType?)null,
                        IndexBy = Enum.TryParse<IndexingKind>(dto.IndexBy, true, out var indexBy) ? indexBy : (IndexingKind?)null,
                        ShowBackdrop = dto.ShowBackdrop,
                        ShowSidebar = dto.ShowSidebar,
                        SortBy = dto.SortBy,
                        SortOrder = dto.SortOrder,
                        RememberIndexing = dto.RememberIndexing,
                        RememberSorting = dto.RememberSorting,
                        ScrollDirection = dto.ScrollDirection,
                        ChromecastVersion = chromecastVersion
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
