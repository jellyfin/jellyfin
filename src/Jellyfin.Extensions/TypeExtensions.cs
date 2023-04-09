using System;
using System.Globalization;

namespace Jellyfin.Extensions;

/// <summary>
/// Provides extensions methods for <see cref="Type" />.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Checks if the supplied value is the default or null value for that type.
    /// </summary>
    /// <typeparam name="T">The type of the value to compare.</typeparam>
    /// <param name="type">The type.</param>
    /// <param name="value">The value to check.</param>
    /// <returns><see langword="true"/> if the value is the default for the type. Otherwise, <see langword="false"/>.</returns>
    public static bool IsNullOrDefault<T>(this Type type, T value)
    {
        if (value is null)
        {
            return true;
        }

        object? tmp = value;
        object? defaultValue = type.IsValueType ? Activator.CreateInstance(type) : null;
        if (type.IsAssignableTo(typeof(IConvertible)))
        {
            tmp = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        return Equals(tmp, defaultValue);
    }

    /// <summary>
    /// Checks if the object is currently a default or null value. Boxed types will be unboxed prior to comparison.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns><see langword="true"/> if the value is the default for the type. Otherwise, <see langword="false"/>.</returns>
    public static bool IsNullOrDefault(this object? obj)
    {
        // Unbox the type and check.
        return obj?.GetType().IsNullOrDefault(obj) ?? true;
    }
}
