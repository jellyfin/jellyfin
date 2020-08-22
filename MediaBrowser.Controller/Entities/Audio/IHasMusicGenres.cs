#pragma warning disable CS1591

namespace MediaBrowser.Controller.Entities.Audio
{
    public interface IHasMusicGenres
    {
        string[] Genres { get; }
    }
}
