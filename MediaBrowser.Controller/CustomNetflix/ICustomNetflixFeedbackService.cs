#pragma warning disable CS1591

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.CustomNetflix;

public interface ICustomNetflixFeedbackService
{
    Task<CustomNetflixItemFeedbackDto?> GetAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken);

    Task<CustomNetflixItemFeedbackDto?> SetAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CustomNetflixItemFeedbackRequest request,
        CancellationToken cancellationToken);

    Task<CustomNetflixItemFeedbackDto?> ClearAsync(
        Guid jellyfinUserId,
        Guid profileId,
        Guid itemId,
        CancellationToken cancellationToken);
}
