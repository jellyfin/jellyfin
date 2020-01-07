using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Jellyfin.Api
{
    /// <summary>
    /// Route prefixing for ASP.NET MVC.
    /// </summary>
    public static class MvcRoutePrefix
    {
        /// <summary>
        /// Adds route prefixes to the MVC conventions.
        /// </summary>
        /// <param name="opts">The MVC options.</param>
        /// <param name="prefixes">The list of prefixes.</param>
        public static void UseGeneralRoutePrefix(this MvcOptions opts, params string[] prefixes)
        {
            opts.Conventions.Insert(0, new RoutePrefixConvention(prefixes));
        }

        private class RoutePrefixConvention : IApplicationModelConvention
        {
            private readonly AttributeRouteModel[] _routePrefixes;

            public RoutePrefixConvention(IEnumerable<string> prefixes)
            {
                _routePrefixes = prefixes.Select(p => new AttributeRouteModel(new RouteAttribute(p))).ToArray();
            }

            public void Apply(ApplicationModel application)
            {
                foreach (var controller in application.Controllers)
                {
                    if (controller.Selectors == null)
                    {
                        continue;
                    }

                    var newSelectors = new List<SelectorModel>();
                    foreach (var selector in controller.Selectors)
                    {
                        newSelectors.AddRange(_routePrefixes.Select(routePrefix => new SelectorModel(selector)
                        {
                            AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(routePrefix, selector.AttributeRouteModel)
                        }));
                    }

                    controller.Selectors.Clear();
                    newSelectors.ForEach(selector => controller.Selectors.Add(selector));
                }
            }
        }
    }
}
