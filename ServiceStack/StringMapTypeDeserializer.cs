using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;
using System.Reflection;

namespace ServiceStack.Serialization
{
    /// <summary>
    /// Serializer cache of delegates required to create a type from a string map (e.g. for REST urls)
    /// </summary>
    public class StringMapTypeDeserializer
    {
        internal class PropertySerializerEntry
        {
            public PropertySerializerEntry(Action<object,object> propertySetFn, Func<string, object> propertyParseStringFn)
            {
                PropertySetFn = propertySetFn;
                PropertyParseStringFn = propertyParseStringFn;
            }

            public Action<object, object> PropertySetFn;
            public Func<string,object> PropertyParseStringFn;
            public Type PropertyType;
        }

        private readonly Type type;
        private readonly Dictionary<string, PropertySerializerEntry> propertySetterMap
            = new Dictionary<string, PropertySerializerEntry>(StringComparer.OrdinalIgnoreCase);

        public Func<string, object> GetParseFn(Type propertyType)
        {
            //Don't JSV-decode string values for string properties
            if (propertyType == typeof(string))
                return s => s;

            return ServiceStackHost.Instance.GetParseFn(propertyType);
        }

        public StringMapTypeDeserializer(Type type)
        {
            this.type = type;

            foreach (var propertyInfo in type.GetSerializableProperties())
            {
                var propertySetFn = TypeAccessor.GetSetPropertyMethod(type, propertyInfo);
                var propertyType = propertyInfo.PropertyType;
                var propertyParseStringFn = GetParseFn(propertyType);
                var propertySerializer = new PropertySerializerEntry(propertySetFn, propertyParseStringFn) { PropertyType = propertyType };

                var attr = propertyInfo.AllAttributes<DataMemberAttribute>().FirstOrDefault();
                if (attr != null && attr.Name != null)
                {
                    propertySetterMap[attr.Name] = propertySerializer;
                }
                propertySetterMap[propertyInfo.Name] = propertySerializer;
            }
        }

        public object PopulateFromMap(object instance, IDictionary<string, string> keyValuePairs)
        {
            string propertyName = null;
            string propertyTextValue = null;
            PropertySerializerEntry propertySerializerEntry = null;

            if (instance == null)
                instance = ServiceStackHost.Instance.CreateInstance(type);

            foreach (var pair in keyValuePairs.Where(x => !string.IsNullOrEmpty(x.Value)))
            {
                propertyName = pair.Key;
                propertyTextValue = pair.Value;

                if (!propertySetterMap.TryGetValue(propertyName, out propertySerializerEntry))
                {
                    if (propertyName == "v")
                    {
                        continue;
                    }

                    continue;
                }

                if (propertySerializerEntry.PropertySetFn == null)
                {
                    continue;
                }

                if (propertySerializerEntry.PropertyType == typeof(bool))
                {
                    //InputExtensions.cs#530 MVC Checkbox helper emits extra hidden input field, generating 2 values, first is the real value
                    propertyTextValue = LeftPart(propertyTextValue, ',');
                }

                var value = propertySerializerEntry.PropertyParseStringFn(propertyTextValue);
                if (value == null)
                {
                    continue;
                }
                propertySerializerEntry.PropertySetFn(instance, value);
            }

            return instance;
        }

        public static string LeftPart(string strVal, char needle)
        {
            if (strVal == null) return null;
            var pos = strVal.IndexOf(needle);
            return pos == -1
                ? strVal
                : strVal.Substring(0, pos);
        }
    }

    internal class TypeAccessor
    {
        public static Action<object, object> GetSetPropertyMethod(Type type, PropertyInfo propertyInfo)
        {
            if (!propertyInfo.CanWrite || propertyInfo.GetIndexParameters().Any()) return null;

            var setMethodInfo = propertyInfo.SetMethod;
            return (instance, value) => setMethodInfo.Invoke(instance, new[] { value });
        }
    }
}
