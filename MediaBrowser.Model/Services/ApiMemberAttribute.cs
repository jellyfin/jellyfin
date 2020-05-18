using System;

namespace MediaBrowser.Model.Services
{
    /// <summary>
    /// Identifies a single API endpoint.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class ApiMemberAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets verb to which applies attribute. By default applies to all verbs.
        /// </summary>
        public string Verb { get; set; }

        /// <summary>
        /// Gets or sets parameter type: It can be only one of the following: path, query, body, form, or header.
        /// </summary>
        public string ParameterType { get; set; }

        /// <summary>
        /// Gets or sets unique name for the parameter. Each name must be unique, even if they are associated with different paramType values.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Other notes on the name field:
        /// If paramType is body, the name is used only for UI and codegeneration.
        /// If paramType is path, the name field must correspond to the associated path segment from the path field in the api object.
        /// If paramType is query, the name field corresponds to the query param name.
        /// </para>
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the human-readable description for the parameter.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets for path, query, and header paramTypes, this field must be a primitive. For body, this can be a complex or container datatype.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this field must be supplied. For path, this is always true.
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a comma-separated list of values can be passed to the API in the query params. For path and body types, this field cannot be true.
        /// </summary>
        public bool AllowMultiple { get; set; }

        /// <summary>
        /// Gets or sets route to which applies attribute, matches using StartsWith. By default applies to all routes.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether whether to exclude this property from being included in the ModelSchema.
        /// </summary>
        public bool ExcludeInSchema { get; set; }
    }
}
