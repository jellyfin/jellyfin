using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.Api.Attributes
{
    /// <summary>
    /// Specifies that the marked controller or method is only accessible via the management interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ManagementAttribute : Attribute
    {
    }
}
