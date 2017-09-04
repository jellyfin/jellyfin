using System.IO;
using System.Text;

namespace MediaBrowser.Model.Text
{
    public interface ITextEncoding
    {
        Encoding GetASCIIEncoding();

        string GetDetectedEncodingName(byte[] bytes, int size, string language, bool enableLanguageDetection);
        Encoding GetDetectedEncoding(byte[] bytes, int size, string language, bool enableLanguageDetection);
        Encoding GetEncodingFromCharset(string charset);
    }
}
