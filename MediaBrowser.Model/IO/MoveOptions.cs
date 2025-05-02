#pragma warning disable CS1591

namespace MediaBrowser.Controller.Library
{
    public class MoveOptions
    {
        public MoveOptions()
        {
            CreateParent = false;
            Overwrite = true;
            Recursive = true;
        }

        public bool CreateParent { get; set; }

        public bool Overwrite { get; set; }

        public bool Recursive { get; set; }
    }
}
