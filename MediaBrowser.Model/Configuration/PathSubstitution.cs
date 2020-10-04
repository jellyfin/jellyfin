#nullable enable
#pragma warning disable CS1591
#pragma warning disable CA1819

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.Configuration
{

    public class PathSubstitution
    {
        public string From { get; set; } = string.Empty;

        public string To { get; set; } = string.Empty;
    }
}
