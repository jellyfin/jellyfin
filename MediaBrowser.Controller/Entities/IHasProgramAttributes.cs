
namespace MediaBrowser.Controller.Entities
{
    public interface IHasProgramAttributes
    {
        bool IsMovie { get; set; }
        bool IsSports { get; set; }
    }
}
