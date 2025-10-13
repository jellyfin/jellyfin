using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Chapters;

/// <summary>
/// Interface IChapterManager.
/// </summary>
public interface IChapterManager
{
    /// <summary>
    /// Saves the chapters.
    /// </summary>
    /// <param name="video">The video.</param>
    /// <param name="chapters">The set of chapters.</param>
    void SaveChapters(Video video, IReadOnlyList<ChapterInfo> chapters);

    /// <summary>
    /// Gets a single chapter of a BaseItem on a specific index.
    /// </summary>
    /// <param name="baseItemId">The BaseItems id.</param>
    /// <param name="index">The index of that chapter.</param>
    /// <returns>A chapter instance.</returns>
    ChapterInfo? GetChapter(Guid baseItemId, int index);

    /// <summary>
    /// Gets all chapters associated with the baseItem.
    /// </summary>
    /// <param name="baseItemId">The BaseItems id.</param>
    /// <returns>A readonly list of chapter instances.</returns>
    IReadOnlyList<ChapterInfo> GetChapters(Guid baseItemId);

    /// <summary>
    /// Refreshes the chapter images.
    /// </summary>
    /// <param name="video">Video to use.</param>
    /// <param name="directoryService">Directory service to use.</param>
    /// <param name="chapters">Set of chapters to refresh.</param>
    /// <param name="extractImages">Option to extract images.</param>
    /// <param name="saveChapters">Option to save chapters.</param>
    /// <param name="cancellationToken">CancellationToken to use for operation.</param>
    /// <returns><c>true</c> if successful, <c>false</c> if not.</returns>
    Task<bool> RefreshChapterImages(Video video, IDirectoryService directoryService, IReadOnlyList<ChapterInfo> chapters, bool extractImages, bool saveChapters, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes the chapter data.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Task.</returns>
    Task DeleteChapterDataAsync(Guid itemId, CancellationToken cancellationToken);
}
