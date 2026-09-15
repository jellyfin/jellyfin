using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Jellyfin.Server.Filters
{
    /// <summary>
    /// Searches for actions and controllers marked with <see cref="ObsoleteAttribute"/> and applies a custom filter.
    /// </summary>
    public class ObsoleteEndpointConvention : IApplicationModelConvention
    {
        /// <inheritdoc />
        public void Apply(ApplicationModel application)
        {
            foreach (var controller in application.Controllers)
            {
                var isControllerObsolete = controller.Attributes.OfType<ObsoleteAttribute>().Any();

                foreach (var action in controller.Actions)
                {
                    if (isControllerObsolete || action.Attributes.OfType<ObsoleteAttribute>().Any())
                    {
                        action.Filters.Add(new ObsoleteEndpointFilter());
                    }
                }
            }
        }
    }
}
