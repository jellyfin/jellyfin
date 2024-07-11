#pragma warning disable CS1591

using System;

namespace MediaBrowser.Controller.LiveTv
{
    /// <summary>
    /// Class LiveTvConflictException.
    /// </summary>
    public class LiveTvConflictException : Exception
    {
        public LiveTvConflictException(string message)
            : base(message)
        {
        }
    }
}
