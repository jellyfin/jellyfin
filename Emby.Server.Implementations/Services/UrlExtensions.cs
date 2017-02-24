using System;

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
            var typeName = type.FullName != null //can be null, e.g. generic types
                ? LeftPart(type.FullName, "[[")   //Generic Fullname
                    .Replace(type.Namespace + ".", "") //Trim Namespaces
                    .Replace("+", ".") //Convert nested into normal type
                : type.Name;

            return type.IsGenericParameter ? "'" + typeName : typeName;
        }

        public static string LeftPart(string strVal, string needle)
        {
            if (strVal == null) return null;
            var pos = strVal.IndexOf(needle, StringComparison.OrdinalIgnoreCase);
            return pos == -1
                ? strVal
                : strVal.Substring(0, pos);
        }
    }
}