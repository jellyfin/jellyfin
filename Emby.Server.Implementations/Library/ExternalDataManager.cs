using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Controller.Trickplay;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library;

/// <summary>
/// IExternalDataManager implementation.
/// </summary>
public class ExternalDataManager : IExternalDataManager
{
    private readonly IKeyframeManager _keyframeManager;
    private readonly IMediaSegmentManager _mediaSegmentManager;
    private readonly IPathManager _pathManager;
    private readonly ITrickplayManager _trickplayManager;
    private readonly IChapterManager _chapterManager;
    private readonly ILogger<ExternalDataManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalDataManager"/> class.
    /// </summary>
    /// <param name="keyframeManager">The keyframe manager.</param>
    /// <param name="mediaSegmentManager">The media segment manager.</param>
    /// <param name="pathManager">The path manager.</param>
    /// <param name="trickplayManager">The trickplay manager.</param>
    /// <param name="chapterManager">The chapter manager.</param>
    /// <param name="logger">The logger.</param>
    public ExternalDataManager(
        IKeyframeManager keyframeManager,
        IMediaSegmentManager mediaSegmentManager,
        IPathManager pathManager,
        ITrickplayManager trickplayManager,
        IChapterManager chapterManager,
        ILogger<ExternalDataManager> logger)
    {
        _keyframeManager = keyframeManager;
        _mediaSegmentManager = mediaSegmentManager;
        _pathManager = pathManager;
        _trickplayManager = trickplayManager;
        _chapterManager = chapterManager;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task DeleteExternalItemDataAsync(BaseItem item, CancellationToken cancellationToken)
    {
        var validPaths = _pathManager.GetExtractedDataPaths(item).Where(Directory.Exists).ToList();
        var itemId = item.Id;
        if (validPaths.Count > 0)
        {
            foreach (var path in validPaths)
            {
                try
                {
                    Directory.Delete(path, true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Unable to prune external item data at {Path}: {Exception}", path, ex);
                }
            }
        }

        await _keyframeManager.DeleteKeyframeDataAsync(itemId, cancellationToken).ConfigureAwait(false);
        await _mediaSegmentManager.DeleteSegmentsAsync(itemId, cancellationToken).ConfigureAwait(false);
        await _trickplayManager.DeleteTrickplayDataAsync(itemId, cancellationToken).ConfigureAwait(false);
        await _chapterManager.DeleteChapterDataAsync(itemId, cancellationToken).ConfigureAwait(false);
    }
}
