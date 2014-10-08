using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.ApiClient
{
    /// <summary>
    /// Class SessionUpdatesEventArgs
    /// </summary>
    public class SessionUpdatesEventArgs
    {
        public SessionInfoDto[] Sessions { get; set; }
    }
}