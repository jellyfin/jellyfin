#pragma warning disable CS1591

namespace MediaBrowser.Controller.Library
{
    public class MoveItemOptions
    {
        public MoveItemOptions()
        {
            MoveOnFileSystem = true;
            UpdatePathInDb = true;
            Overwrite = false;
        }

        public bool MoveOnFileSystem { get; set; }

        public bool UpdatePathInDb { get; set; }

        public bool Overwrite { get; set; }
    }
}
