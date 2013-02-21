using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Sorting {
    /// <summary>
    /// Class BaseItemComparer
    /// </summary>
    public class BaseItemComparer : IComparer<BaseItem> {
        /// <summary>
        /// The _order
        /// </summary>
        private readonly SortOrder _order;
        /// <summary>
        /// The _property name
        /// </summary>
        private readonly string _propertyName;
        /// <summary>
        /// The _compare culture
        /// </summary>
        private readonly StringComparison _compareCulture = StringComparison.CurrentCultureIgnoreCase;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        private ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemComparer" /> class.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="logger">The logger.</param>
        public BaseItemComparer(SortOrder order, ILogger logger) {
            _order = order;
            Logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemComparer" /> class.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="compare">The compare.</param>
        /// <param name="logger">The logger.</param>
        public BaseItemComparer(SortOrder order, StringComparison compare, ILogger logger)
        {
            _order = order;
            _compareCulture = compare;
            Logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemComparer" /> class.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="logger">The logger.</param>
        public BaseItemComparer(string property, ILogger logger)
        {
            _order = SortOrder.Custom;
            _propertyName = property;
            Logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseItemComparer" /> class.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="compare">The compare.</param>
        /// <param name="logger">The logger.</param>
        public BaseItemComparer(string property, StringComparison compare, ILogger logger)
        {
            _order = SortOrder.Custom;
            _propertyName = property;
            _compareCulture = compare;
            Logger = logger;
        }

        #region IComparer<BaseItem> Members

        /// <summary>
        /// Compares the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Int32.</returns>
        public int Compare(BaseItem x, BaseItem y) {
            int compare = 0;

            switch (_order) {

                case SortOrder.Date:
                    compare = -x.DateCreated.CompareTo(y.DateCreated);
                    break;

                case SortOrder.Year:

                    var xProductionYear = x.ProductionYear ?? 0;
                    var yProductionYear = y.ProductionYear ?? 0;


                    compare = yProductionYear.CompareTo(xProductionYear); 
                    break;

                case SortOrder.Rating:

                    var xRating = x.CommunityRating ?? 0;
                    var yRating = y.CommunityRating ?? 0;

                    compare = yRating.CompareTo(xRating);
                    break;

                case SortOrder.Runtime:
                    var xRuntime = x.RunTimeTicks ?? 0;
                    var yRuntime = y.RunTimeTicks ?? 0;

                    compare = xRuntime.CompareTo(yRuntime);
                    break;

                case SortOrder.Custom:

                    Logger.Debug("Sorting on custom field " + _propertyName);
                    var yProp = y.GetType().GetProperty(_propertyName);
                    var xProp = x.GetType().GetProperty(_propertyName);
                    if (yProp == null || xProp == null) break;
                    var yVal = yProp.GetValue(y, null);
                    var xVal = xProp.GetValue(x,null);
                    if (yVal == null && xVal == null) break;
                    if (yVal == null) return 1;
                    if (xVal == null) return -1;
                    compare = String.Compare(xVal.ToString(), yVal.ToString(),_compareCulture);
                    break;

                default:
                    compare = 0;
                    break;
            }

            if (compare == 0) {

                var name1 = x.SortName ?? x.Name ?? "";
                var name2 = y.SortName ?? y.Name ?? "";

                //if (Config.Instance.EnableAlphanumericSorting)
                    compare = AlphaNumericCompare(name1, name2,_compareCulture);
                //else
                //    compare = String.Compare(name1,name2,_compareCulture);
            }

            return compare;
        }


        #endregion

        /// <summary>
        /// Alphas the numeric compare.
        /// </summary>
        /// <param name="s1">The s1.</param>
        /// <param name="s2">The s2.</param>
        /// <param name="compareCulture">The compare culture.</param>
        /// <returns>System.Int32.</returns>
        private int AlphaNumericCompare(string s1, string s2, StringComparison compareCulture) {
            // http://dotnetperls.com/Content/Alphanumeric-Sorting.aspx

            int len1 = s1.Length;
            int len2 = s2.Length;
            int marker1 = 0;
            int marker2 = 0;

            // Walk through two the strings with two markers.
            while (marker1 < len1 && marker2 < len2) {
                char ch1 = s1[marker1];
                char ch2 = s2[marker2];

                // Some buffers we can build up characters in for each chunk.
                var space1 = new char[len1];
                var loc1 = 0;
                var space2 = new char[len2];
                var loc2 = 0;

                // Walk through all following characters that are digits or
                // characters in BOTH strings starting at the appropriate marker.
                // Collect char arrays.
                do {
                    space1[loc1++] = ch1;
                    marker1++;

                    if (marker1 < len1) {
                        ch1 = s1[marker1];
                    } else {
                        break;
                    }
                } while (char.IsDigit(ch1) == char.IsDigit(space1[0]));

                do {
                    space2[loc2++] = ch2;
                    marker2++;

                    if (marker2 < len2) {
                        ch2 = s2[marker2];
                    } else {
                        break;
                    }
                } while (char.IsDigit(ch2) == char.IsDigit(space2[0]));

                // If we have collected numbers, compare them numerically.
                // Otherwise, if we have strings, compare them alphabetically.
                var str1 = new string(space1);
                var str2 = new string(space2);
                
                var result = 0;

                //biggest int - 2147483647
                if (char.IsDigit(space1[0]) && char.IsDigit(space2[0]) /*&& str1.Length < 10 && str2.Length < 10*/) //this assumed the entire string was a number...
                {
                    int thisNumericChunk;
                    var isValid = false;

                    if (int.TryParse(str1.Substring(0, str1.Length > 9 ? 10 : str1.Length), out thisNumericChunk))
                    {
                        int thatNumericChunk;
                        
                        if (int.TryParse(str2.Substring(0, str2.Length > 9 ? 10 : str2.Length), out thatNumericChunk))
                        {
                            isValid = true;
                            result = thisNumericChunk.CompareTo(thatNumericChunk);
                        }
                    }
                    
                    if (!isValid)
                    {
                        Logger.Error("Error comparing numeric strings: " + str1 + "/" + str2);
                        result = String.Compare(str1, str2, compareCulture);
                    }
                    
                } else {
                    result = String.Compare(str1,str2,compareCulture);
                }

                if (result != 0) {
                    return result;
                }
            }
            return len1 - len2;
        }
    }
}
