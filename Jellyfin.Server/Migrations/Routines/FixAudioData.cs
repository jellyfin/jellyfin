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
#pragma warning disable CS0618 // Type or member is obsolete
    [JellyfinMigration("2025-04-20T18:00:00", nameof(FixAudioData), "CF6FABC2-9FBE-4933-84A5-FFE52EF22A58")]
    [JellyfinMigrationBackup(LegacyLibraryDb = true)]
    internal class FixAudioData : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
    {
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
        public void Perform()
        {
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
