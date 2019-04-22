using System;

namespace Jellyfin.Controller.Entities
{
    public interface IHasStartDate
    {
        DateTime StartDate { get; set; }
    }
}
