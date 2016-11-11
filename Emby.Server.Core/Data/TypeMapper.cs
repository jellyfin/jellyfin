using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Emby.Server.Core.Data
{
    /// <summary>
    /// Class TypeMapper
    /// </summary>
    public class TypeMapper
    {
        /// <summary>
        /// This holds all the types in the running assemblies so that we can de-serialize properly when we don't have strong types
        /// </summary>
        private readonly ConcurrentDictionary<string, Type> _typeMap = new ConcurrentDictionary<string, Type>();

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns>Type.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public Type GetType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException();
            }

            return _typeMap.GetOrAdd(typeName, LookupType);
        }

        /// <summary>
        /// Lookups the type.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns>Type.</returns>
        private Type LookupType(string typeName)
        {
            return AppDomain
                        .CurrentDomain
                        .GetAssemblies()
                        .Select(a => a.GetType(typeName, false))
                        .FirstOrDefault(t => t != null);
        }
    }
}
