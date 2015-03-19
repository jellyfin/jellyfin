
namespace MediaBrowser.Model.Dlna
{
    public class NullLocalPlayer : ILocalPlayer
    {
        public bool CanAccessFile(string path)
        {
            return false;
        }

        public bool CanAccessDirectory(string path)
        {
            return false;
        }

        public bool CanAccessUrl(string url, bool requiresCustomRequestHeaders)
        {
            return false;
        }
    }
}
