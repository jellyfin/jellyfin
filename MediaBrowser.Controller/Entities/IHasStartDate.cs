#pragma warning disable CS1591

using System;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasStartDate
    {
        DateTime StartDate { get; set; }
    }
}
