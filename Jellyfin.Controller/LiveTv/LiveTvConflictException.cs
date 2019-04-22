using System;

namespace Jellyfin.Controller.LiveTv
{
    /// <summary>
    /// Class LiveTvConflictException.
    /// </summary>
    public class LiveTvConflictException : Exception
    {
        public LiveTvConflictException()
        {

        }
        public LiveTvConflictException(string message)
            : base(message)
        {

        }
    }
}
