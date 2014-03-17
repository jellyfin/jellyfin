
namespace MediaBrowser.Model.Configuration
{
    public class DlnaOptions
    {
        public bool EnablePlayTo { get; set; }
        public bool EnablePlayToDebugLogging { get; set; }

        public DlnaOptions()
        {
            EnablePlayTo = true;
        }
    }
}
