#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Dlna
{
    public class FullTranscoderSupport : ITranscoderSupport
    {
        public bool CanEncodeToAudioCodec(string codec)
        {
            return true;
        }

        public bool CanEncodeToSubtitleCodec(string codec)
        {
            return true;
        }

        public bool CanExtractSubtitles(string codec)
        {
            return true;
        }
    }
}
