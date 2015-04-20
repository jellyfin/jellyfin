using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.Dto
{
    public static class ItemLayout
    {
        public static double? GetDisplayAspectRatio(BaseItemDto item)
        {
            List<BaseItemDto> items = new List<BaseItemDto>();
            items.Add(item);
            return GetDisplayAspectRatio(items);
        }

        public static double? GetDisplayAspectRatio(List<BaseItemDto> items)
        {
            List<double> values = new List<double>();

            foreach (BaseItemDto item in items)
            {
                if (item.PrimaryImageAspectRatio.HasValue)
                {
                    values.Add(item.PrimaryImageAspectRatio.Value);
                }
            }

            if (values.Count == 0)
            {
                return null;
            }

            values.Sort();

            double halfDouble = values.Count;
            halfDouble /= 2;
            int half = Convert.ToInt32(Math.Floor(halfDouble));

            double result;

            if (values.Count % 2 > 0)
                result = values[half];
            else
                result = (values[half - 1] + values[half]) / 2.0;

            // If really close to 2:3 (poster image), just return 2:3
            if (Math.Abs(0.66666666667 - result) <= .15) 
            {
                return 0.66666666667;
            }

            // If really close to 16:9 (episode image), just return 16:9
            if (Math.Abs(1.777777778 - result) <= .2)
            {
                return 1.777777778;
            }

            // If really close to 1 (square image), just return 1
            if (Math.Abs(1 - result) <= .15)
            {
                return 1.0;
            }

            // If really close to 4:3 (poster image), just return 2:3
            if (Math.Abs(1.33333333333 - result) <= .15)
            {
                return 1.33333333333;
            }

            return result;
        }
    }
}
