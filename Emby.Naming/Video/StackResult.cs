#pragma warning disable CS1591
#pragma warning disable SA1600

using System.Collections.Generic;

namespace Emby.Naming.Video
{
    public class StackResult
    {
        public List<FileStack> Stacks { get; set; }

        public StackResult()
        {
            Stacks = new List<FileStack>();
        }
    }
}
