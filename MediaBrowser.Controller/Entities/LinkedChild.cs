using System;
using System.Collections;

namespace MediaBrowser.Controller.Entities
{
    public class LinkedChild
    {
        public string Path { get; set; }
        public LinkedChildType Type { get; set; }
    }

    public enum LinkedChildType
    {
        Manual = 1,
        Shortcut = 2
    }

    public class LinkedChildComparer : IComparer
    {
        public int Compare(object x, object y)
        {
            var a = (LinkedChild)x;

            var b = (LinkedChild)y;

            if (!string.Equals(a.Path, b.Path, StringComparison.OrdinalIgnoreCase))
            {
                return string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
            }
            if (a.Type != b.Type)
            {
                return a.Type.CompareTo(b.Type);
            }

            return 0;
        }
    }
}
