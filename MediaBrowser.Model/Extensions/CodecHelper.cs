using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Model.Extensions
{
    public static class CodecHelper
    {
        public static string FriendlyName(string codec)
        {
            if (string.IsNullOrEmpty(codec)) return "";

            switch (codec.ToLower())
            {
                case "ac3":
                    return "Dolby Digital";
                case "eac3":
                    return "Dolby Digital+";
                case "dca":
                    return "DTS";
                default:
                    return codec.ToUpper();
            }
        }
    }
}
