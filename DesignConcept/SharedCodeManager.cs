namespace Plugins
{
    public static class SharedCodeManager
    {    
        /// <summary>
        /// Checks to see if the inteface type <see cref="interfaceType"/> has been loaded.
        /// If not, additional assemblies are loaded, and any class implimenting the ISharedCode
        /// interface are registered in the DI.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="interfaceType">The <see cref="Type"/> to ensure is loaded.</param>
        /// <param name="path">Optional path to the dll to load.</param>
        
        public static void RegisterSharedCode(IServiceCollection serviceCollection, Type interfaceType, string path = "\common\")
        {
            // Has this interface already been registered in DI?
            var classRegistered = service.Where(s => string.Equals(service.ServiceType.FullName, interfaceType.FullName));
            
            if (classRegistered.count != 0)
            {
                return;
            }

            // Load the assembly, as it hasn't been found.
            var assembly = System.Runtime.Loader.AssemblyLoadContext.Default
                .LoadFromAssemblyPath(PluginPath + path);

            // Extract all the objects from the registry.
            try
            {
                exportedTypes = assembly.GetExportedTypes();
            }
            catch (TypeLoadException ex)
            {
                // TODO: change this exception.
                throw new Exception(ex, "Error loading types from {Assembly}.", assembly.FullName);
            }

            /// Register each class implimenting SharedCode attribute.
            foreach (Type type in assembly.GetTypes()) 
            {
                var customAttrib = type.GetCustomAttributes(typeof(SharedCodeAttribute), true);
                if (customAttrib.Length > 0) 
                {
                    // Extract the interfaceName property from the attribute, to decide which type to register it against.
                    var interfaceType = assembly.GetTypes().Where(i = string.Equals(i.FullName, (string)customAttrib[0]).FirstOrDefault();
                    if (interface == null)
                    {
                        throw new Exception("Cannot locate {interfaceName} in {Assembly}", (string)customAttrib[0], assembly.FullName);
                    }

                    serviceCollection.AddSingleton(interfaceSupported, type);
                }
            }
        }
    }
}
