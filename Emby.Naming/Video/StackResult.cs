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
