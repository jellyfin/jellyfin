#pragma warning disable IDE0059 // Unnecessary assignment of a value : Left in to aid in debugging.

using System;
using System.Text;
using System.Web;
using XMLProperties = System.Collections.Generic.Dictionary<string, string>;

namespace Emby.Dlna.PlayTo
{
    /// <summary>
    /// Non-standard XML parser.
    ///
    /// Parses an XML style document into a dictionary.
    ///
    /// Does not support multiple objects. eg <child>1</child><child>2</child> will parse to : "child","2".
    /// </summary>
    public static class XMLUtilities
    {
        /// <summary>
        /// Fixes the issue whereby some dlna devices htmlencode all the data and provide it under LastChange.
        /// HtmlDecode can corrupt the XML by inserting non-permittable characters in the attribute values.
        /// </summary>
        /// <param name="xml">The xml string to check.</param>
        /// <param name="properties">The xml values in attrib/attribvalue, or tagname/value.</param>
        /// <returns>A valid XDocument, or null if there is an error.</returns>
        public static bool ParseXML(string xml, out XMLProperties properties)
        {
            // Some devices encode the whole return wrapped in a XML container, and encode the individual properties as well.
            xml = HttpUtility.HtmlDecode(xml);
            properties = new XMLProperties();

            bool inTag = false;
            bool inAttrib = false;
            bool inAttribValue = false;
            bool isClosed = false;
            string tagName = string.Empty;
            string attribName = string.Empty;
            string attribValue;
            string value = string.Empty;
            string lastTag = string.Empty;
            bool hasAttribute = false;

            StringBuilder token = new StringBuilder();

            char lastChar = ' ';

            int a = 0;
            while (a < xml.Length)
            {
                char c = xml[a];

                switch (c)
                {
                    case '?':
                        if (lastChar == '<')
                        {
                            // Skip XML definition.
                            while (a < xml.Length && xml[a] != '>')
                            {
                                a++;
                            }

                            a++;
                            lastChar = c;
                            continue;
                        }

                        break;

                    case '/':
                        // Closing character. Valid for tags only.
                        if (inTag & !inAttrib & !inAttribValue)
                        {
                            a++;
                            lastChar = c;
                            isClosed = true;
                            continue;
                        }

                        break;

                    case ':':
                        // Namespaces only exist in tags and attribute names.
                        if ((inTag || inAttrib) & !inAttribValue)
                        {
                            // We don't want namespaces.
                            token.Clear();
                            a++;
                            lastChar = c;
                            continue;
                        }

                        break;

                    case '<':
                        if (!inTag)
                        {
                            // If we weren't in a tag, it was text.
                            value = token.ToString();
                            token.Clear();

                            // If there was a value, and we had a tag name then store it.
                            if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(tagName))
                            {
                                properties[tagName] = value;
                            }
                        }

                        if (isClosed)
                        {
                            lastTag = tagName;
                            tagName = string.Empty;
                            isClosed = false;
                        }
                        else if (!inTag)
                        {
                            tagName = string.Empty;
                        }

                        inTag = true;

                        // Reset everthing else.
                        hasAttribute = false;
                        inAttrib = false;
                        inAttribValue = false;
                        a++;
                        lastChar = c;
                        continue;

                    case '>':
                        // If we were in an attribute name
                        if (inAttrib && !inAttribValue)
                        {
                            attribName = token.ToString(); // tagName;
                            if (!string.IsNullOrEmpty(attribName))
                            {
                                // If the attribute has no value, store it against empty.
                                properties[tagName + "." + attribName] = string.Empty;
                                attribName = string.Empty;
                            }

                            inAttrib = false;
                            hasAttribute = true;
                        }
                        else if (inAttrib)
                        {
                            attribValue = token.ToString();
                            var blank = attribValue.Equals("NOT_IMPLEMENTED", StringComparison.Ordinal);
                            properties[tagName + "." + attribName] = blank ? string.Empty : attribValue;
                            inAttribValue = false;
                            inAttrib = false;
                            attribName = string.Empty;
                            attribValue = string.Empty;
                            hasAttribute = true;
                        }
                        else if (inTag)
                        {
                            // If we haven't already got the tag's name.
                            if (string.IsNullOrEmpty(tagName))
                            {
                                tagName = token.ToString();
                            }

                            if (!string.IsNullOrEmpty(tagName) && !properties.TryGetValue(tagName, out _))
                            {
                                properties[tagName] = string.Empty;
                            }

                            if (isClosed)
                            {
                                lastTag = tagName;
                                tagName = string.Empty;
                                isClosed = false;
                            }

                            inTag = false;
                        }

                        token.Clear();
                        a++;
                        lastChar = c;
                        continue;

                    case ' ': // doesn't support single name-only attributes.
                        if (inTag)
                        {
                            if (!inAttrib && !hasAttribute)
                            {
                                // End of tagName marker.
                                tagName = token.ToString();
                                token.Clear();
                                inAttrib = true;
                            }
                            else if (!inAttrib)
                            {
                                inAttrib = true;
                            }
                            else if (inAttribValue)
                            {
                                break;
                            }

                            a++;
                            lastChar = c;
                            continue;
                        }

                        break;

                    case '=':
                        if (inTag && inAttrib & !inAttribValue)
                        {
                            attribName = token.ToString();
                            token.Clear();
                            a++;
                            hasAttribute = true;
                            lastChar = c;
                            continue;
                        }

                        break;

                    case '"':
                        if (inTag && inAttrib)
                        {
                            if (!inAttribValue)
                            {
                                inAttribValue = true;
                            }
                            else
                            {
                                attribValue = token.ToString();
                                token.Clear();
                                var blank = attribValue.Equals("NOT_IMPLEMENTED", StringComparison.Ordinal);
                                properties[tagName + "." + attribName] = blank ? string.Empty : attribValue;
                                attribValue = string.Empty;
                                attribName = string.Empty;
                                hasAttribute = true;
                                inAttribValue = false;
                                inAttrib = false;
                            }

                            a++;
                            lastChar = c;
                            continue;
                        }

                        break;
                }

                token.Append(c);
                a++;
                lastChar = c;
            }

            return properties.Count > 0;
        }
    }
}
