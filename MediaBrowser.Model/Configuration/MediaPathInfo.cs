#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.Configuration
{
    public class MediaPathInfo
    {
        public string Path { get; set; }

        public string NetworkPath { get; set; }
    }
}
