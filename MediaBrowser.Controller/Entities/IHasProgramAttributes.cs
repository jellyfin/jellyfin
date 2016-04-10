using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasProgramAttributes
    {
        bool IsMovie { get; set; }
        bool IsSports { get; set; }
        bool IsNews { get; set; }
        bool IsKids { get; set; }
        bool IsRepeat { get; set; }
        bool? IsHD { get; set; }
        bool IsSeries { get; set; }
        bool IsLive { get; set; }
        bool IsPremiere { get; set; }
        ProgramAudio? Audio { get; set; }
        string EpisodeTitle { get; set; }
    }
}
