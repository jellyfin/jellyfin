using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Entities;

/// <summary>
/// Compare MediaSource of the same file by Video width <see cref="IComparer{T}" />.
/// </summary>
public class MediaSourceWidthComparator : IComparer<MediaSourceInfo>
{
    /// <inheritdoc />
    public int Compare(MediaSourceInfo? x, MediaSourceInfo? y)
    {
        if (x is null && y is null)
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        if (string.Equals(x.Path, y.Path, StringComparison.OrdinalIgnoreCase))
        {
            if (x.VideoStream is null && y.VideoStream is null)
            {
                return 0;
            }

            if (x.VideoStream is null)
            {
                return -1;
            }

            if (y.VideoStream is null)
            {
                return 1;
            }

            var xWidth = x.VideoStream.Width ?? 0;
            var yWidth = y.VideoStream.Width ?? 0;

            return xWidth - yWidth;
        }

        return 0;
    }
}
