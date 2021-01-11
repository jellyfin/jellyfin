using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MediaBrowser.Controller.Serialization
{
    /// <summary>
    /// Xml attribute to define multiple [XmlElement()] tags.
    /// https://stackoverflow.com/questions/24707399/how-to-define-multiple-names-for-xmlelement-field.
    /// </summary>
    public class XmlSynonymsAttribute : Attribute
    {
        /// <summary>
        /// Gets the xml synonyms.
        /// </summary>
        private readonly ISet<string> _names;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlSynonymsAttribute"/> class.
        /// </summary>
        /// <param name="names">The synonym tags.</param>
        public XmlSynonymsAttribute(params string[] names)
        {
            this._names = new HashSet<string>(names);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static MemberInfo GetMember(object obj, string name)
        {
            Type type = obj.GetType();

            var result = type.GetProperty(name);
            if (result != null)
            {
                return result;
            }

            foreach (MemberInfo member in type.GetProperties().Cast<MemberInfo>().Union(type.GetFields()))
            {
                foreach (var attr in member.GetCustomAttributes(typeof(XmlSynonymsAttribute), true))
                {
                    if (attr is XmlSynonymsAttribute && ((XmlSynonymsAttribute)attr)._names.Contains(name))
                    {
                        return member;
                    }
                }
            }

            return null;
        }
    }
}
