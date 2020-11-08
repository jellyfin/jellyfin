#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasMediaSources
    {
        Guid Id { get; set; }

        long? RunTimeTicks { get; set; }

        string Path { get; }

        /// <summary>
        /// Gets the media sources.
        /// </summary>
        List<MediaSourceInfo> GetMediaSources(bool enablePathSubstitution);

        List<MediaStream> GetMediaStreams();
    }
}
