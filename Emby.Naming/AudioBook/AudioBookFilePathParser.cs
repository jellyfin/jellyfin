using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Emby.Naming.Audio;
using Emby.Naming.Common;

namespace Emby.Naming.AudioBook
{
    /// <summary>
    /// Parser class to extract part and/or chapter number from audiobook filename.
    /// </summary>
    public class AudioBookFilePathParser
    {
        private readonly NamingOptions _options;
        private readonly AlbumParser _albumParser;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBookFilePathParser"/> class.
        /// </summary>
        /// <param name="options">Naming options containing AudioBookPartsExpressions.</param>
        public AudioBookFilePathParser(NamingOptions options)
        {
            _options = options;
            _albumParser = new AlbumParser(options);
        }

        /// <summary>
        /// Based on regex determines if filename includes part/chapter number.
        /// When a part number is not found in the filename, the parent folder name is also
        /// checked so that files inside "Part 1/", "Part 2/" subfolders inherit the part number.
        /// </summary>
        /// <param name="path">Path to audiobook file.</param>
        /// <returns>Returns <see cref="AudioBookFilePathParserResult"/> object.</returns>
        public AudioBookFilePathParserResult Parse(string path)
        {
            AudioBookFilePathParserResult result = default;
            var fileName = Path.GetFileNameWithoutExtension(path);
            foreach (var expression in _options.AudioBookPartsExpressions)
            {
                var match = Regex.Match(fileName, expression, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    if (!result.ChapterNumber.HasValue)
                    {
                        var value = match.Groups["chapter"];
                        if (value.Success)
                        {
                            if (int.TryParse(value.ValueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                            {
                                result.ChapterNumber = intValue;
                            }
                        }
                    }

                    if (!result.PartNumber.HasValue)
                    {
                        var value = match.Groups["part"];
                        if (value.Success)
                        {
                            if (int.TryParse(value.ValueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                            {
                                result.PartNumber = intValue;
                            }
                        }
                    }
                }
            }

            // If the filename itself didn't yield a part number, check whether the immediate
            // parent folder is a named part folder (e.g. "Part 1", "Part 2"). This mirrors how
            // MusicAlbumResolver treats disc subfolders, and allows organizing audiobooks as:
            //   Book/Part 1/Chapter 01.mp3
            //   Book/Part 2/Chapter 01.mp3
            // AlbumParser.IsMultiPart is used as a gate so that only unambiguous part folder
            // names (those starting with a known stacking prefix) contribute a part number,
            // preventing generic trailing-number patterns from misidentifying chapter folders.
            if (!result.PartNumber.HasValue)
            {
                var parentFolderPath = Path.GetDirectoryName(path);
                var parentFolder = Path.GetFileName(parentFolderPath);
                if (!string.IsNullOrEmpty(parentFolder) && !string.IsNullOrEmpty(parentFolderPath)
                    && _albumParser.IsMultiPart(parentFolderPath))
                {
                    foreach (var expression in _options.AudioBookPartsExpressions)
                    {
                        var match = Regex.Match(parentFolder, expression, RegexOptions.IgnoreCase);
                        if (match.Success)
                        {
                            var value = match.Groups["part"];
                            if (value.Success && int.TryParse(value.ValueSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                            {
                                result.PartNumber = intValue;
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}
