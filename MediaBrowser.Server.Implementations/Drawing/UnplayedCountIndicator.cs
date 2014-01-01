using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Drawing
{
    public class UnplayedCountIndicator
    {
        private const int IndicatorHeight = 50;
        public const int IndicatorWidth = 50;
        private const int OffsetFromTopRightCorner = 10;

        public void DrawUnplayedCountIndicator(Graphics graphics, Size imageSize, int count)
        {
            var x = imageSize.Width - IndicatorWidth - OffsetFromTopRightCorner;

            using (var backdroundBrush = new SolidBrush(Color.FromArgb(225, 82, 181, 75)))
            {
                graphics.FillEllipse(backdroundBrush, x, OffsetFromTopRightCorner, IndicatorWidth, IndicatorHeight);

                var text = count.ToString();

                x = imageSize.Width - 50 - OffsetFromTopRightCorner;
                var y = OffsetFromTopRightCorner + 7;
                var fontSize = 30;

                if (text.Length == 1)
                {
                    x += 11;
                }
                else if (text.Length == 2)
                {
                    x += 3;
                }
                else if (text.Length == 3)
                {
                    //x += 1;
                    y += 3;
                    fontSize = 24;
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
