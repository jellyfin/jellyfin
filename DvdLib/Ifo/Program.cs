using System.Collections.Generic;

namespace DvdLib.Ifo;

/// <summary>
/// A program.
/// </summary>
public class Program
{
    /// <summary>
    /// The cells.
    /// </summary>
    public IReadOnlyList<Cell> Cells { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Program"/> class.
    /// </summary>
    /// <param name="cells">The list of cells.</param>
    /// <returns>The <see cref="Title"/>.</returns>
    public Program(List<Cell> cells)
    {
        Cells = cells;
    }
}
