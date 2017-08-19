using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Interface IHasItemFields
    /// </summary>
    public interface IHasItemFields
    {
        /// <summary>
        /// Gets or sets the fields.
        /// </summary>
        /// <value>The fields.</value>
        string Fields { get; set; }
    }

    /// <summary>
    /// Class ItemFieldsExtensions.
    /// </summary>
    public static class ItemFieldsExtensions
    {
        /// <summary>
        /// Gets the item fields.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable{ItemFields}.</returns>
        public static ItemFields[] GetItemFields(this IHasItemFields request)
        {
            var val = request.Fields;

            if (string.IsNullOrEmpty(val))
            {
                return new ItemFields[] { };
            }

            return val.Split(',').Select(v =>
            {
                ItemFields value;

                if (Enum.TryParse(v, true, out value))
                {
                    return (ItemFields?)value;
                }
                return null;

            }).Where(i => i.HasValue).Select(i => i.Value).ToArray();
        }
    }
}
