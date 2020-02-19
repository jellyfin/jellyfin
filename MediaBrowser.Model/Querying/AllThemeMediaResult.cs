#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Querying
{
    public class AllThemeMediaResult
    {
        public ThemeMediaResult ThemeVideosResult { get; set; }

        public ThemeMediaResult ThemeSongsResult { get; set; }

        public ThemeMediaResult SoundtrackSongsResult { get; set; }

        public AllThemeMediaResult()
        {
            ThemeVideosResult = new ThemeMediaResult();

            ThemeSongsResult = new ThemeMediaResult();

            SoundtrackSongsResult = new ThemeMediaResult();
        }
    }
}
