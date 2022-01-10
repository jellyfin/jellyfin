#pragma warning disable CS1591

using System.Collections.Generic;

namespace Emby.Server.Implementations.LiveTv.TunerHosts.HdHomerun
{
    public interface IHdHomerunChannelCommands
    {
        IEnumerable<(string CommandName, string CommandValue)> GetCommands();
    }
}
