#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.CustomNetflix;

namespace Jellyfin.Server.Implementations.CustomNetflix;

internal static class CustomNetflixManualSegmentPolicy
{
    public const string ManualSource = "manual";

    public static IReadOnlyList<CustomMediaSegmentRow> BuildManualRows(
        Guid itemId,
        IReadOnlyList<CustomNetflixManualMediaSegmentRequest> requests,
        DateTime updatedAt)
        => requests
            .Select(request => BuildManualRow(itemId, request, updatedAt))
            .OrderBy(segment => segment.StartSeconds)
            .ToArray();

    private static CustomMediaSegmentRow BuildManualRow(
        Guid itemId,
        CustomNetflixManualMediaSegmentRequest request,
        DateTime updatedAt)
    {
        var segmentType = CustomNetflixSegmentTypeMapper.NormalizeType(request.Type);
        if (segmentType.Length == 0)
        {
            throw new ArgumentException("Segment type must be intro, recap, outro, or credits.", nameof(request));
        }

        if (request.StartSeconds < 0)
        {
            throw new ArgumentException("Segment start must be greater than or equal to zero.", nameof(request));
        }

        if (request.EndSeconds <= request.StartSeconds)
        {
            throw new ArgumentException("Segment end must be greater than segment start.", nameof(request));
        }

        return new CustomMediaSegmentRow(
            Guid.NewGuid(),
            itemId,
            segmentType,
            request.StartSeconds,
            request.EndSeconds,
            ManualSource,
            updatedAt);
    }
}
