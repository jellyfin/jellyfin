#pragma warning disable CA1826 // Do not use Enumerable methods on indexable collections

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Jellyfin.MediaEncoding.Hls.Extractors;
using Jellyfin.MediaEncoding.Keyframes;
using MediaBrowser.Controller.Persistence;
using Microsoft.Extensions.Logging;

namespace Jellyfin.MediaEncoding.Hls.Cache;

/// <inheritdoc />
public class CacheDecorator : IKeyframeExtractor
{
    private readonly IKeyframeRepository _keyframeRepository;
    private readonly IKeyframeExtractor _keyframeExtractor;
    private readonly ILogger<CacheDecorator> _logger;
    private readonly string _keyframeExtractorName;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheDecorator"/> class.
    /// </summary>
    /// <param name="keyframeRepository">An instance of the <see cref="IKeyframeRepository"/> interface.</param>
    /// <param name="keyframeExtractor">An instance of the <see cref="IKeyframeExtractor"/> interface.</param>
    /// <param name="logger">An instance of the <see cref="ILogger{CacheDecorator}"/> interface.</param>
    public CacheDecorator(IKeyframeRepository keyframeRepository, IKeyframeExtractor keyframeExtractor, ILogger<CacheDecorator> logger)
    {
        ArgumentNullException.ThrowIfNull(keyframeRepository);
        ArgumentNullException.ThrowIfNull(keyframeExtractor);

        _keyframeRepository = keyframeRepository;
        _keyframeExtractor = keyframeExtractor;
        _logger = logger;
        _keyframeExtractorName = keyframeExtractor.GetType().Name;
    }

    /// <inheritdoc />
    public bool IsMetadataBased => _keyframeExtractor.IsMetadataBased;

    /// <inheritdoc />
    public bool TryExtractKeyframes(Guid itemId, string filePath, [NotNullWhen(true)] out KeyframeData? keyframeData)
    {
        keyframeData = _keyframeRepository.GetKeyframeData(itemId).FirstOrDefault();
        if (keyframeData is null)
        {
            if (!_keyframeExtractor.TryExtractKeyframes(itemId, filePath, out var result))
            {
                _logger.LogDebug("Failed to extract keyframes using {ExtractorName}", _keyframeExtractorName);
                return false;
            }

            _logger.LogDebug("Successfully extracted keyframes using {ExtractorName}", _keyframeExtractorName);
            keyframeData = result;
            _keyframeRepository.SaveKeyframeDataAsync(itemId, keyframeData, CancellationToken.None).GetAwaiter().GetResult();
        }

        return true;
    }
}
