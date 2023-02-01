using System;

namespace DvdLib.Ifo;

/// <summary>
/// A classs holding information on DVD runtime and framerate.
/// </summary>
public class DvdTime
{
    /// <summary>
    /// The hours.
    /// </summary>
    public readonly byte Hours;

    /// <summary>
    /// The minutes.
    /// </summary>
    public readonly byte Minutes;

    /// <summary>
    /// The seconds.
    /// </summary>
    public readonly byte Seconds;

    /// <summary>
    /// The total amount of frames.
    /// </summary>
    public readonly byte Frames;

    /// <summary>
    /// The frame rate.
    /// </summary>
    public readonly byte FrameRate;

    /// <summary>
    /// Initializes a new instance of the <see cref="DvdTime"/> class.
    /// </summary>
    /// <param name="data">The binary data.</param>
    /// <returns>The <see cref="DvdTime"/>.</returns>
    public DvdTime(byte[] data)
    {
        Hours = GetBCDValue(data[0]);
        Minutes = GetBCDValue(data[1]);
        Seconds = GetBCDValue(data[2]);
        Frames = GetBCDValue((byte)(data[3] & 0x3F));

        if ((data[3] & 0x80) != 0)
        {
            FrameRate = 30;
        }
        else if ((data[3] & 0x40) != 0)
        {
            FrameRate = 25;
        }
    }

    private static byte GetBCDValue(byte data)
    {
        return (byte)((((data & 0xF0) >> 4) * 10) + (data & 0x0F));
    }

    /// <summary>
    /// Converts <see cref="DvdTime"/> to <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="time">The <see cref="DvdTime"/>.</param>
    /// <returns>The <see cref="TimeSpan"/>.</returns>
    public static explicit operator TimeSpan(DvdTime time)
    {
        int ms = (int)(((1.0 / (double)time.FrameRate) * time.Frames) * 1000.0);
        return new TimeSpan(0, time.Hours, time.Minutes, time.Seconds, ms);
    }
}
