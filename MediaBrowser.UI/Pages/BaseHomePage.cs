
namespace MediaBrowser.UI.Pages
{
    public abstract class BaseHomePage : BaseFolderPage
    {
        protected BaseHomePage()
            : base(string.Empty)
        {
        }

        protected override void OnLoaded()
        {
            base.OnLoaded();
            ClearBackdrops();
        }
    }
}
