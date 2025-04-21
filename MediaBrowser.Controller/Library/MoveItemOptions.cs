#pragma warning disable CS1591

namespace MediaBrowser.Controller.Library
{
    public class MoveItemOptions
    {
        public MoveItemOptions()
        {
            MoveOnFileSystem = true;
        }

        public bool MoveOnFileSystem { get; set; }
    }
}
