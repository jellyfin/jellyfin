
namespace MediaBrowser.Model.System
{
    public interface IPowerManagement
    {
        void PreventSystemStandby();
        void AllowSystemStandby();
    }
}
