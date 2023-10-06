using System;
using System.Globalization;
using System.Xml;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Extensions;

/// <summary>
/// Provides extension methods for <see cref="XmlReader"/> to parse <see cref="BaseItem"/>'s.
/// </summary>
public static class XmlReaderExtensions
{
    /// <summary>
    /// Parses a <see cref="PersonInfo"/> from the xml node.
    /// </summary>
    /// <param name="reader">The <see cref="XmlReader"/>.</param>
    /// <returns>A <see cref="PersonInfo"/>, or <c>null</c> if none is found.</returns>
    public static PersonInfo? GetPersonFromXmlNode(this XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        if (reader.IsEmptyElement)
        {
            reader.Read();
            return null;
        }

        var name = string.Empty;
        var type = PersonKind.Actor;  // If type is not specified assume actor
        var role = string.Empty;
        int? sortOrder = null;
        string? imageUrl = null;

        using var subtree = reader.ReadSubtree();
        subtree.MoveToContent();
        subtree.Read();

        while (subtree is { EOF: false, ReadState: ReadState.Interactive })
        {
            if (subtree.NodeType != XmlNodeType.Element)
            {
                subtree.Read();
                continue;
            }

            switch (subtree.Name)
            {
                case "name":
                case "Name":
                    name = subtree.ReadElementContentAsString();
                    break;
                case "role":
                case "Role":
                    var roleValue = subtree.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(roleValue))
                    {
                        role = roleValue;
                    }

                    break;
                case "type":
                case "Type":
                    Enum.TryParse(subtree.ReadElementContentAsString(), true, out type);
                    break;
                case "order":
                case "sortorder":
                case "SortOrder":
                    if (int.TryParse(
                        subtree.ReadElementContentAsString(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var intVal))
                    {
                        sortOrder = intVal;
                    }

                    break;
                case "thumb":
                    var thumb = subtree.ReadElementContentAsString();
                    if (!string.IsNullOrWhiteSpace(thumb))
                    {
                        imageUrl = thumb;
                    }

                    break;
                default:
                    subtree.Skip();
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return new PersonInfo
        {
            Name = name.Trim(),
            Role = role,
            Type = type,
            SortOrder = sortOrder,
            ImageUrl = imageUrl
        };
    }
}
