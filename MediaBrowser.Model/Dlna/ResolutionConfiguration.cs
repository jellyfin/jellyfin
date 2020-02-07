#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Dlna
{
    public class ResolutionConfiguration
    {
        public int MaxWidth { get; set; }
        public int MaxBitrate { get; set; }

        public ResolutionConfiguration(int maxWidth, int maxBitrate)
        {
            MaxWidth = maxWidth;
            MaxBitrate = maxBitrate;
        }
    }
}
