#pragma warning disable CS1591

using System.IO;

namespace MediaBrowser.Model.IO
{
    /// <summary>
    /// Interface IZipClient.
    /// </summary>
    public interface IZipClient
    {
        void ExtractAllFromGz(Stream source, string targetPath, bool overwriteExistingFiles);

        void ExtractFirstFileFromGz(Stream source, string targetPath, string defaultFileName);
    }
}
