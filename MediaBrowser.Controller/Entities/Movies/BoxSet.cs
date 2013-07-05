
namespace MediaBrowser.Controller.Entities.Movies
{
    /// <summary>
    /// Class BoxSet
    /// </summary>
    public class BoxSet : Folder
    {
        protected override bool SupportsLinkedChildren
        {
            get
            {
                return true;
            }
        }
    }
}
