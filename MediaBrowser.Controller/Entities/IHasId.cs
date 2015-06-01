using System;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasId
    {
        Guid Id { get; }
    }
}
