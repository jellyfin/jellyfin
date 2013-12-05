using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasProductionLocations
    /// </summary>
    public interface IHasProductionLocations
    {
        /// <summary>
        /// Gets or sets the production locations.
        /// </summary>
        /// <value>The production locations.</value>
        List<string> ProductionLocations { get; set; }
    }

    public static class ProductionLocationExtensions
    {
        public static void AddProductionLocation(this IHasProductionLocations item, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }

            if (!item.ProductionLocations.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                item.ProductionLocations.Add(name);
            }
        }
    }
}
