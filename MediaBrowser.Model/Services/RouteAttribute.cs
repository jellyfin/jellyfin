#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Services
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RouteAttribute : Attribute
    {
        /// <summary>
        ///     <para>Initializes an instance of the <see cref="RouteAttribute"/> class.</para>
        /// </summary>
        /// <param name="path">
        ///     <para>The path template to map to the request.  See
        ///        <see cref="Path">RouteAttribute.Path</see>
        ///        for details on the correct format.</para>
        /// </param>
        public RouteAttribute(string path)
            : this(path, null)
        {
        }

        /// <summary>
        ///     <para>Initializes an instance of the <see cref="RouteAttribute"/> class.</para>
        /// </summary>
        /// <param name="path">
        ///     <para>The path template to map to the request.  See
        ///         <see cref="Path">RouteAttribute.Path</see>
        ///         for details on the correct format.</para>
        /// </param>
        /// <param name="verbs">A comma-delimited list of HTTP verbs supported by the
        ///     service.  If unspecified, all verbs are assumed to be supported.</param>
        public RouteAttribute(string path, string verbs)
        {
            Path = path;
            Verbs = verbs;
        }

        /// <summary>
        ///     Gets or sets the path template to be mapped to the request.
        /// </summary>
        /// <value>
        ///     A <see cref="String"/> value providing the path mapped to
        ///     the request.  Never <see langword="null"/>.
        /// </value>
        /// <remarks>
        ///     <para>Some examples of valid paths are:</para>
        ///
        ///     <list>
        ///         <item>"/Inventory"</item>
        ///         <item>"/Inventory/{Category}/{ItemId}"</item>
        ///         <item>"/Inventory/{ItemPath*}"</item>
        ///     </list>
        ///
        ///     <para>Variables are specified within "{}"
        ///     brackets.  Each variable in the path is mapped to the same-named property
        ///     on the request DTO.  At runtime, ServiceStack will parse the
        ///     request URL, extract the variable values, instantiate the request DTO,
        ///     and assign the variable values into the corresponding request properties,
        ///     prior to passing the request DTO to the service object for processing.</para>
        ///
        ///     <para>It is not necessary to specify all request properties as
        ///     variables in the path.  For unspecified properties, callers may provide
        ///     values in the query string.  For example: the URL
        ///     "http://services/Inventory?Category=Books&amp;ItemId=12345" causes the same
        ///     request DTO to be processed as "http://services/Inventory/Books/12345",
        ///     provided that the paths "/Inventory" (which supports the first URL) and
        ///     "/Inventory/{Category}/{ItemId}" (which supports the second URL)
        ///     are both mapped to the request DTO.</para>
        ///
        ///     <para>Please note that while it is possible to specify property values
        ///     in the query string, it is generally considered to be less RESTful and
        ///     less desirable than to specify them as variables in the path.  Using the
        ///     query string to specify property values may also interfere with HTTP
        ///     caching.</para>
        ///
        ///     <para>The final variable in the path may contain a "*" suffix
        ///     to grab all remaining segments in the path portion of the request URL and assign
        ///     them to a single property on the request DTO.
        ///     For example, if the path "/Inventory/{ItemPath*}" is mapped to the request DTO,
        ///     then the request URL "http://services/Inventory/Books/12345" will result
        ///     in a request DTO whose ItemPath property contains "Books/12345".
        ///     You may only specify one such variable in the path, and it must be positioned at
        ///     the end of the path.</para>
        /// </remarks>
        public string Path { get; set; }

        /// <summary>
        ///    Gets or sets short summary of what the route does.
        /// </summary>
        public string Summary { get; set; }

        public string Description { get; set; }

        public bool IsHidden { get; set; }

        /// <summary>
        ///    Gets or sets longer text to explain the behaviour of the route.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        ///     Gets or sets a comma-delimited list of HTTP verbs supported by the service, such as
        ///     "GET,PUT,POST,DELETE".
        /// </summary>
        /// <value>
        ///     A <see cref="String"/> providing a comma-delimited list of HTTP verbs supported
        ///     by the service, <see langword="null"/> or empty if all verbs are supported.
        /// </value>
        public string Verbs { get; set; }

        /// <summary>
        /// Used to rank the precedences of route definitions in reverse routing.
        /// i.e. Priorities below 0 are auto-generated have less precedence.
        /// </summary>
        public int Priority { get; set; }

        protected bool Equals(RouteAttribute other)
        {
            return base.Equals(other)
                && string.Equals(Path, other.Path)
                && string.Equals(Summary, other.Summary)
                && string.Equals(Notes, other.Notes)
                && string.Equals(Verbs, other.Verbs)
                && Priority == other.Priority;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RouteAttribute)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (Path != null ? Path.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Summary != null ? Summary.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Notes != null ? Notes.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Verbs != null ? Verbs.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Priority;
                return hashCode;
            }
        }
    }
}
