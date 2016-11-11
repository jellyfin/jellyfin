using System;
using System.Drawing;

namespace Emby.Drawing.Net
{
    public class PercentPlayedDrawer
    {
        private const int IndicatorHeight = 8;

        public void Process(Graphics graphics, Size imageSize, double percent)
        {
            var y = imageSize.Height - IndicatorHeight;

            using (var backdroundBrush = new SolidBrush(Color.FromArgb(225, 0, 0, 0)))
            {
                const int innerX = 0;
                var innerY = y;
                var innerWidth = imageSize.Width;
                var innerHeight = imageSize.Height;

                graphics.FillRectangle(backdroundBrush, innerX, innerY, innerWidth, innerHeight);

                using (var foregroundBrush = new SolidBrush(Color.FromArgb(82, 181, 75)))
                {
                    double foregroundWidth = innerWidth;
                    foregroundWidth *= percent;
                    foregroundWidth /= 100;

                    graphics.FillRectangle(foregroundBrush, innerX, innerY, Convert.ToInt32(Math.Round(foregroundWidth)), innerHeight);
                }
            }
        }
    }
}
