using System;
using System.IO;
using System.Threading;

namespace MediaBrowser.MediaEncoding.Subtitles
{
    public class SrtWriter : ISubtitleWriter
    {
        public void Write(SubtitleTrackInfo info, Stream stream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
