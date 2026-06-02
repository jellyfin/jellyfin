using System;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Lyrics;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Migrations.Routines
{
    /// <summary>
    /// Backfills audio lyric file information.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    [JellyfinMigration("2026-06-03T09:00:00", nameof(BackfillAudioLyricFileInfos), "3B9E9FCF-1C67-4326-95C4-4899409AFF67")]
    [JellyfinMigrationBackup(LegacyLibraryDb = true)]
    internal class BackfillAudioLyricFileInfos : IMigrationRoutine
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly ILogger<BackfillAudioLyricFileInfos> _logger;
        private readonly IItemRepository _itemRepository;
        private readonly IItemCountService _countService;
        private readonly IItemPersistenceService _persistenceService;

        public BackfillAudioLyricFileInfos(
            ILoggerFactory loggerFactory,
            IItemRepository itemRepository,
            IItemCountService countService,
            IItemPersistenceService persistenceService)
        {
            _itemRepository = itemRepository;
            _countService = countService;
            _persistenceService = persistenceService;
            _logger = loggerFactory.CreateLogger<BackfillAudioLyricFileInfos>();
        }

        /// <inheritdoc/>
        public void Perform()
        {
            _logger.LogInformation("Backfilling audio lyric file information to database.");
            var startIndex = 0;
            var records = _countService.GetCount(new InternalItemsQuery
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
                    var lyricMediaStreams = audio.GetMediaStreams()
                        .Where(s => s.Type == MediaStreamType.Lyric)
                        .ToList();

                    if (lyricMediaStreams.Count == 0)
                    {
                        continue;
                    }

                    audio.LyricFileInfos = lyricMediaStreams.Select(CreateLyricFileInfo).ToList();
                }

                _persistenceService.SaveItems(results, CancellationToken.None);
                startIndex += results.Count;
                _logger.LogInformation("Backfilled lyric file information for {UpdatedRecords} of {TotalRecords} audio records", startIndex, records);
            }
        }

        private static LyricFileInfo CreateLyricFileInfo(MediaStream stream)
        {
            var extension = System.IO.Path.GetExtension(stream.Path) ?? string.Empty;
            return new LyricFileInfo
            {
                Path = stream.Path,
                Language = string.IsNullOrWhiteSpace(stream.Language) ? null : stream.Language,
                IsExternal = stream.IsExternal,
                IsEmbedded = !stream.IsExternal,
                IsSynced = extension.Equals(".lrc", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".elrc", StringComparison.OrdinalIgnoreCase),
                HasSyllableTiming = extension.Equals(".elrc", StringComparison.OrdinalIgnoreCase),
                StreamIndex = stream.Index
            };
        }
    }
}
