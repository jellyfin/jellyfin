
namespace MediaBrowser.Model.Session
{
    public class SessionCapabilities
    {
        public string[] PlayableMediaTypes { get; set; }

        public bool SupportsFullscreenToggle { get; set; }

        public SessionCapabilities()
        {
            PlayableMediaTypes = new string[] {};
        }
    }
}
