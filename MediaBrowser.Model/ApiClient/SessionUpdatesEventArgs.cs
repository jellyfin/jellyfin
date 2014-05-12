using System;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.ApiClient
{
    /// <summary>
    /// Class SessionUpdatesEventArgs
    /// </summary>
    public class SessionUpdatesEventArgs : EventArgs
    {
        public SessionInfoDto[] Sessions { get; set; }
    }
}