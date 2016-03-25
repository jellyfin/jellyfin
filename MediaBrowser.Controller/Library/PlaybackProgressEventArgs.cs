using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Holds information about a playback progress event
    /// </summary>
    public class PlaybackProgressEventArgs : EventArgs
    {
        public List<User> Users { get; set; }
        public long? PlaybackPositionTicks { get; set; }
        public BaseItem Item { get; set; }
        public BaseItemInfo MediaInfo { get; set; }
        public string MediaSourceId { get; set; }
        public bool IsPaused { get; set; }

        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string ClientName { get; set; }

        public string PlaySessionId { get; set; }
      
        public PlaybackProgressEventArgs()
        {
            Users = new List<User>();
        }
    }
}
