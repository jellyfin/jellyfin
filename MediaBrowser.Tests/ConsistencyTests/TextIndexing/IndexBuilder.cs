using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Tests.ConsistencyTests.TextIndexing
{
    public class IndexBuilder
    {
        public const int MinumumWordLength = 4;

        public static char[] WordChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

        public static WordIndex BuildIndexFromFiles(IEnumerable<FileInfo> wordFiles, string rootFolderPath)
        {
            var index = new WordIndex();

            var wordSeparators = Enumerable.Range(32, 127).Select(e => Convert.ToChar(e)).Where(c => !WordChars.Contains(c)).ToArray();
            wordSeparators = wordSeparators.Concat(new[] { '\t' }).ToArray(); // add tab

            foreach (var file in wordFiles)
            {
                var lineNumber = 1;
                var displayFileName = file.FullName.Replace(rootFolderPath, string.Empty);
                using (var reader = file.OpenText())
                {
                    while (!reader.EndOfStream)
                    {
                        var words = reader
                             .ReadLine()
                             .Split(wordSeparators, StringSplitOptions.RemoveEmptyEntries);
                        ////.Select(f => f.Trim());

                        var wordIndex = 1;
                        foreach (var word in words)
                        {
                            if (word.Length >= MinumumWordLength)
                            {
                                index.AddWordOccurrence(word, displayFileName, file.FullName, lineNumber, wordIndex++);
                            }
                        }

                        lineNumber++;
                    }
                }
            }

            return index;
        }

    }
}
