using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using LrcParser.Model;
using LrcParser.Parser;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Lyrics;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MediaBrowser.Providers.Lyric
{
    /// <summary>
    /// LRC Lyric Provider.
    /// </summary>
    public class LrcLyricProvider : ILyricProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LrcLyricProvider"/> class.
        /// </summary>
        public LrcLyricProvider()
        {
            Name = "LrcLyricProvider";

            SupportedMediaTypes = new Collection<string>
            {
                "lrc"
            };
        }

        /// <summary>
        /// Gets a value indicating the provider name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets a value indicating the File Extenstions this provider supports.
        /// </summary>
        public IEnumerable<string> SupportedMediaTypes { get; }

        /// <summary>
        /// Opens lyric file for the requested item, and processes it for API return.
        /// </summary>
        /// <param name="item">The item to to process.</param>
        /// <returns>If provider can determine lyrics, returns a <see cref="LyricResponse"/> with or without metadata; otherwise, null.</returns>
        public LyricResponse? GetLyrics(BaseItem item)
        {
            string? lyricFilePath = LyricInfo.GetLyricFilePath(this, item.Path);

            if (string.IsNullOrEmpty(lyricFilePath))
            {
                return null;
            }

            List<Controller.Lyrics.Lyric> lyricList = new List<Controller.Lyrics.Lyric>();
            List<LrcParser.Model.Lyric> sortedLyricData = new List<LrcParser.Model.Lyric>();

            IDictionary<string, string> metaData = new Dictionary<string, string>();
            string lrcFileContent = System.IO.File.ReadAllText(lyricFilePath);

            try
            {
                // Parse and sort lyric rows
                LyricParser lrcLyricParser = new LrcParser.Parser.Lrc.LrcParser();
                Song lyricData = lrcLyricParser.Decode(lrcFileContent);
                sortedLyricData = lyricData.Lyrics.Where(x => x.TimeTags.Count > 0).OrderBy(x => x.TimeTags.ToArray()[0].Value).ToList();

                // Parse metadata rows
                var metaDataRows = lyricData.Lyrics
                    .Where(x => x.TimeTags.Count == 0)
                    .Where(x => x.Text.StartsWith("[", StringComparison.Ordinal) && x.Text.EndsWith("]", StringComparison.Ordinal))
                    .Select(x => x.Text)
                    .ToList();

                foreach (string metaDataRow in metaDataRows)
                {
                    var metaDataField = metaDataRow.Split(":");

                    string metaDataFieldName = metaDataField[0].Replace("[", string.Empty, StringComparison.Ordinal).Trim();
                    string metaDataFieldValue = metaDataField[1].Replace("]", string.Empty, StringComparison.Ordinal).Trim();

                    metaData.Add(metaDataFieldName, metaDataFieldValue);
                }
            }
            catch
            {
                return null;
            }

            if (!sortedLyricData.Any())
            {
                return null;
            }

            for (int i = 0; i < sortedLyricData.Count; i++)
            {
                var timeData = sortedLyricData[i].TimeTags.ToArray()[0].Value;
                long ticks = Convert.ToInt64(timeData, new NumberFormatInfo()) * 10000;
                lyricList.Add(new Controller.Lyrics.Lyric { Start = ticks, Text = sortedLyricData[i].Text });
            }

            if (metaData.Any())
            {
               return new LyricResponse { Metadata = metaData, Lyrics = lyricList };
            }

            return new LyricResponse { Lyrics = lyricList };
        }
    }
}
