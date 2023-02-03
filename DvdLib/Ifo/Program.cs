#pragma warning disable CS1591

using System.Collections.Generic;

namespace DvdLib.Ifo
{
    public class Program
    {
        public IReadOnlyList<Cell> Cells { get; }

        public Program(List<Cell> cells)
        {
            Cells = cells;
        }
    }
}
