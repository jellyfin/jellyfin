#pragma warning disable CS1591

using System;

namespace MediaBrowser.Controller.CustomNetflix;

public sealed class CustomNetflixUnavailableException : Exception
{
    public CustomNetflixUnavailableException(string message)
        : base(message)
    {
    }
}
