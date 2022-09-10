using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Api.Models.UserDtos;
using Kfstorm.LrcParser;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
        internal static List<Lyrics> GetLyricData(BaseItem item)
        {
            List<Lyrics> lyricsList = new List<Lyrics>();

            string lrcFilePath = @Path.ChangeExtension(item.Path, "lrc");

            // LRC File not found, fallback to TXT file
            if (!System.IO.File.Exists(lrcFilePath))
            {
                string txtFilePath = @Path.ChangeExtension(item.Path, "txt");
                if (!System.IO.File.Exists(txtFilePath))
                {
                    lyricsList.Add(new Lyrics { Error = "Lyric File Not Found" });
                    return lyricsList;
                }

                var lyricTextData = System.IO.File.ReadAllText(txtFilePath);
                string[] lyricTextLines = lyricTextData.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

                foreach (var lyricLine in lyricTextLines)
                {
                    lyricsList.Add(new Lyrics { Text = lyricLine });
                }

                return lyricsList;
            }

            // Process LRC File
            ILrcFile lyricData;
            string lrcFileContent = System.IO.File.ReadAllText(lrcFilePath);
            try
            {
                lrcFileContent = lrcFileContent.Replace('<', '[');
                lrcFileContent = lrcFileContent.Replace('>', ']');
                lyricData = Kfstorm.LrcParser.LrcFile.FromText(lrcFileContent);
            }
            catch
            {
                lyricsList.Add(new Lyrics { Error = "No Lyrics Data" });
                return lyricsList;
            }

            if (lyricData == null)
            {
                lyricsList.Add(new Lyrics { Error = "No Lyrics Data" });
                return lyricsList;
            }

            foreach (var lyricLine in lyricData.Lyrics)
            {
                double ticks = lyricLine.Timestamp.TotalSeconds * 10000000;
                lyricsList.Add(new Lyrics { Start = Math.Ceiling(ticks), Text = lyricLine.Content });
            }

            return lyricsList;
        }
    }
}
