using System.Drawing;

namespace MediaBrowser.Server.Implementations.Drawing
{
    public class PlayedIndicatorDrawer
    {
        private const int IndicatorHeight = 50;
        public const int IndicatorWidth = 50;
        private const int FontSize = 50;
        private const int OffsetFromTopRightCorner = 10;

        public void DrawPlayedIndicator(Graphics graphics, Size imageSize)
        {
            var x = imageSize.Width - IndicatorWidth - OffsetFromTopRightCorner;

            using (var backdroundBrush = new SolidBrush(Color.FromArgb(225, 82, 181, 75)))
            {
                graphics.FillEllipse(backdroundBrush, x, OffsetFromTopRightCorner, IndicatorWidth, IndicatorHeight);

                x = imageSize.Width - 55 - OffsetFromTopRightCorner;

                using (var font = new Font("Webdings", FontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    using (var fontBrush = new SolidBrush(Color.White))
                    {
                        graphics.DrawString("a", font, fontBrush, x, OffsetFromTopRightCorner - 2);
                    }
                }
            }
        }
    }
}
