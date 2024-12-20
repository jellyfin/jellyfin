#nullable disable

#pragma warning disable CA1819, CS1591

using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Audio
{
    public interface IHasMusicGenres
    {
        IReadOnlyList<string> Genres { get; }
    }
}
