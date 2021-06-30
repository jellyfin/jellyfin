using System.Collections.Generic;
using System.Text;

namespace Jellyfin.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="StringBuilder"/> class.
    /// </summary>
    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Concatenates and appends the members of a collection in single quotes using the specified delimiter.
        /// </summary>
        /// <param name="builder">The string builder.</param>
        /// <param name="delimiter">The character delimiter.</param>
        /// <param name="values">The collection of strings to concatenate.</param>
        /// <returns>The updated string builder.</returns>
        public static StringBuilder AppendJoinInSingleQuotes(this StringBuilder builder, char delimiter, IReadOnlyList<string> values)
        {
            var len = values.Count;
            for (var i = 0; i < len; i++)
            {
                builder.Append('\'')
                    .Append(values[i])
                    .Append('\'')
                    .Append(delimiter);
            }

            // remove last ,
            builder.Length--;

            return builder;
        }
    }
}
