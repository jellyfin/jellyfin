#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using MediaBrowser.Controller.Chapters;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Chapters
{
    public class ChapterManager : IChapterManager
    {
        private readonly IItemRepository _itemRepo;

        public ChapterManager(IItemRepository itemRepo)
        {
            _itemRepo = itemRepo;
        }

        /// <inheritdoc />
        public void SaveChapters(Guid itemId, IReadOnlyList<ChapterInfo> chapters)
        {
            _itemRepo.SaveChapters(itemId, chapters);
        }

        /// <summary>
        /// Reads chapter information from external XML file.
        /// </summary>
        /// <param name="video">The video item.</param>
        /// <param name="chapters">The set of chapters.</param>
        /// <returns>A list of ChapterInfo objects created using data read from the local XML file.</returns>
        public ChapterInfo[] AddExternalChapters(Video video, IReadOnlyList<ChapterInfo> chapters)
        {
            string? xmlFilePath = null;

            // Look for the XML Chapter file.
            string[] matchingFiles = Directory.GetFiles(video.ContainingFolderPath, "*chapters.xml");
            xmlFilePath = matchingFiles.FirstOrDefault(f => f.EndsWith("chapters.xml", StringComparison.OrdinalIgnoreCase));

            // Use embedded chapters if local chapter file doesn't exist
            if (!File.Exists(xmlFilePath))
            {
                return (ChapterInfo[])chapters;
            }

            var chapterInfos = new List<ChapterInfo>();

            // Load and parse the XML file
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);

            // Navigate to the "chapters" node
            XmlNodeList chapterNodes = xmlDoc.GetElementsByTagName("chapter");

            foreach (XmlNode chapterNode in chapterNodes)
            {
                // Parse the "time" attribute as the chapter start time
                var startTime = chapterNode.Attributes?["time"]?.InnerText;
                // Parse the "name" attribute as the chapter name
                var name = chapterNode.Attributes?["name"]?.InnerText;

                // Create and populate a ChapterInfo object
                var chapterInfo = new ChapterInfo();

                if (startTime != null && TimeSpan.TryParse(startTime, out var timeSpan))
                {
                    chapterInfo.StartPositionTicks = timeSpan.Ticks;
                }

                chapterInfo.Name = name;

                chapterInfos.Add(chapterInfo);
            }

            return chapterInfos.ToArray();
        }
    }
}
