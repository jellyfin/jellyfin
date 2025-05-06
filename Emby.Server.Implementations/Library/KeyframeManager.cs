using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.MediaEncoding.Keyframes;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Persistence;

namespace Emby.Server.Implementations.Library;

/// <summary>
/// Manager for Keyframe data.
/// </summary>
public class KeyframeManager : IKeyframeManager
{
    private readonly IKeyframeRepository _repository;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyframeManager"/> class.
    /// </summary>
    /// <param name="repository">The keyframe repository.</param>
    public KeyframeManager(IKeyframeRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public IReadOnlyList<KeyframeData> GetKeyframeData(Guid itemId)
    {
        return _repository.GetKeyframeData(itemId);
    }

    /// <inheritdoc />
    public async Task SaveKeyframeDataAsync(Guid itemId, KeyframeData data, CancellationToken cancellationToken)
    {
        await _repository.SaveKeyframeDataAsync(itemId, data, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DeleteKeyframeDataAsync(Guid itemId, CancellationToken cancellationToken)
    {
        await _repository.DeleteKeyframeDataAsync(itemId, cancellationToken).ConfigureAwait(false);
    }
}
