#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Audio
{
    public interface IHasMusicGenres
    {
        IEnumerable<string> Genres { get; }
    }
}
