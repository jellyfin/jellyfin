using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Jellyfin.Api.Attributes
{
    /// <summary>
    /// Declares the [SharedCodeAttribute(interfaceName="abc")] attribute.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]  
    public class SharedCodeAttribute : System.Attribute  
    {  
        private string interfaceName;
    
        public SharedCodeAttribute(string interfaceName)
        {  
            this.interfaceName = interfaceName;
        }  
    }  
}
