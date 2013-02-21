using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Reflection;

namespace MediaBrowser.Common.Mef
{
    /// <summary>
    /// Class MefUtils
    /// </summary>
    public static class MefUtils
    {
        /// <summary>
        /// Plugins that live on both the server and UI are going to have references to assemblies from both sides.
        /// But looks for Parts on one side, it will throw an exception when it seems Types from the other side that it doesn't have a reference to.
        /// For example, a plugin provides a Resolver. When MEF runs in the UI, it will throw an exception when it sees the resolver because there won't be a reference to the base class.
        /// This method will catch those exceptions while retining the list of Types that MEF is able to resolve.
        /// </summary>
        /// <param name="catalogs">The catalogs.</param>
        /// <returns>CompositionContainer.</returns>
        /// <exception cref="System.ArgumentNullException">catalogs</exception>
        public static CompositionContainer GetSafeCompositionContainer(IEnumerable<ComposablePartCatalog> catalogs)
        {
            if (catalogs == null)
            {
                throw new ArgumentNullException("catalogs");
            }

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

        /// <summary>
        /// Gets a list of types within an assembly
        /// This will handle situations that would normally throw an exception - such as a type within the assembly that depends on some other non-existant reference
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>IEnumerable{Type}.</returns>
        /// <exception cref="System.ArgumentNullException">assembly</exception>
        public static IEnumerable<Type> GetTypes(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException("assembly");
            }

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // If it fails we can still get a list of the Types it was able to resolve
                return ex.Types.Where(t => t != null);
            }
        }
    }
}
