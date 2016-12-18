using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DvdLib.Ifo
{
    public enum VideoCodec
    {
        MPEG1 = 0,
        MPEG2 = 1,
    }

    public enum VideoFormat
    {
        NTSC = 0,
        PAL = 1,
    }

    public enum AspectRatio
    {
        ar4to3 = 0,
        ar16to9 = 3
    }

    public enum FilmMode
    {
        None = -1,
        Camera = 0,
        Film = 1,
    }

    public class VideoAttributes
    {
        public readonly VideoCodec Codec;
        public readonly VideoFormat Format;
        public readonly AspectRatio Aspect;
        public readonly bool AutomaticPanScan;
        public readonly bool AutomaticLetterBox;
        public readonly bool Line21CCField1;
        public readonly bool Line21CCField2;
        public readonly int Width;
        public readonly int Height;
        public readonly bool Letterboxed;
        public readonly FilmMode FilmMode;

        public VideoAttributes()
        {
        }
    }
}
