namespace MediaBrowser.Tests.ConsistencyTests.TextIndexing
{
    public struct WordOccurrence
    {
        public readonly string FileName; // file containing the word.
        public readonly string FullPath; // file containing the word.
        public readonly int LineNumber;  // line within the file.
        public readonly int WordIndex;   // index within the line.

        public WordOccurrence(string fileName, string fullPath, int lineNumber, int wordIndex)
        {
            FileName = fileName;
            FullPath = fullPath;
            LineNumber = lineNumber;
            WordIndex = wordIndex;
        }
    }
}
