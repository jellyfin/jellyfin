
namespace MediaBrowser.Controller.Entities.Movies
{
    /// <summary>
    /// Class BoxSet
    /// </summary>
    public class BoxSet : Folder
    {
        protected override bool SupportsShortcutChildren
        {
            get
            {
                return true;
            }
        }
    }
}
