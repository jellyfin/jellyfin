using System.Collections.Generic;

namespace DvdLib.Ifo
{
    public class ProgramChainCommandTable
    {
        public readonly ushort LastByteAddress;
        public readonly List<VirtualMachineCommand> PreCommands;
        public readonly List<VirtualMachineCommand> PostCommands;
        public readonly List<VirtualMachineCommand> CellCommands;
    }

    public class VirtualMachineCommand
    {
        public readonly byte[] Command;
    }
}
