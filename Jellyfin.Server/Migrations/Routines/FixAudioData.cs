using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Fixes the data column of audio types to be deserializable.
    /// </summary>
    internal class FixAudioData : IMigrationRoutine
    {
        private const string DbFilename = "library.db";
        private readonly ILogger<FixAudioData> _logger;
        private readonly IServerApplicationPaths _applicationPaths;
        private readonly IItemRepository _itemRepository;

        public FixAudioData(
            IServerApplicationPaths applicationPaths,
            ILoggerFactory loggerFactory,
            IItemRepository itemRepository)
        {
            _applicationPaths = applicationPaths;
            _itemRepository = itemRepository;
            _logger = loggerFactory.CreateLogger<FixAudioData>();
        }

        /// <inheritdoc/>
        public Guid Id => Guid.Parse("{CF6FABC2-9FBE-4933-84A5-FFE52EF22A58}");

        /// <inheritdoc/>
        public string Name => "FixAudioData";

        /// <inheritdoc/>
        public bool PerformOnNewInstall => false;

        /// <inheritdoc/>
        public void Perform()
        {
            var dbPath = Path.Combine(_applicationPaths.DataPath, DbFilename);

            // Back up the database before modifying any entries
            for (int i = 1; ; i++)
            {
                var bakPath = string.Format(CultureInfo.InvariantCulture, "{0}.bak{1}", dbPath, i);
                if (!File.Exists(bakPath))
                {
                    try
                    {
                        _logger.LogInformation("Backing up {Library} to {BackupPath}", DbFilename, bakPath);
                        File.Copy(dbPath, bakPath);
                        _logger.LogInformation("{Library} backed up to {BackupPath}", DbFilename, bakPath);
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Cannot make a backup of {Library} at path {BackupPath}", DbFilename, bakPath);
                        throw;
                    }
                }
            }

            _logger.LogInformation("Backfilling audio lyrics data to database.");
            var startIndex = 0;
            var records = _itemRepository.GetCount(new InternalItemsQuery
            {
                IncludeItemTypes = [BaseItemKind.Audio],
            });

            while (startIndex < records)
            {
                var results = _itemRepository.GetItemList(new InternalItemsQuery
                {
                    IncludeItemTypes = [BaseItemKind.Audio],
                    StartIndex = startIndex,
                    Limit = 5000,
                    SkipDeserialization = true
                })
                .Cast<Audio>()
                .ToList();

                foreach (var audio in results)
                {
                    var lyricMediaStreams = audio.GetMediaStreams().Where(s => s.Type == MediaStreamType.Lyric).Select(s => s.Path).ToList();
                    if (lyricMediaStreams.Count > 0)
                    {
                        audio.HasLyrics = true;
                        audio.LyricFiles = lyricMediaStreams;
                    }
                }

                _itemRepository.SaveItems(results, CancellationToken.None);
                startIndex += results.Count;
                _logger.LogInformation("Backfilled data for {UpdatedRecords} of {TotalRecords} audio records", startIndex, records);
            }
        }
    }
}
