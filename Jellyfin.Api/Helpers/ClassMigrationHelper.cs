using System;
using System.Reflection;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// A static class for copying matching properties from one object to another.
    /// TODO: remove at the point when a fixed migration path has been decided upon.
    /// </summary>
    public static class ClassMigrationHelper
    {
        /// <summary>
        /// Extension for 'Object' that copies the properties to a destination object.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        public static void CopyProperties(this object source, object destination)
        {
            // If any this null throw an exception.
            if (source == null || destination == null)
            {
                throw new Exception("Source or/and Destination Objects are null");
            }

            // Getting the Types of the objects.
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();

            // Iterate the Properties of the source instance and populate them from their destination counterparts.
            PropertyInfo[] srcProps = typeSrc.GetProperties();
            foreach (PropertyInfo srcProp in srcProps)
            {
                if (!srcProp.CanRead)
                {
                    continue;
                }

                var targetProperty = typeDest.GetProperty(srcProp.Name);
                if (targetProperty == null)
                {
                    continue;
                }

                if (!targetProperty.CanWrite)
                {
                    continue;
                }

                var obj = targetProperty.GetSetMethod(true);
                if (obj != null && obj.IsPrivate)
                {
                    continue;
                }

                var target = targetProperty.GetSetMethod();
                if (target != null && (target.Attributes & MethodAttributes.Static) != 0)
                {
                    continue;
                }

                if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                {
                    continue;
                }

                // Passed all tests, lets set the value.
                targetProperty.SetValue(destination, srcProp.GetValue(source, null), null);
            }
        }
    }
}
