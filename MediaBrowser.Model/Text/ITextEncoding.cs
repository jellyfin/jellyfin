using System.Text;

namespace MediaBrowser.Model.Text
{
    public interface ITextEncoding
    {
        Encoding GetASCIIEncoding();
        Encoding GetFileEncoding(string path);
    }
}
