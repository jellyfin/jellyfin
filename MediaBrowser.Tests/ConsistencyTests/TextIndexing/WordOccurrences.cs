using System.Collections.Generic;

namespace MediaBrowser.Tests.ConsistencyTests.TextIndexing
{
    public class WordOccurrences : List<WordOccurrence>
    {
        public void AddOccurrence(string fileName, string fullPath, int lineNumber, int wordIndex)
        {
            this.Add(new WordOccurrence(fileName, fullPath, lineNumber, wordIndex));
        }

    }
}
