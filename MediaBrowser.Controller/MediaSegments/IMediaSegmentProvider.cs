using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model;
using MediaBrowser.Model.MediaSegments;

namespace MediaBrowser.Controller;

/// <summary>
/// Provides methods for Obtaining the Media Segments from an Item.
/// </summary>
public interface IMediaSegmentProvider
{
    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Enumerates all Media Segments from an Media Item.
    /// </summary>
    /// <param name="request">Arguments to enumerate MediaSegments.</param>
    /// <param name="cancellationToken">Abort token.</param>
    /// <returns>A list of all MediaSegments found from this provider.</returns>
    Task<IReadOnlyList<MediaSegmentDto>> GetMediaSegments(MediaSegmentGenerationRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Should return support state for the given item.
    /// </summary>
    /// <param name="item">The base item to extract segments from.</param>
    /// <returns>True if item is supported, otherwise false.</returns>
    ValueTask<bool> Supports(BaseItem item);
}
