using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;

namespace MediaBrowser.Common.Mef
{
    public static class MefUtils
    {
        /// <summary>
        /// Plugins that live on both the server and UI are going to have references to assemblies from both sides.
        /// But looks for Parts on one side, it will throw an exception when it seems Types from the other side that it doesn't have a reference to.
        /// For example, a plugin provides a Resolver. When MEF runs in the UI, it will throw an exception when it sees the resolver because there won't be a reference to the base class.
        /// This method will catch those exceptions while retining the list of Types that MEF is able to resolve.
        /// </summary>
        public static CompositionContainer GetSafeCompositionContainer(IEnumerable<ComposablePartCatalog> catalogs)
        {
            var newList = new List<ComposablePartCatalog>();

            // Go through each Catalog
            foreach (var catalog in catalogs)
            {
                try
                {
                    // Try to have MEF find Parts
                    catalog.Parts.ToArray();

                    // If it succeeds we can use the entire catalog
                    newList.Add(catalog);
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // If it fails we can still get a list of the Types it was able to resolve and create TypeCatalogs
                    var typeCatalogs = ex.Types.Where(t => t != null).Select(t => new TypeCatalog(t));
                    newList.AddRange(typeCatalogs);
                }
            }

            return new CompositionContainer(new AggregateCatalog(newList));
        }
    }
}
