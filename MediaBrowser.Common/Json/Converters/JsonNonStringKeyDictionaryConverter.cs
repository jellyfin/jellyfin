#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaBrowser.Common.Json.Converters
{
    /// <summary>
    /// Converter for Dictionaries without string key.
    /// TODO This can be removed when System.Text.Json supports Dictionaries with non-string keys.
    /// </summary>
    /// <typeparam name="TKey">Type of key.</typeparam>
    /// <typeparam name="TValue">Type of value.</typeparam>
    internal sealed class JsonNonStringKeyDictionaryConverter<TKey, TValue> : JsonConverter<IDictionary<TKey, TValue>>
    {
        /// <summary>
        /// Read JSON.
        /// </summary>
        /// <param name="reader">The Utf8JsonReader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The json serializer options.</param>
        /// <returns>Typed dictionary.</returns>
        /// <exception cref="NotSupportedException">Not supported.</exception>
        public override IDictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var convertedType = typeof(Dictionary<,>).MakeGenericType(typeof(string), typeToConvert.GenericTypeArguments[1]);
            var value = JsonSerializer.Deserialize(ref reader, convertedType, options);
            var instance = (Dictionary<TKey, TValue>)Activator.CreateInstance(
                typeToConvert,
                BindingFlags.Instance | BindingFlags.Public,
                null,
                null,
                CultureInfo.CurrentCulture);
            var enumerator = (IEnumerator)convertedType.GetMethod("GetEnumerator")!.Invoke(value, null);
            var parse = typeof(TKey).GetMethod(
                "Parse",
                0,
                BindingFlags.Public | BindingFlags.Static,
                null,
                CallingConventions.Any,
                new[] { typeof(string) },
                null);
            if (parse == null)
            {
                throw new NotSupportedException($"{typeof(TKey)} as TKey in IDictionary<TKey, TValue> is not supported.");
            }

            while (enumerator.MoveNext())
            {
                var element = (KeyValuePair<string?, TValue>)enumerator.Current;
                instance.Add((TKey)parse.Invoke(null, new[] { (object?)element.Key }), element.Value);
            }

            return instance;
        }

        /// <summary>
        /// Write dictionary as Json.
        /// </summary>
        /// <param name="writer">The Utf8JsonWriter.</param>
        /// <param name="value">The dictionary value.</param>
        /// <param name="options">The Json serializer options.</param>
        public override void Write(Utf8JsonWriter writer, IDictionary<TKey, TValue> value, JsonSerializerOptions options)
        {
            var convertedDictionary = new Dictionary<string?, TValue>(value.Count);
            foreach (var (k, v) in value)
            {
                if (k != null)
                {
                  convertedDictionary[k.ToString()] = v;
                }
            }

            JsonSerializer.Serialize(writer, convertedDictionary, options);
        }
    }
}
