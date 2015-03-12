using System.IO;

namespace MediaBrowser.Model.Dlna
{
    public class DefaultLocalPlayer : ILocalPlayer
    {
        public bool CanAccessFile(string path)
        {
            return File.Exists(path);
        }

        public bool CanAccessDirectory(string path)
        {
            return Directory.Exists(path);
        }

        public virtual bool CanAccessUrl(string url, bool requiresCustomRequestHeaders)
        {
            return false;
        }
    }
}
