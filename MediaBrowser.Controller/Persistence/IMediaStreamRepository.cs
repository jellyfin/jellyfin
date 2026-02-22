#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Persistence;

/// <summary>
/// Provides methods for accessing MediaStreams.
/// </summary>
public interface IMediaStreamRepository
{
    /// <summary>
    /// Gets the media streams.
    /// </summary>
    /// <param name="filter">The query.</param>
    /// <returns>IEnumerable{MediaStream}.</returns>
    IReadOnlyList<MediaStream> GetMediaStreams(MediaStreamQuery filter);

    /// <summary>
    /// Saves the media streams.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <param name="streams">The streams.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    void SaveMediaStreams(Guid id, IReadOnlyList<MediaStream> streams, CancellationToken cancellationToken);
}
