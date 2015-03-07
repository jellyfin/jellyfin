using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasMediaSources
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        Guid Id { get; }

        /// <summary>
        /// Gets the media sources.
        /// </summary>
        /// <param name="enablePathSubstitution">if set to <c>true</c> [enable path substitution].</param>
        /// <returns>Task{IEnumerable{MediaSourceInfo}}.</returns>
        IEnumerable<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution);
    }
}
