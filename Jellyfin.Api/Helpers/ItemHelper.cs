using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Api.Models.UserDtos;
using LrcParser.Model;
using LrcParser.Parser;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Item helper.
    /// </summary>
    public static class ItemHelper
    {
        /// <summary>
        /// Opens lyrics file, converts to a List of Lyrics, and returns it.
        /// </summary>
        /// <param name="item">Requested Item.</param>
        /// <returns>Collection of Lyrics.</returns>
        internal static object? GetLyricData(BaseItem item)
        {
            List<Lyrics> lyricsList = new List<Lyrics>();

            string lrcFilePath = @Path.ChangeExtension(item.Path, "lrc");

            // LRC File not found, fallback to TXT file
            if (!System.IO.File.Exists(lrcFilePath))
            {
                string txtFilePath = @Path.ChangeExtension(item.Path, "txt");
                if (!System.IO.File.Exists(txtFilePath))
                {
                    return null;
                }

                var lyricTextData = System.IO.File.ReadAllText(txtFilePath);
                string[] lyricTextLines = lyricTextData.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

                foreach (var lyricLine in lyricTextLines)
                {
                    lyricsList.Add(new Lyrics { Text = lyricLine });
                }

                return new { lyrics = lyricsList };
            }

            // Process LRC File
            Song lyricData;
            List<Lyric> sortedLyricData = new List<Lyric>();
            var metaData = new ExpandoObject() as IDictionary<string, object>;
            string lrcFileContent = System.IO.File.ReadAllText(lrcFilePath);

            try
            {
                LyricParser lrcLyricParser = new LrcParser.Parser.Lrc.LrcParser();
                lyricData = lrcLyricParser.Decode(lrcFileContent);
                var _metaData = lyricData.Lyrics
                    .Where(x => x.TimeTags.Count == 0)
                    .Where(x => x.Text.StartsWith("[", StringComparison.Ordinal) && x.Text.EndsWith("]", StringComparison.Ordinal))
                    .Select(x => x.Text)
                    .ToList();

                foreach (string dataRow in _metaData)
                {
                    var data = dataRow.Split(":");

                    string newPropertyName = data[0].Replace("[", string.Empty, StringComparison.Ordinal);
                    string newPropertyValue = data[1].Replace("]", string.Empty, StringComparison.Ordinal);

                    metaData.Add(newPropertyName, newPropertyValue);
                }

                sortedLyricData = lyricData.Lyrics.Where(x => x.TimeTags.Count > 0).OrderBy(x => x.TimeTags.ToArray()[0].Value).ToList();
            }
            catch
            {
                return null;
            }

            if (lyricData == null)
            {
                return null;
            }

            for (int i = 0; i < sortedLyricData.Count; i++)
            {
                if (sortedLyricData[i].TimeTags.Count > 0)
                {
                    var timeData = sortedLyricData[i].TimeTags.ToArray()[0].Value;
                    double ticks = Convert.ToDouble(timeData, new NumberFormatInfo()) * 10000;
                    lyricsList.Add(new Lyrics { Start = Math.Ceiling(ticks), Text = sortedLyricData[i].Text });
                }
            }

            return new { MetaData = metaData, lyrics = lyricsList };
        }
    }
}
