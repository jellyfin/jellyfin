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
        /// <param name="callerAssembly">Assembly calling this method.</param>
        
        public static void RegisterSharedCode(
            IServiceCollection serviceCollection, 
            Type interfaceType, 
            string path = "\common\", 
            Assembly callerAssembly)
        {
            // Has this interface already been registered in DI?
            var classRegistered = service.Where(s => string.Equals(service.ServiceType.FullName, interfaceType.FullName));
            
            if (classRegistered.count == 0)
            {
                // Load the assembly, as it hasn't been found.
                var ass = System.Runtime.Loader.AssemblyLoadContext.Default.LoadFromAssemblyPath(PluginPath + path);

                // Extract all the objects from the registry.
                try
                {
                    exportedTypes = ass.GetExportedTypes();
                }
                catch (TypeLoadException ex)
                {
                    // TODO: change this exception.
                    throw new Exception(ex, "Error loading types from {Assembly}.", ass.FullName);
                }

                /// Register each class implimenting SharedCode attribute (DI in shared code).                
                var sharedCode = GetSharedCodeAssets(ass);
                foreach (var (interfaceToRegisterAgainst, type) in sharedCode)
                {   
                    // Add loaded assembly's attributed interfaces to the assembly.
                    serviceCollection.AddSingleton(interfaceToRegisterAgainst, type);
                }
            }

            // Transmute all sharedCode to loaded assembly.
            var codeBase = GetSharedCode(callerAssembly);
            foreach (var (interfaceToRegisterAgainst, _) in sharedCode)
            {
                // how do we know which interfaces to repoint?
                foreach (var service in serviceCollection)
                {
                    // Possible issue: requires unique interfaces across namespaces. 
                    // TODO: look at interface assembly path to see if it ends with <path>
                    if (string.Equals(service.ServiceType.Type.FullName, interfaceToRegisterAgainst))
                    {
                        // Point out interfaces to the loaded assembly interface.
                        interfaceToRegisterAgainst.Transmute(service.ServiceType.Type);
                    }
                }
            }

        }

        private IEnumerable<(Type t, Type c)>GetSharedCodeAssets(Assembly ass)
        {
            foreach (Type type in ass.GetTypes()) 
            {
                var customAttrib = type.GetCustomAttributes(typeof(SharedCodeAttribute), true);
                if (customAttrib.Length > 0) 
                {
                    // Extract the interfaceName property from the attribute, to decide which type to register it against.
                    var interfaceToRegisterAgainst = ass.GetTypes().Where(i = string.Equals(i.FullName, (string)customAttrib[0]).FirstOrDefault();
                    if (interfaceToRegisterAgainst == null)
                    {
                        throw new Exception("Cannot locate {interfaceName} in {Assembly}", (string)customAttrib[0], ass.FullName);
                    }
                    
                    yield (interfaceToRegisterAgainst, type);
                }
            }
        }
    }
}
