using MediaBrowser.Model.Dto;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasMediaSources : IHasUserData
    {
        /// <summary>
        /// Gets the media sources.
        /// </summary>
        /// <param name="enablePathSubstitution">if set to <c>true</c> [enable path substitution].</param>
        /// <returns>Task{IEnumerable{MediaSourceInfo}}.</returns>
        IEnumerable<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution);
    }
}
