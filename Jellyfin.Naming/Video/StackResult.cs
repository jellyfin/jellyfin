using System.Collections.Generic;

namespace Jellyfin.Naming.Video
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
