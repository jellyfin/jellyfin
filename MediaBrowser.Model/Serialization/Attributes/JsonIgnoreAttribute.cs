using System;
using System.Text.Json.Serialization;

namespace MediaBrowser.Model.Serialization.Attributes
{
    /// <summary>
    /// Json ignore attribute.
    /// TODO remove after .Net 5.0
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class JsonIgnoreAttribute : JsonAttribute
    {
        /// <summary>
        /// Gets or sets specifies the condition that must be met before a property will be ignored.
        /// </summary>
        /// <remarks>The default value is <see cref="JsonIgnoreCondition.Always"/>.</remarks>
        public JsonIgnoreCondition Condition { get; set; } = JsonIgnoreCondition.Always;
    }
}
