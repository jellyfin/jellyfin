#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Reflection;
using MediaBrowser.Common.Extensions;

namespace Emby.Server.Implementations.Services
{
    /// <summary>
    /// Serializer cache of delegates required to create a type from a string map (e.g. for REST urls)
    /// </summary>
    public class StringMapTypeDeserializer
    {
        internal class PropertySerializerEntry
        {
            public PropertySerializerEntry(Action<object, object> propertySetFn, Func<string, object> propertyParseStringFn, Type propertyType)
            {
                PropertySetFn = propertySetFn;
                PropertyParseStringFn = propertyParseStringFn;
                PropertyType = propertyType;
            }

            public Action<object, object> PropertySetFn { get; private set; }
            public Func<string, object> PropertyParseStringFn { get; private set; }
            public Type PropertyType { get; private set; }
        }

        private readonly Type type;
        private readonly Dictionary<string, PropertySerializerEntry> propertySetterMap
            = new Dictionary<string, PropertySerializerEntry>(StringComparer.OrdinalIgnoreCase);

        public Func<string, object> GetParseFn(Type propertyType)
        {
            if (propertyType == typeof(string))
            {
                return s => s;
            }

            return _GetParseFn(propertyType);
        }

        private readonly Func<Type, object> _CreateInstanceFn;
        private readonly Func<Type, Func<string, object>> _GetParseFn;

        public StringMapTypeDeserializer(Func<Type, object> createInstanceFn, Func<Type, Func<string, object>> getParseFn, Type type)
        {
            _CreateInstanceFn = createInstanceFn;
            _GetParseFn = getParseFn;
            this.type = type;

            foreach (var propertyInfo in RestPath.GetSerializableProperties(type))
            {
                var propertySetFn = TypeAccessor.GetSetPropertyMethod(propertyInfo);
                var propertyType = propertyInfo.PropertyType;
                var propertyParseStringFn = GetParseFn(propertyType);
                var propertySerializer = new PropertySerializerEntry(propertySetFn, propertyParseStringFn, propertyType);

                propertySetterMap[propertyInfo.Name] = propertySerializer;
            }
        }

        public object PopulateFromMap(object instance, IDictionary<string, string> keyValuePairs)
        {
            PropertySerializerEntry propertySerializerEntry = null;

            if (instance == null)
            {
                instance = _CreateInstanceFn(type);
            }

            foreach (var pair in keyValuePairs)
            {
                string propertyName = pair.Key;
                string propertyTextValue = pair.Value;

                if (propertyTextValue == null
                    || !propertySetterMap.TryGetValue(propertyName, out propertySerializerEntry)
                    || propertySerializerEntry.PropertySetFn == null)
                {
                    continue;
                }

                if (propertySerializerEntry.PropertyType == typeof(bool))
                {
                    //InputExtensions.cs#530 MVC Checkbox helper emits extra hidden input field, generating 2 values, first is the real value
                    propertyTextValue = StringExtensions.LeftPart(propertyTextValue, ',').ToString();
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
    }

    internal static class TypeAccessor
    {
        public static Action<object, object> GetSetPropertyMethod(PropertyInfo propertyInfo)
        {
            if (!propertyInfo.CanWrite || propertyInfo.GetIndexParameters().Length > 0)
            {
                return null;
            }

            var setMethodInfo = propertyInfo.SetMethod;
            return (instance, value) => setMethodInfo.Invoke(instance, new[] { value });
        }
    }
}
