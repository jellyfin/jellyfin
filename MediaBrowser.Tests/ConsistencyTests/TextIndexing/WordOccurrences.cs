using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
