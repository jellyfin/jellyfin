namespace Jellyfin.Controller.Entities.Audio
{
    public interface IHasMusicGenres
    {
        string[] Genres { get; }
    }
}
