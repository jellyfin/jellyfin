using System.IO;
using System.Collections;
using DvdLib.Enums;

namespace DvdLib.Ifo;

/// <summary>
/// A title type.
/// </summary>
public class TitleType
{
    /// <summary>
    /// The first user operation.
    /// </summary>
    public readonly UserOperation Uop0 = UserOperation.None;

    /// <summary>
    /// The second user operation.
    /// </summary>
    public readonly UserOperation Uop1 = UserOperation.None;

    /// <summary>
    /// The jump/link/call commands.
    /// </summary>
    public readonly Command commands = Command.None;

    /// <summary>
    /// A value indicating whether this <see cref="TitleType" /> is sequential.
    /// </summary>
    /// <value><c>true</c> if sequential; otherwise, <c>false</c>.</value>
    public bool Sequential;

    internal TitleType(BinaryReader br)
    {
        var bitArray = new BitArray(new byte[] {br.ReadByte()});
        if (bitArray[0])
        {
            Uop0 = UserOperation.TimePlayOrSearch;
        }

        if (bitArray[1])
        {
            Uop0 = UserOperation.PTTPlayOrSearch;
        }

        if (bitArray[2])
        {
            commands |= Command.Exists;
        }

        if (bitArray[3])
        {
            commands |= Command.Button;
        }

        if (bitArray[4])
        {
            commands |= Command.PrePost;
        }

        if (bitArray[5])
        {
            commands |= Command.Cell;
        }

        Sequential = !bitArray[6];
    }
}
