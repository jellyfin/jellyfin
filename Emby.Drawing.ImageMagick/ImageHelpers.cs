using System;
using System.Collections.Generic;
using System.Linq;

namespace Emby.Drawing.ImageMagick
{
    internal static class ImageHelpers
    {
        internal static List<string> ProjectPaths(string[] paths, int count)
        {
            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (paths.Length == 0)
            {
                throw new ArgumentOutOfRangeException("paths");
            }

            var list = new List<string>();

            AddToList(list, paths, count);

            return list.Take(count).ToList();
        }

        private static void AddToList(List<string> list, string[] paths, int count)
        {
            while (list.Count < count)
            {
                foreach (var path in paths)
                {
                    list.Add(path);

                    if (list.Count >= count)
                    {
                        return;
                    }
                }
            }
        }
    }
}
