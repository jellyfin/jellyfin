using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MediaBrowser.MediaInfo
{
    public class MediaInfoLib
    {
        public MediaInfoResult GetVideoInfo(string path)
        {
            var lib = new MediaInfo();

            lib.Open(path);

            var result = new MediaInfoResult();

            // TODO: Don't hardcode
            var videoStreamIndex = 0;

            var text = GetValue(lib, videoStreamIndex, new[] { "ScanType", "Scan type", "ScanType/String" });
            if (!string.IsNullOrWhiteSpace(text))
            {
                result.IsInterlaced = text.IndexOf("interlac", StringComparison.OrdinalIgnoreCase) != -1;
            }

            text = GetValue(lib, videoStreamIndex, new[] { "Format_Settings_CABAC", "Format_Settings_CABAC/String" });
            if (!string.IsNullOrWhiteSpace(text))
            {
                result.IsCabac = string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase);
            }

            int bitDepth;
            text = GetValue(lib, videoStreamIndex, new[] { "BitDepth", "BitDepth/String" });

            if (!string.IsNullOrWhiteSpace(text) && int.TryParse(text.Split(' ').First(), NumberStyles.Any, CultureInfo.InvariantCulture, out bitDepth))
            {
                result.BitDepth = bitDepth;
            }

            int refFrames;
            text = GetValue(lib, videoStreamIndex, new[] { "Format_Settings_RefFrames", "Format_Settings_RefFrames/String" });

            if (!string.IsNullOrWhiteSpace(text) && int.TryParse(text.Split(' ').First(), NumberStyles.Any, CultureInfo.InvariantCulture, out refFrames))
            {
                result.RefFrames = refFrames;
            }

            return result;
        }

        private string GetValue(MediaInfo lib, int index, IEnumerable<string> names)
        {
            return names.Select(i => lib.Get(StreamKind.Video, index, i)).FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));
        }
    }

    public class MediaInfoResult
    {
        public bool? IsCabac { get; set; }
        public bool? IsInterlaced { get; set; }
        public int? BitDepth { get; set; }
        public int? RefFrames { get; set; }
    }
}
