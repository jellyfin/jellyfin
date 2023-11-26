#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Server.Implementations;
using MediaBrowser.Controller.Chapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Providers.Chapters
{
    public class ChapterManager : IChapterManager
    {
        private readonly IDbContextFactory<LibraryDbContext> _provider;
        private readonly ILogger<ChapterManager> _logger;

        public ChapterManager(IDbContextFactory<LibraryDbContext> provider, ILogger<ChapterManager> logger)
        {
            _provider = provider;
            this._logger = logger;
        }

        /// <inheritdoc />
        public async void SaveChapters(Guid itemId, IReadOnlyList<ChapterInfo> chapters)
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
