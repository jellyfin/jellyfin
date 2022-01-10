#nullable disable

#pragma warning disable CA1819, CS1591

namespace MediaBrowser.Controller.Entities.Audio
{
    public interface IHasMusicGenres
    {
        string[] Genres { get; }
    }
}
