namespace Jellyfin.Drawing.Skia
{
    /// <summary>
    /// Used to provide utility functions to the project.
    /// </summary>
    public static class Utils {
        /// <summary>
        /// Checks if the string contains a RTL character. Copied from https://stackoverflow.com/a/4331154
        /// </summary>
        /// <param name="text">The string to check for RTL characters.</param>
        public static bool HasRTLCharacters(string? text)
        {
            if (text == null)
                return false;
            bool hasRandALCat = false;
            for (var i = 0; i < text.Length; i += char.IsSurrogatePair(text, i) ? 2 : 1) {
                var c = char.ConvertToUtf32(text, i);
                if(c >= 0x5BE && c <= 0x10B7F)
                {
                    if(c <= 0x85E)
                    {
                        if(c == 0x5BE)                        hasRandALCat = true;
                        else if(c == 0x5C0)                   hasRandALCat = true;
                        else if(c == 0x5C3)                   hasRandALCat = true;
                        else if(c == 0x5C6)                   hasRandALCat = true;
                        else if(0x5D0 <= c && c <= 0x5EA)     hasRandALCat = true;
                        else if(0x5F0 <= c && c <= 0x5F4)     hasRandALCat = true;
                        else if(c == 0x608)                   hasRandALCat = true;
                        else if(c == 0x60B)                   hasRandALCat = true;
                        else if(c == 0x60D)                   hasRandALCat = true;
                        else if(c == 0x61B)                   hasRandALCat = true;
                        else if(0x61E <= c && c <= 0x64A)     hasRandALCat = true;
                        else if(0x66D <= c && c <= 0x66F)     hasRandALCat = true;
                        else if(0x671 <= c && c <= 0x6D5)     hasRandALCat = true;
                        else if(0x6E5 <= c && c <= 0x6E6)     hasRandALCat = true;
                        else if(0x6EE <= c && c <= 0x6EF)     hasRandALCat = true;
                        else if(0x6FA <= c && c <= 0x70D)     hasRandALCat = true;
                        else if(c == 0x710)                   hasRandALCat = true;
                        else if(0x712 <= c && c <= 0x72F)     hasRandALCat = true;
                        else if(0x74D <= c && c <= 0x7A5)     hasRandALCat = true;
                        else if(c == 0x7B1)                   hasRandALCat = true;
                        else if(0x7C0 <= c && c <= 0x7EA)     hasRandALCat = true;
                        else if(0x7F4 <= c && c <= 0x7F5)     hasRandALCat = true;
                        else if(c == 0x7FA)                   hasRandALCat = true;
                        else if(0x800 <= c && c <= 0x815)     hasRandALCat = true;
                        else if(c == 0x81A)                   hasRandALCat = true;
                        else if(c == 0x824)                   hasRandALCat = true;
                        else if(c == 0x828)                   hasRandALCat = true;
                        else if(0x830 <= c && c <= 0x83E)     hasRandALCat = true;
                        else if(0x840 <= c && c <= 0x858)     hasRandALCat = true;
                        else if(c == 0x85E)                   hasRandALCat = true;
                    }
                    else if(c == 0x200F)                      hasRandALCat = true;
                    else if(c >= 0xFB1D)
                    {
                        if(c == 0xFB1D)                       hasRandALCat = true;
                        else if(0xFB1F <= c && c <= 0xFB28)   hasRandALCat = true;
                        else if(0xFB2A <= c && c <= 0xFB36)   hasRandALCat = true;
                        else if(0xFB38 <= c && c <= 0xFB3C)   hasRandALCat = true;
                        else if(c == 0xFB3E)                  hasRandALCat = true;
                        else if(0xFB40 <= c && c <= 0xFB41)   hasRandALCat = true;
                        else if(0xFB43 <= c && c <= 0xFB44)   hasRandALCat = true;
                        else if(0xFB46 <= c && c <= 0xFBC1)   hasRandALCat = true;
                        else if(0xFBD3 <= c && c <= 0xFD3D)   hasRandALCat = true;
                        else if(0xFD50 <= c && c <= 0xFD8F)   hasRandALCat = true;
                        else if(0xFD92 <= c && c <= 0xFDC7)   hasRandALCat = true;
                        else if(0xFDF0 <= c && c <= 0xFDFC)   hasRandALCat = true;
                        else if(0xFE70 <= c && c <= 0xFE74)   hasRandALCat = true;
                        else if(0xFE76 <= c && c <= 0xFEFC)   hasRandALCat = true;
                        else if(0x10800 <= c && c <= 0x10805) hasRandALCat = true;
                        else if(c == 0x10808)                 hasRandALCat = true;
                        else if(0x1080A <= c && c <= 0x10835) hasRandALCat = true;
                        else if(0x10837 <= c && c <= 0x10838) hasRandALCat = true;
                        else if(c == 0x1083C)                 hasRandALCat = true;
                        else if(0x1083F <= c && c <= 0x10855) hasRandALCat = true;
                        else if(0x10857 <= c && c <= 0x1085F) hasRandALCat = true;
                        else if(0x10900 <= c && c <= 0x1091B) hasRandALCat = true;
                        else if(0x10920 <= c && c <= 0x10939) hasRandALCat = true;
                        else if(c == 0x1093F)                 hasRandALCat = true;
                        else if(c == 0x10A00)                 hasRandALCat = true;
                        else if(0x10A10 <= c && c <= 0x10A13) hasRandALCat = true;
                        else if(0x10A15 <= c && c <= 0x10A17) hasRandALCat = true;
                        else if(0x10A19 <= c && c <= 0x10A33) hasRandALCat = true;
                        else if(0x10A40 <= c && c <= 0x10A47) hasRandALCat = true;
                        else if(0x10A50 <= c && c <= 0x10A58) hasRandALCat = true;
                        else if(0x10A60 <= c && c <= 0x10A7F) hasRandALCat = true;
                        else if(0x10B00 <= c && c <= 0x10B35) hasRandALCat = true;
                        else if(0x10B40 <= c && c <= 0x10B55) hasRandALCat = true;
                        else if(0x10B58 <= c && c <= 0x10B72) hasRandALCat = true;
                        else if(0x10B78 <= c && c <= 0x10B7F) hasRandALCat = true;
                    }
                }
                if (hasRandALCat)
                    break;
            }
            return hasRandALCat;
        }
    }
}
