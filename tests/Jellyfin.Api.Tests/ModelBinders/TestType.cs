using System;
using System.Collections.Generic;
using System.Text;

namespace Jellyfin.Api.Tests.ModelBinders
{
    public enum TestType
    {
#pragma warning disable SA1602 // Enumeration items should be documented
        How,
        Much,
        Is,
        The,
        Fish
#pragma warning restore SA1602 // Enumeration items should be documented
    }
}
