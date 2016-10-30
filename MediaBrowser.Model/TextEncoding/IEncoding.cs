
namespace MediaBrowser.Model.TextEncoding
{
    public interface IEncoding
    {
        byte[] GetASCIIBytes(string text);
        string GetASCIIString(byte[] bytes, int startIndex, int length);
    }
}
