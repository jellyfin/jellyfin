using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasMediaSources
    {
        /// <summary>
        /// Gets the media sources.
        /// </summary>
        List<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution);
        List<MediaStream> GetMediaStreams();
        Guid Id { get; set; }

        long? RunTimeTicks { get; set; }

        string Path { get; }
    }
}
