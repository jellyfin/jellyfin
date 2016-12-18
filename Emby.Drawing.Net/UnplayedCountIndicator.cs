using System.Drawing;

namespace Emby.Drawing.Net
{
    public class UnplayedCountIndicator
    {
        private const int IndicatorHeight = 41;
        public const int IndicatorWidth = 41;
        private const int OffsetFromTopRightCorner = 10;

        public void DrawUnplayedCountIndicator(Graphics graphics, Size imageSize, int count)
        {
            var x = imageSize.Width - IndicatorWidth - OffsetFromTopRightCorner;

            using (var backdroundBrush = new SolidBrush(Color.FromArgb(225, 82, 181, 75)))
            {
                graphics.FillEllipse(backdroundBrush, x, OffsetFromTopRightCorner, IndicatorWidth, IndicatorHeight);

                var text = count.ToString();

                x = imageSize.Width - IndicatorWidth - OffsetFromTopRightCorner;
                var y = OffsetFromTopRightCorner + 6;
                var fontSize = 24;

                if (text.Length == 1)
                {
                    x += 10;
                }
                else if (text.Length == 2)
                {
                    x += 3;
                }
                else if (text.Length == 3)
                {
                    x += 1;
                    y += 1;
                    fontSize = 20;
                }

                using (var font = new Font("Sans-Serif", fontSize, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    using (var fontBrush = new SolidBrush(Color.White))
                    {
                        graphics.DrawString(text, font, fontBrush, x, y);
                    }
                }
            }
        }
    }
}
