using System.Collections.Generic;
using Jellyfin.Data.Entities;

namespace Jellyfin.Data
{
    public interface IHasPermissions
    {
        ICollection<Permission> Permissions { get; }
    }
}
