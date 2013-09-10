using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Audio
{
    public interface IHasMusicGenres
    {
        List<string> Genres { get; }
    }
}
