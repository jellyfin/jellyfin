using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack
{
    public static class ReflectionExtensions
    {
        public static bool IsInstanceOf(this Type type, Type thisOrBaseType)
        {
            while (type != null)
            {
                if (type == thisOrBaseType)
                    return true;

                type = type.BaseType();
            }
            return false;
        }

        public static Type FirstGenericType(this Type type)
        {
            while (type != null)
            {
                if (type.IsGeneric())
                    return type;

                type = type.BaseType();
            }
            return null;
        }

        public static Type GetTypeWithGenericTypeDefinitionOf(this Type type, Type genericTypeDefinition)
        {
            foreach (var t in type.GetTypeInterfaces())
            {
                if (t.IsGeneric() && t.GetGenericTypeDefinition() == genericTypeDefinition)
                {
                    return t;
                }
            }

            var genericType = type.FirstGenericType();
            if (genericType != null && genericType.GetGenericTypeDefinition() == genericTypeDefinition)
            {
                return genericType;
            }

            return null;
        }

        public static PropertyInfo[] GetAllProperties(this Type type)
        {
            if (type.IsInterface())
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);

                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetTypeInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetTypesProperties();

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetTypesProperties()
                .Where(t => t.GetIndexParameters().Length == 0) // ignore indexed properties
                .ToArray();
        }

        public static PropertyInfo[] GetPublicProperties(this Type type)
        {
            if (type.IsInterface())
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);

                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetTypeInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetTypesPublicProperties();

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetTypesPublicProperties()
                .Where(t => t.GetIndexParameters().Length == 0) // ignore indexed properties
                .ToArray();
        }

        public const string DataMember = "DataMemberAttribute";

        internal static string[] IgnoreAttributesNamed = new[] {
            "IgnoreDataMemberAttribute",
            "JsonIgnoreAttribute"
        };

        public static PropertyInfo[] GetSerializableProperties(this Type type)
        {
            var properties = type.IsDto()
                ? type.GetAllProperties()
                : type.GetPublicProperties();
            return properties.OnlySerializableProperties(type);
        }


        private static List<Type> _excludeTypes = new List<Type> { typeof(Stream) };

        public static PropertyInfo[] OnlySerializableProperties(this PropertyInfo[] properties, Type type = null)
        {
            var isDto = type.IsDto();
            var readableProperties = properties.Where(x => x.PropertyGetMethod(nonPublic: isDto) != null);

            if (isDto)
            {
                return readableProperties.Where(attr =>
                    attr.HasAttribute<DataMemberAttribute>()).ToArray();
            }

            // else return those properties that are not decorated with IgnoreDataMember
            return readableProperties
                .Where(prop => prop.AllAttributes()
                    .All(attr =>
                    {
                        var name = attr.GetType().Name;
                        return !IgnoreAttributesNamed.Contains(name);
                    }))
                .Where(prop => !_excludeTypes.Contains(prop.PropertyType))
                .ToArray();
        }
    }

    public static class PlatformExtensions //Because WinRT is a POS
    {
        public static bool IsInterface(this Type type)
        {
            return type.GetTypeInfo().IsInterface;
        }

        public static bool IsGeneric(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        public static Type BaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }

        public static Type[] GetTypeInterfaces(this Type type)
        {
            return type.GetTypeInfo().ImplementedInterfaces.ToArray();
        }

        internal static PropertyInfo[] GetTypesPublicProperties(this Type subType)
        {
            var pis = new List<PropertyInfo>();
            foreach (var pi in subType.GetRuntimeProperties())
            {
                var mi = pi.GetMethod ?? pi.SetMethod;
                if (mi != null && mi.IsStatic) continue;
                pis.Add(pi);
            }
            return pis.ToArray();
        }

        internal static PropertyInfo[] GetTypesProperties(this Type subType)
        {
            var pis = new List<PropertyInfo>();
            foreach (var pi in subType.GetRuntimeProperties())
            {
                var mi = pi.GetMethod ?? pi.SetMethod;
                if (mi != null && mi.IsStatic) continue;
                pis.Add(pi);
            }
            return pis.ToArray();
        }

        public static bool HasAttribute<T>(this Type type)
        {
            return type.AllAttributes().Any(x => x.GetType() == typeof(T));
        }

        public static bool HasAttribute<T>(this PropertyInfo pi)
        {
            return pi.AllAttributes().Any(x => x.GetType() == typeof(T));
        }

        public static bool IsDto(this Type type)
        {
            if (type == null)
                return false;

            return type.HasAttribute<DataContractAttribute>();
        }

        public static MethodInfo PropertyGetMethod(this PropertyInfo pi, bool nonPublic = false)
        {
            return pi.GetMethod;
        }

        public static object[] AllAttributes(this PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttributes(true).ToArray();
        }

        public static object[] AllAttributes(this PropertyInfo propertyInfo, Type attrType)
        {
            return propertyInfo.GetCustomAttributes(true).Where(x => attrType.IsInstanceOf(x.GetType())).ToArray();
        }

        public static object[] AllAttributes(this Type type)
        {
            return type.GetTypeInfo().GetCustomAttributes(true).ToArray();
        }

        public static TAttr[] AllAttributes<TAttr>(this PropertyInfo pi)
        {
            return pi.AllAttributes(typeof(TAttr)).Cast<TAttr>().ToArray();
        }

        public static TAttr[] AllAttributes<TAttr>(this Type type)
            where TAttr : Attribute
        {
            return type.GetTypeInfo().GetCustomAttributes<TAttr>(true).ToArray();
        }
    }
}
