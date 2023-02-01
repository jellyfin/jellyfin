namespace DvdLib.Ifo;

/// <summary>
/// A chapter.
/// </summary>
public class Chapter
{
    /// <summary>
    /// The program chain number.
    /// </summary>
    public ushort ProgramChainNumber { get; private set; }

    /// <summary>
    /// The program number.
    /// </summary>
    public ushort ProgramNumber { get; private set; }

    /// <summary>
    /// The chapter number.
    /// </summary>
    public uint ChapterNumber { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Chapter"/> class.
    /// </summary>
    /// <param name="pgcNum">The program chain number.</param>
    /// <param name="programNum">The program number.</param>
    /// <param name="chapterNum">The chapter number.</param>
    /// <returns>The <see cref="Chapter"/>.</returns>
    public Chapter(ushort pgcNum, ushort programNum, uint chapterNum)
    {
        ProgramChainNumber = pgcNum;
        ProgramNumber = programNum;
        ChapterNumber = chapterNum;
    }
}
