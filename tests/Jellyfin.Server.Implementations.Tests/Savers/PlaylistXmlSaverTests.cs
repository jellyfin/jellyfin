using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.LocalMetadata.Savers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Savers
{
    public sealed class PlaylistXmlSaverTests
    {
        // Regression test for #12008: a library scan briefly removes and re-adds items, so a
        // playlist item's ItemId can momentarily fail to resolve to a path. The saver used to
        // drop such entries from playlist.xml, permanently emptying playlists after a scan.
        // The entry must instead be preserved via its ItemId.
        [Fact]
        public async Task SaveAsync_WhenLinkedChildIsTemporarilyUnresolvable_PreservesEntryByItemId()
        {
            Video.RecordingsManager = Mock.Of<IRecordingsManager>();

            var resolvableId = Guid.NewGuid();
            var unresolvableId = Guid.NewGuid(); // simulates an item removed during a scan

            var libraryManager = new Mock<ILibraryManager> { DefaultValue = DefaultValue.Empty };
            libraryManager
                .Setup(l => l.GetPeople(It.IsAny<BaseItem>()))
                .Returns(new List<PersonInfo>());
            libraryManager
                .Setup(l => l.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(new List<BaseItem> { new Movie { Id = resolvableId, Path = "/media/Movie/movie.mkv" } });

            var configManager = new Mock<IServerConfigurationManager>();
            configManager.Setup(c => c.Configuration).Returns(new ServerConfiguration());

            var saver = new PlaylistXmlSaver(
                Mock.Of<IFileSystem>(),
                configManager.Object,
                libraryManager.Object,
                NullLogger<PlaylistXmlSaver>.Instance);

            var tempDir = Directory.CreateTempSubdirectory("jf-playlist-test-");
            try
            {
                var playlist = new Playlist
                {
                    Path = tempDir.FullName,
                    PlaylistMediaType = MediaType.Video,
                    LinkedChildren =
                    [
                        new LinkedChild { ItemId = resolvableId, Type = LinkedChildType.Manual },
                        new LinkedChild { ItemId = unresolvableId, Type = LinkedChildType.Manual },
                    ],
                };

                await saver.SaveAsync(playlist, TestContext.Current.CancellationToken);

                var xml = await File.ReadAllTextAsync(
                    PlaylistXmlSaver.GetSavePath(tempDir.FullName),
                    TestContext.Current.CancellationToken);

                // Both linked children must survive the save, even the one that could not be
                // resolved to a path at save time.
                Assert.Equal(2, CountOccurrences(xml, "<PlaylistItem>"));
                Assert.Contains(resolvableId.ToString("N"), xml, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(unresolvableId.ToString("N"), xml, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                tempDir.Delete(true);
            }
        }

        private static int CountOccurrences(string haystack, string needle)
        {
            var count = 0;
            var index = 0;
            while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += needle.Length;
            }

            return count;
        }
    }
}
