#pragma warning disable CS1591

using System;
using MediaBrowser.Common.Extensions;

namespace Emby.Server.Implementations.Services
{
    /// <summary>
    /// Donated by Ivan Korneliuk from his post:
    /// http://korneliuk.blogspot.com/2012/08/servicestack-reusing-dtos.html
    ///
    /// Modified to only allow using routes matching the supplied HTTP Verb
    /// </summary>
    public static class UrlExtensions
    {
        public static string GetMethodName(this Type type)
        {
            var typeName = type.FullName != null // can be null, e.g. generic types
                ? StringExtensions.LeftPart(type.FullName, "[[", StringComparison.Ordinal).ToString() // Generic Fullname
                    .Replace(type.Namespace + ".", string.Empty, StringComparison.Ordinal) // Trim Namespaces
                    .Replace("+", ".", StringComparison.Ordinal) // Convert nested into normal type
                : type.Name;

            return type.IsGenericParameter ? "'" + typeName : typeName;
        }
    }
}
