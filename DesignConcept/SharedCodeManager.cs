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
        /// <param name="path">Optional path to where the dll is that may need loading.</param>
        /// <param name="callerAssembly">The Assembly to be transmuted, normally the calling assembly.</param>
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
                // No it hasn't som load the assembly.
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

                /// Register each class with the attribute SharedCodeAttribute into the DI. Basically, extending the DI into the shared code.
                var sharedCode = GetSharedCodeAssets(ass, false);
                foreach (var (interfaceToRegisterAgainst, type) in sharedCode)
                {   
                    if (interfaceToRegisterAgainst == null)
                    {
                        throw new Exception("Cannot locate {interfaceName} in {Assembly}", (string)customAttrib[0], ass.FullName);
                    }

                    // Add loaded assembly's attributed interfaces to the assembly.
                    serviceCollection.AddSingleton(interfaceToRegisterAgainst, type);
                }
            }

            // Get a list of all interfaces with the attribute SharedCodeAttribute in the calling assembly.
            var codeBase = GetSharedCodeAssets(callerAssembly, true);
            foreach (var (interfaceToRegisterAgainst, _) in sharedCode)
            {
                // Lookup each one in the serviceCollection to see where we need to transmute them to.
                foreach (var service in serviceCollection)
                {
                    // Possible issue: requires unique interfaces across namespaces. 
                    // TODO: look at interface assembly path to see if it ends with <path>
                    if (string.Equals(service.ServiceType.Type.FullName, interfaceToRegisterAgainst))
                    {
                        // Transmute the interfaces, so that they now point to the ones into the loaded assembly.
                        interfaceToRegisterAgainst.Transmute(service.ServiceType.Type);
                    }
                }
            }

        }

        /// <summary>
        /// Returns the with the attribute SharedCodeAttribute in an assembly.
        /// </summary>
        private IEnumerable<(Type t, Type c)>GetSharedCodeAssets(Assembly ass, bool interfaceOnly)
        {
            var types = ass.GetTypes();
            if (interfaceOnly)
            {
                types = types.Where(p => p.IsInterface);
            }
                                     
            foreach (Type type in types) 
            {
                var customAttrib = type.GetCustomAttributes(typeof(SharedCodeAttribute), true);
                if (customAttrib.Length > 0) 
                {
                    // Extract the interfaceName property from the attribute, to decide which type to register it against.
                    var interfaceToRegisterAgainst = ass.GetTypes().Where(i = string.Equals(i.FullName, (string)customAttrib[0]).FirstOrDefault();
                    yield (interfaceToRegisterAgainst, type);
                }
            }
        }
    }
}
