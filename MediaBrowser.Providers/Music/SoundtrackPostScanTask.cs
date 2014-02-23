using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.Music
{
    public class SoundtrackPostScanTask : ILibraryPostScanTask
    {
        private readonly ILibraryManager _libraryManager;

        public SoundtrackPostScanTask(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        private readonly Task _cachedTask = Task.FromResult(true);
        public Task Run(IProgress<double> progress, CancellationToken cancellationToken)
        {
            RunInternal(progress, cancellationToken);

            return _cachedTask;
        }

        private void RunInternal(IProgress<double> progress, CancellationToken cancellationToken)
        {
            var allItems = _libraryManager.RootFolder
                .RecursiveChildren
                .ToList();

            var musicAlbums = allItems
                .OfType<MusicAlbum>()
                .ToList();

            var itemsWithSoundtracks = allItems.OfType<IHasSoundtracks>().ToList();

            foreach (var item in itemsWithSoundtracks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                item.SoundtrackIds = GetSoundtrackIds(item, musicAlbums).ToList();
            }

            progress.Report(50);

            itemsWithSoundtracks = itemsWithSoundtracks.Where(i => i.SoundtrackIds.Count > 0).ToList();

            foreach (var album in musicAlbums)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                album.SoundtrackIds = GetAlbumLinks(album.Id, itemsWithSoundtracks).ToList();
            }

            progress.Report(100);
        }

        private IEnumerable<Guid> GetSoundtrackIds(IHasSoundtracks item, IEnumerable<MusicAlbum> albums)
        {
            var itemName = GetComparableName(item.Name);

            return albums.Where(i => string.Equals(itemName, GetComparableName(i.Name), StringComparison.OrdinalIgnoreCase)).Select(i => i.Id);
        }

        private static string GetComparableName(string name)
        {
            name = " " + name + " ";

            name = name.Replace(".", " ")
            .Replace("_", " ")
            .Replace("&", " ")
            .Replace("!", " ")
            .Replace("(", " ")
            .Replace(")", " ")
            .Replace(",", " ")
            .Replace("-", " ")
            .Replace(" a ", String.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" the ", String.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", String.Empty);

            return name.Trim();
        }

        /// <summary>
        /// Removes the diacritics.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>System.String.</returns>
        private static string RemoveDiacritics(string text)
        {
            return String.Concat(
                text.Normalize(NormalizationForm.FormD)
                .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                              UnicodeCategory.NonSpacingMark)
              ).Normalize(NormalizationForm.FormC);
        }

        private IEnumerable<Guid> GetAlbumLinks(Guid albumId, IEnumerable<IHasSoundtracks> items)
        {
            return items.Where(i => i.SoundtrackIds.Contains(albumId)).Select(i => i.Id);
        }
    }
}
