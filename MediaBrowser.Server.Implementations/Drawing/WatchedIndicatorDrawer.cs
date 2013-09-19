using System.Drawing;

namespace MediaBrowser.Server.Implementations.Drawing
{
    public class WatchedIndicatorDrawer
    {
        private const int IndicatorHeight = 50;
        private const int FontSize = 50;

        public void Process(Graphics graphics, Size imageSize)
        {
            var x = imageSize.Width - IndicatorHeight;

            using (var backdroundBrush = new SolidBrush(Color.FromArgb(225, 204, 51, 51)))
            {
                graphics.FillRectangle(backdroundBrush, x, 0, IndicatorHeight, IndicatorHeight);

                const string text = "a";

                x = imageSize.Width - 55;

                using (var font = new Font("Webdings", FontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    using (var fontBrush = new SolidBrush(Color.White))
                    {
                        graphics.DrawString(text, font, fontBrush, x, -2);
                    }
                }
            }

        }
    }
}
