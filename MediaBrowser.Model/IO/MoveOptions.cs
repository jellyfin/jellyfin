#pragma warning disable CS1591

namespace MediaBrowser.Controller.Library
{
    public class MoveOptions
    {
        public MoveOptions()
        {
            CreateParent = false;
            Overwrite = true;
            Recursive = false;
        }

        public bool CreateParent { get; set; }

        public bool Overwrite { get; set; }

        public bool Recursive { get; set; }
    }
}
