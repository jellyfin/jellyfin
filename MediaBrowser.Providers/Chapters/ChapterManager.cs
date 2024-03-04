#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Entities;
using Microsoft.EntityFrameworkCore;

namespace MediaBrowser.Providers.Chapters
{
    public class ChapterManager : IChapterManager
    {
        private readonly IDbContextFactory<LibraryDbContext> _provider;

        public ChapterManager(IDbContextFactory<LibraryDbContext> provider)
        {
            _provider = provider;
        }

        /// <inheritdoc />
        public async Task<List<ChapterInfo>> GetChapters(BaseItem item, CancellationToken cancellationToken)
        {
            List<ChapterInfo> chapters;
            var dbContext = await _provider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                chapters = dbContext.ChapterInfos.Where(p => p.ItemId.Equals(item.Id)).ToList();
            }

            return chapters;
        }

        /// <inheritdoc />
        public async Task<ChapterInfo?> GetChapter(BaseItem item, int chapterIndex, CancellationToken cancellationToken)
        {
            ChapterInfo? chapter;
            var dbContext = await _provider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                chapter = await dbContext.ChapterInfos.FirstOrDefaultAsync(p => p.ItemId.Equals(item.Id) && p.ChapterIndex == chapterIndex, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return chapter;
        }

        /// <inheritdoc />
        public async Task SaveChapters(Guid itemId, IReadOnlyList<ChapterInfo> chapters)
        {
            if (itemId.Equals(default))
            {
                throw new ArgumentNullException(nameof(itemId));
            }

            ArgumentNullException.ThrowIfNull(chapters);

            var dbContext = await _provider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                await dbContext.ChapterInfos.Where(e => e.ItemId.Equals(itemId)).ExecuteDeleteAsync().ConfigureAwait(false);
                int index = 0;
                foreach (var chapterInfo in chapters)
                {
                    chapterInfo.ItemId = itemId;
                    chapterInfo.ChapterIndex = index;
                    dbContext.ChapterInfos.Add(chapterInfo);
                    index++;
                }

                await dbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }
    }
}
