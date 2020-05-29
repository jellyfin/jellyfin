#pragma warning disable CS1591

using System.Collections.Generic;

namespace DvdLib.Ifo
{
    public class Program
    {
        public readonly List<Cell> Cells;

        public Program(List<Cell> cells)
        {
            Cells = cells;
        }
    }
}
