#pragma warning disable CS1591

namespace MediaBrowser.Controller.Library
{
    public class DeleteOptions
    {
        public bool DeleteFileLocation { get; set; }

        public bool DeleteFromExternalProvider { get; set; }

        public DeleteOptions()
        {
            DeleteFromExternalProvider = true;
        }
    }
}
