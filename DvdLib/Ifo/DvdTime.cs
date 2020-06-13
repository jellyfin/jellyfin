#pragma warning disable CS1591

using System;

namespace DvdLib.Ifo
{
    public class DvdTime
    {
        public readonly byte Hour, Minute, Second, Frames, FrameRate;

        public DvdTime(byte[] data)
        {
            Hour = GetBCDValue(data[0]);
            Minute = GetBCDValue(data[1]);
            Second = GetBCDValue(data[2]);
            Frames = GetBCDValue((byte)(data[3] & 0x3F));

            if ((data[3] & 0x80) != 0) FrameRate = 30;
            else if ((data[3] & 0x40) != 0) FrameRate = 25;
        }

        private static byte GetBCDValue(byte data)
        {
            return (byte)((((data & 0xF0) >> 4) * 10) + (data & 0x0F));
        }

        public static explicit operator TimeSpan(DvdTime time)
        {
            int ms = (int)(((1.0 / (double)time.FrameRate) * time.Frames) * 1000.0);
            return new TimeSpan(0, time.Hour, time.Minute, time.Second, ms);
        }
    }
}
