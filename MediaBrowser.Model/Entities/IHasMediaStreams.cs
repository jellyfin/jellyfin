using System.Collections.Generic;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// This is essentially a marker interface
    /// </summary>
    public interface IHasMediaStreams
    {
        List<MediaStream> MediaStreams { get; set; }
    }
}
