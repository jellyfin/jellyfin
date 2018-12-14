using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Extensions;

namespace Emby.Server.Implementations.Services
{
    public class RestPath
    {
        private const string WildCard = "*";
        private const char WildCardChar = '*';
        private const string PathSeperator = "/";
        private const char PathSeperatorChar = '/';
        private const char ComponentSeperator = '.';
        private const string VariablePrefix = "{";

        readonly bool[] componentsWithSeparators;

        private readonly string restPath;
        public bool IsWildCardPath { get; private set; }

        private readonly string[] literalsToMatch;

        private readonly string[] variablesNames;

        private readonly bool[] isWildcard;
        private readonly int wildcardCount = 0;

        public int VariableArgsCount { get; set; }

        /// <summary>
        /// The number of segments separated by '/' determinable by path.Split('/').Length
        /// e.g. /path/to/here.ext == 3
        /// </summary>
        public int PathComponentsCount { get; set; }

        /// <summary>
        /// The total number of segments after subparts have been exploded ('.') 
        /// e.g. /path/to/here.ext == 4
        /// </summary>
        public int TotalComponentsCount { get; set; }

        public string[] Verbs { get; private set; }

        public Type RequestType { get; private set; }

        public Type ServiceType { get; private set; }

        public string Path { get { return this.restPath; } }

        public string Summary { get; private set; }
        public string Description { get; private set; }
        public bool IsHidden { get; private set; }

        public int Priority { get; set; } //passed back to RouteAttribute

        public IEnumerable<string> PathVariables
        {
            get { return this.variablesNames.Where(e => !string.IsNullOrWhiteSpace(e)); }
        }

        public static string[] GetPathPartsForMatching(string pathInfo)
        {
            return pathInfo.ToLower().Split(new[] { PathSeperatorChar }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static List<string> GetFirstMatchHashKeys(string[] pathPartsForMatching)
        {
            var hashPrefix = pathPartsForMatching.Length + PathSeperator;
            return GetPotentialMatchesWithPrefix(hashPrefix, pathPartsForMatching);
        }

        public static List<string> GetFirstMatchWildCardHashKeys(string[] pathPartsForMatching)
        {
            const string hashPrefix = WildCard + PathSeperator;
            return GetPotentialMatchesWithPrefix(hashPrefix, pathPartsForMatching);
        }

        private static List<string> GetPotentialMatchesWithPrefix(string hashPrefix, string[] pathPartsForMatching)
        {
            var list = new List<string>();

            foreach (var part in pathPartsForMatching)
            {
                list.Add(hashPrefix + part);

                var subParts = part.Split(ComponentSeperator);
                if (subParts.Length == 1) continue;

                foreach (var subPart in subParts)
                {
                    list.Add(hashPrefix + subPart);
                }
            }

            return list;
        }

        public RestPath(Func<Type, object> createInstanceFn, Func<Type, Func<string, object>> getParseFn, Type requestType, Type serviceType, string path, string verbs, bool isHidden = false, string summary = null, string description = null)
        {
            this.RequestType = requestType;
            this.ServiceType = serviceType;
            this.Summary = summary;
            this.IsHidden = isHidden;
            this.Description = description;
            this.restPath = path;

            this.Verbs = string.IsNullOrWhiteSpace(verbs) ? ServiceExecExtensions.AllVerbs : verbs.ToUpper().Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            var componentsList = new List<string>();

            //We only split on '.' if the restPath has them. Allows for /{action}.{type}
            var hasSeparators = new List<bool>();
            foreach (var component in this.restPath.Split(PathSeperatorChar))
            {
                if (String.IsNullOrEmpty(component)) continue;

                if (StringContains(component, VariablePrefix)
                    && component.IndexOf(ComponentSeperator) != -1)
                {
                    hasSeparators.Add(true);
                    componentsList.AddRange(component.Split(ComponentSeperator));
                }
                else
                {
                    hasSeparators.Add(false);
                    componentsList.Add(component);
                }
            }

            var components = componentsList.ToArray();
            this.TotalComponentsCount = components.Length;

            this.literalsToMatch = new string[this.TotalComponentsCount];
            this.variablesNames = new string[this.TotalComponentsCount];
            this.isWildcard = new bool[this.TotalComponentsCount];
            this.componentsWithSeparators = hasSeparators.ToArray();
            this.PathComponentsCount = this.componentsWithSeparators.Length;
            string firstLiteralMatch = null;

            for (var i = 0; i < components.Length; i++)
            {
                var component = components[i];

                if (component.StartsWith(VariablePrefix))
                {
                    var variableName = component.Substring(1, component.Length - 2);
                    if (variableName[variableName.Length - 1] == WildCardChar)
                    {
                        this.isWildcard[i] = true;
                        variableName = variableName.Substring(0, variableName.Length - 1);
                    }
                    this.variablesNames[i] = variableName;
                    this.VariableArgsCount++;
                }
                else
                {
                    this.literalsToMatch[i] = component.ToLower();

                    if (firstLiteralMatch == null)
                    {
                        firstLiteralMatch = this.literalsToMatch[i];
                    }
                }
            }

            for (var i = 0; i < components.Length - 1; i++)
            {
                if (!this.isWildcard[i]) continue;
                if (this.literalsToMatch[i + 1] == null)
                {
                    throw new ArgumentException(
                        "A wildcard path component must be at the end of the path or followed by a literal path component.");
                }
            }

            this.wildcardCount = this.isWildcard.Count(x => x);
            this.IsWildCardPath = this.wildcardCount > 0;

            this.FirstMatchHashKey = !this.IsWildCardPath
                ? this.PathComponentsCount + PathSeperator + firstLiteralMatch
                : WildCardChar + PathSeperator + firstLiteralMatch;

            this.typeDeserializer = new StringMapTypeDeserializer(createInstanceFn, getParseFn, this.RequestType);
            RegisterCaseInsenstivePropertyNameMappings();
        }

        private void RegisterCaseInsenstivePropertyNameMappings()
        {
            foreach (var propertyInfo in GetSerializableProperties(RequestType))
            {
                var propertyName = propertyInfo.Name;
                propertyNamesMap.Add(propertyName.ToLower(), propertyName);
            }
        }

        internal static string[] IgnoreAttributesNamed = new[] {
            "IgnoreDataMemberAttribute",
            "JsonIgnoreAttribute"
        };


        private static Type excludeType = typeof(Stream);

        internal static List<PropertyInfo> GetSerializableProperties(Type type)
        {
            var list = new List<PropertyInfo>();
            var props = GetPublicProperties(type);

            foreach (var prop in props)
            {
                if (prop.GetMethod == null)
                {
                    continue;
                }

                if (excludeType == prop.PropertyType)
                {
                    continue;
                }

                var ignored = false;
                foreach (var attr in prop.GetCustomAttributes(true))
                {
                    if (IgnoreAttributesNamed.Contains(attr.GetType().Name))
                    {
                        ignored = true;
                        break;
                    }
                }

                if (!ignored)
                {
                    list.Add(prop);
                }
            }

            // else return those properties that are not decorated with IgnoreDataMember
            return list;
        }

        private static List<PropertyInfo> GetPublicProperties(Type type)
        {
            if (type.GetTypeInfo().IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);

                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetTypeInfo().ImplementedInterfaces)
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = GetTypesPublicProperties(subType);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos;
            }

            var list = new List<PropertyInfo>();

            foreach (var t in GetTypesPublicProperties(type))
            {
                if (t.GetIndexParameters().Length == 0)
                {
                    list.Add(t);
                }
            }
            return list;
        }

        private static PropertyInfo[] GetTypesPublicProperties(Type subType)
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

        /// <summary>
        /// Provide for quick lookups based on hashes that can be determined from a request url
        /// </summary>
        public string FirstMatchHashKey { get; private set; }

        private readonly StringMapTypeDeserializer typeDeserializer;

        private readonly Dictionary<string, string> propertyNamesMap = new Dictionary<string, string>();

        public int MatchScore(string httpMethod, string[] withPathInfoParts)
        {
            int wildcardMatchCount;
            var isMatch = IsMatch(httpMethod, withPathInfoParts, out wildcardMatchCount);
            if (!isMatch)
            {
                return -1;
            }

            var score = 0;

            //Routes with least wildcard matches get the highest score
            score += Math.Max((100 - wildcardMatchCount), 1) * 1000;

            //Routes with less variable (and more literal) matches
            score += Math.Max((10 - VariableArgsCount), 1) * 100;

            //Exact verb match is better than ANY
            if (Verbs.Length == 1 && string.Equals(httpMethod, Verbs[0], StringComparison.OrdinalIgnoreCase))
            {
                score += 10;
            }
            else
            {
                score += 1;
            }

            return score;
        }

        private bool StringContains(string str1, string str2)
        {
            return str1.IndexOf(str2, StringComparison.OrdinalIgnoreCase) != -1;
        }

        /// <summary>
        /// For performance withPathInfoParts should already be a lower case string
        /// to minimize redundant matching operations.
        /// </summary>
        public bool IsMatch(string httpMethod, string[] withPathInfoParts, out int wildcardMatchCount)
        {
            wildcardMatchCount = 0;

            if (withPathInfoParts.Length != this.PathComponentsCount && !this.IsWildCardPath)
            {
               return false;
            }

            if (!Verbs.Contains(httpMethod, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!ExplodeComponents(ref withPathInfoParts))
            {
                return false;
            }

            if (this.TotalComponentsCount != withPathInfoParts.Length && !this.IsWildCardPath)
            {
                return false;
            }

            int pathIx = 0;
            for (var i = 0; i < this.TotalComponentsCount; i++)
            {
                if (this.isWildcard[i])
                {
                    if (i < this.TotalComponentsCount - 1)
                    {
                        // Continue to consume up until a match with the next literal
                        while (pathIx < withPathInfoParts.Length && !LiteralsEqual(withPathInfoParts[pathIx], this.literalsToMatch[i + 1]))
                        {
                            pathIx++;
                            wildcardMatchCount++;
                        }

                        // Ensure there are still enough parts left to match the remainder
                        if ((withPathInfoParts.Length - pathIx) < (this.TotalComponentsCount - i - 1))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // A wildcard at the end matches the remainder of path
                        wildcardMatchCount += withPathInfoParts.Length - pathIx;
                        pathIx = withPathInfoParts.Length;
                    }
                }
                else
                {
                    var literalToMatch = this.literalsToMatch[i];
                    if (literalToMatch == null)
                    {
                        // Matching an ordinary (non-wildcard) variable consumes a single part
                        pathIx++;
                        continue;
                    }

                    if (withPathInfoParts.Length <= pathIx || !LiteralsEqual(withPathInfoParts[pathIx], literalToMatch))
                    {
                        return false;
                    }
                    pathIx++;
                }
            }

            return pathIx == withPathInfoParts.Length;
        }

        private bool LiteralsEqual(string str1, string str2)
        {
            // Most cases
            if (String.Equals(str1, str2, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Handle turkish i
            str1 = str1.ToUpperInvariant();
            str2 = str2.ToUpperInvariant();

            // Invariant IgnoreCase would probably be better but it's not available in PCL
            return String.Equals(str1, str2, StringComparison.CurrentCultureIgnoreCase);
        }

        private bool ExplodeComponents(ref string[] withPathInfoParts)
        {
            var totalComponents = new List<string>();
            for (var i = 0; i < withPathInfoParts.Length; i++)
            {
                var component = withPathInfoParts[i];
                if (String.IsNullOrEmpty(component)) continue;

                if (this.PathComponentsCount != this.TotalComponentsCount
                    && this.componentsWithSeparators[i])
                {
                    var subComponents = component.Split(ComponentSeperator);
                    if (subComponents.Length < 2) return false;
                    totalComponents.AddRange(subComponents);
                }
                else
                {
                    totalComponents.Add(component);
                }
            }

            withPathInfoParts = totalComponents.ToArray();
            return true;
        }

        public object CreateRequest(string pathInfo, Dictionary<string, string> queryStringAndFormData, object fromInstance)
        {
            var requestComponents = pathInfo.Split(new[] { PathSeperatorChar }, StringSplitOptions.RemoveEmptyEntries);

            ExplodeComponents(ref requestComponents);

            if (requestComponents.Length != this.TotalComponentsCount)
            {
                var isValidWildCardPath = this.IsWildCardPath
                    && requestComponents.Length >= this.TotalComponentsCount - this.wildcardCount;

                if (!isValidWildCardPath)
                    throw new ArgumentException(String.Format(
                        "Path Mismatch: Request Path '{0}' has invalid number of components compared to: '{1}'",
                        pathInfo, this.restPath));
            }

            var requestKeyValuesMap = new Dictionary<string, string>();
            var pathIx = 0;
            for (var i = 0; i < this.TotalComponentsCount; i++)
            {
                var variableName = this.variablesNames[i];
                if (variableName == null)
                {
                    pathIx++;
                    continue;
                }

                string propertyNameOnRequest;
                if (!this.propertyNamesMap.TryGetValue(variableName.ToLower(), out propertyNameOnRequest))
                {
                    if (String.Equals("ignore", variableName, StringComparison.OrdinalIgnoreCase))
                    {
                        pathIx++;
                        continue;
                    }

                    throw new ArgumentException("Could not find property "
                        + variableName + " on " + RequestType.GetMethodName());
                }

                var value = requestComponents.Length > pathIx ? requestComponents[pathIx] : null; //wildcard has arg mismatch
                if (value != null && this.isWildcard[i])
                {
                    if (i == this.TotalComponentsCount - 1)
                    {
                        // Wildcard at end of path definition consumes all the rest
                        var sb = new StringBuilder();
                        sb.Append(value);
                        for (var j = pathIx + 1; j < requestComponents.Length; j++)
                        {
                            sb.Append(PathSeperatorChar + requestComponents[j]);
                        }
                        value = sb.ToString();
                    }
                    else
                    {
                        // Wildcard in middle of path definition consumes up until it
                        // hits a match for the next element in the definition (which must be a literal)
                        // It may consume 0 or more path parts
                        var stopLiteral = i == this.TotalComponentsCount - 1 ? null : this.literalsToMatch[i + 1];
                        if (!String.Equals(requestComponents[pathIx], stopLiteral, StringComparison.OrdinalIgnoreCase))
                        {
                            var sb = new StringBuilder();
                            sb.Append(value);
                            pathIx++;
                            while (!String.Equals(requestComponents[pathIx], stopLiteral, StringComparison.OrdinalIgnoreCase))
                            {
                                sb.Append(PathSeperatorChar + requestComponents[pathIx++]);
                            }
                            value = sb.ToString();
                        }
                        else
                        {
                            value = null;
                        }
                    }
                }
                else
                {
                    // Variable consumes single path item
                    pathIx++;
                }

                requestKeyValuesMap[propertyNameOnRequest] = value;
            }

            if (queryStringAndFormData != null)
            {
                //Query String and form data can override variable path matches
                //path variables < query string < form data
                foreach (var name in queryStringAndFormData)
                {
                    requestKeyValuesMap[name.Key] = name.Value;
                }
            }

            return this.typeDeserializer.PopulateFromMap(fromInstance, requestKeyValuesMap);
        }

        public class RestPathMap : SortedDictionary<string, List<RestPath>>
        {
            public RestPathMap() : base(StringComparer.OrdinalIgnoreCase)
            {
            }
        }
    }
}
