using MediaBrowser.Model.Dto;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasMediaSources : IHasMetadata
    {
        /// <summary>
        /// Gets the media sources.
        /// </summary>
        /// <param name="enablePathSubstitution">if set to <c>true</c> [enable path substitution].</param>
        /// <returns>Task{IEnumerable{MediaSourceInfo}}.</returns>
        List<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution);
        List<MediaStream> GetMediaStreams();
    }
}
