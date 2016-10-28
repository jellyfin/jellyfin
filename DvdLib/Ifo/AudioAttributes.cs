using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DvdLib.Ifo
{
    public enum AudioCodec
    {
        AC3 = 0,
        MPEG1 = 2,
        MPEG2ext = 3,
        LPCM = 4,
        DTS = 6,
    }

    public enum ApplicationMode
    {
        Unspecified = 0,
        Karaoke = 1,
        Surround = 2,
    }

    public class AudioAttributes
    {
        public readonly AudioCodec Codec;
        public readonly bool MultichannelExtensionPresent;
        public readonly ApplicationMode Mode;
        public readonly byte QuantDRC;
        public readonly byte SampleRate;
        public readonly byte Channels;
        public readonly ushort LanguageCode;
        public readonly byte LanguageExtension;
        public readonly byte CodeExtension;
    }

    public class MultiChannelExtension
    {

    }
}
