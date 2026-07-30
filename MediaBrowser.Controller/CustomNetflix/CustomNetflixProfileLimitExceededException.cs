#pragma warning disable CS1591

using System;

namespace MediaBrowser.Controller.CustomNetflix;

public sealed class CustomNetflixProfileLimitExceededException : Exception
{
    public CustomNetflixProfileLimitExceededException(int limit)
        : base($"A Jellyfin account cannot have more than {limit} CustomNetflix profiles.")
    {
        Limit = limit;
    }

    public int Limit { get; }
}
