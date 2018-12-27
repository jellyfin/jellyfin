using System;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasStartDate
    {
        DateTime StartDate { get; set; }
    }
}
