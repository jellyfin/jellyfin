#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Defines a named Live TV view and the tuner hosts it contains.
    /// </summary>
    public class LiveTvInstanceInfo
    {
        public LiveTvInstanceInfo()
        {
            TunerHostIds = Array.Empty<string>();
        }

        public Guid Id { get; set; }

        public string Name { get; set; }

        public string[] TunerHostIds { get; set; }
    }
}
