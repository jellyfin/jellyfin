using System.Drawing;
using System.Globalization;

namespace MediaBrowser.Server.Implementations.Drawing
{
    public class PercentPlayedDrawer
    {
        private const int IndicatorWidth = 80;
        private const int IndicatorHeight = 50;
        private const int FontSize = 30;
        private readonly CultureInfo _usCulture = new CultureInfo("en-US");

        public void Process(Graphics graphics, Size imageSize, int percent)
        {
            var x = imageSize.Width - IndicatorWidth;

            using (var backdroundBrush = new SolidBrush(Color.FromArgb(225, 102, 192, 16)))
            {
                graphics.FillRectangle(backdroundBrush, x, 0, IndicatorWidth, IndicatorHeight);

                var text = string.Format("{0}%", percent.ToString(_usCulture));

                x = imageSize.Width - (percent < 10 ? 66 : 75);

                using (var font = new Font(FontFamily.GenericSansSerif, FontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    using (var fontBrush = new SolidBrush(Color.White))
                    {
                        graphics.DrawString(text, font, fontBrush, x, 6);
                    }
                }
            }

        }
    }
}
