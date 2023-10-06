using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Extensions;

/// <summary>
/// Provides extension methods for <see cref="XmlReader"/> to parse <see cref="BaseItem"/>'s.
/// </summary>
public static class XmlReaderExtensions
{
    /// <summary>
    /// Parses a <see cref="DateTime"/> from the current node.
    /// </summary>
    /// <param name="reader">The <see cref="XmlReader"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/> to use on failure.</param>
    /// <param name="value">The parsed <see cref="DateTime"/>.</param>
    /// <returns>A value indicating whether the parsing succeeded.</returns>
    public static bool TryReadDateTime(this XmlReader reader, ILogger logger, out DateTime value)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(logger);

        var text = reader.ReadElementContentAsString();
        if (DateTime.TryParse(
                text,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out value))
        {
            return true;
        }

        logger.LogWarning("Invalid date: {Date}", text);
        return false;
    }

    /// <summary>
    /// Parses a <see cref="DateTime"/> from the current node.
    /// </summary>
    /// <param name="reader">The <see cref="XmlReader"/>.</param>
    /// <param name="formatString">The date format string.</param>
    /// <param name="value">The parsed <see cref="DateTime"/>.</param>
    /// <returns>A value indicating whether the parsing succeeded.</returns>
    public static bool TryReadDateTimeExact(this XmlReader reader, string formatString, out DateTime value)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(formatString);

        return DateTime.TryParseExact(
            reader.ReadElementContentAsString(),
            formatString,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out value);
    }

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

    /// <summary>
    /// Used to split names of comma or pipe delimited genres and people.
    /// </summary>
    /// <param name="reader">The <see cref="XmlReader"/>.</param>
    /// <returns>IEnumerable{System.String}.</returns>
    public static IEnumerable<string> GetStringArray(this XmlReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var value = reader.ReadElementContentAsString();

        // Only split by comma if there is no pipe in the string
        // We have to be careful to not split names like Matthew, Jr.
        var separator = !value.Contains('|', StringComparison.Ordinal)
            && !value.Contains(';', StringComparison.Ordinal)
                ? new[] { ',' }
                : new[] { '|', ';' };

        foreach (var part in value.Trim().Trim(separator).Split(separator))
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                yield return part.Trim();
            }
        }
    }

    /// <summary>
    /// Parses a <see cref="PersonInfo"/> array from the xml node.
    /// </summary>
    /// <param name="reader">The <see cref="XmlReader"/>.</param>
    /// <param name="personKind">The <see cref="PersonKind"/>.</param>
    /// <returns>The <see cref="IEnumerable{PersonInfo}"/>.</returns>
    public static IEnumerable<PersonInfo> GetPersonArray(this XmlReader reader, PersonKind personKind)
        => reader.GetStringArray()
            .Select(part => new PersonInfo { Name = part, Type = personKind });
}
