using System.IO;

namespace DvdLib.Ifo
{
    /// <summary>
    /// Builder class to make sure ProgramChain is build correctly,
    /// since we are using late initialization in ParseHeader method for several properties
    /// </summary>
    public class ProgramChainBuilder
    {
        private class ConstructableProgramChain : ProgramChain
        {
            public ConstructableProgramChain(uint vtsPgcNum, BinaryReader br) : base(vtsPgcNum)
            {
                ParseHeader(br);
            }
        }
        internal ProgramChain Build(uint vtsPgcNum, BinaryReader br)
        {
            var pc = new ConstructableProgramChain(vtsPgcNum, br);
            return pc;
        }
    }
}
