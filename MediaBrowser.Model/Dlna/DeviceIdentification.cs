using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="DeviceIdentification" />.
    /// </summary>
    [XmlRoot("Profile")]
    public class DeviceIdentification
    {
        /// <summary>
        /// Gets or sets the ip address.
        /// </summary>
        /// <remarks>
        /// If the instance is a <see cref="DeviceIdentification"/> this address is used for client ip matching purposes.
        /// </remarks>
        public string? Address { get; set; }

        /// <summary>
        /// Gets or sets the profile name.
        /// </summary>
        public string? ProfileName { get; set; }

        /// <summary>
        /// Gets or sets the friendly name of the device profile, which can be shown to users.
        /// </summary>
        public string? FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the manufacturer of the device which this profile represents.
        /// </summary>
        public string? Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets an url for the manufacturer of the device which this profile represents.
        /// </summary>
        public string? ManufacturerUrl { get; set; }

        /// <summary>
        /// Gets or sets the model name of the device which this profile represents.
        /// </summary>
        public string? ModelName { get; set; }

        /// <summary>
        /// Gets or sets the model description of the device which this profile represents.
        /// </summary>
        public string? ModelDescription { get; set; }

        /// <summary>
        /// Gets or sets the model number of the device which this profile represents.
        /// </summary>
        public string? ModelNumber { get; set; }

        /// <summary>
        /// Gets or sets the Model Url of the device which this profile represents.
        /// </summary>
        public string? ModelUrl { get; set; }

        /// <summary>
        /// Gets or sets the serial number of the device which this profile represents.
        /// </summary>
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the headers.
        /// </summary>
        /// <value>The headers.</value>
        public HttpHeaderInfo[] Headers { get; set; } = Array.Empty<HttpHeaderInfo>();

        /// <summary>
        /// Compares this instance against <paramref name="headers"/>.
        /// </summary>
        /// <param name="headers">The <see cref="IHeaderDictionary"/> instance to match against.</param>
        /// <param name="addrString">The ip address of the device.</param>
        /// <returns>A weighted number representing the match, or zero if none.</returns>
        public int Matches(IHeaderDictionary headers, string addrString)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            if (string.Equals(Address, addrString, StringComparison.Ordinal))
            {
                return ProfileComparison.IpMatch; // IP Match cannot be beaten by header matches.
            }

            int matchRating = 0;
            for (int i = 0; i < Headers.Length; i++)
            {
                var headerRating = IsMatch(headers, Headers[i]);
                if (headerRating == ProfileComparison.NoMatch)
                {
                    return ProfileComparison.NoMatch;
                }

                matchRating += headerRating;
            }

            return matchRating;
        }

        /// <summary>
        /// Copies all the properties from <see cref="DeviceIdentification"/>.
        /// </summary>
        /// <param name="deviceInfo">Source <see cref="DeviceIdentification"/> instance.</param>
        public void CopyFrom(DeviceIdentification deviceInfo)
        {
            if (deviceInfo == null)
            {
                throw new ArgumentNullException(nameof(deviceInfo));
            }

            ProfileName = deviceInfo.ProfileName;
            Address = deviceInfo.Address;
            Manufacturer = deviceInfo.Manufacturer;
            FriendlyName = deviceInfo.FriendlyName;
            ModelNumber = deviceInfo.ModelNumber;
            ModelName = deviceInfo.ModelName;
            ModelUrl = deviceInfo.ModelUrl;
            ModelDescription = deviceInfo.ModelDescription;
            ManufacturerUrl = deviceInfo.ManufacturerUrl;
            SerialNumber = deviceInfo.SerialNumber;
        }

        /// <summary>
        /// Compares this instance with <paramref name="profileInfo"/>.
        /// </summary>
        /// <param name="profileInfo">The <paramref name="profileInfo"/> instance to match against.</param>
        /// <returns>The rating of the comparison matches, or zero if none.</returns>
        /// <exception cref="ArgumentException">Raised if the regular expression is invalid.</exception>
        public int Matches(DeviceIdentification profileInfo)
        {
            if (profileInfo == null)
            {
                return ProfileComparison.NoMatch;
            }

            int matchRating = ProfileComparison.NoMatch;
            if (IsRegexOrSubstringMatch(Address, profileInfo.Address, false)
                && IsRegexOrSubstringMatch(ProfileName, profileInfo.ProfileName)
                && IsRegexOrSubstringMatch(FriendlyName, profileInfo.FriendlyName)
                && IsRegexOrSubstringMatch(Manufacturer, profileInfo.Manufacturer)
                && IsRegexOrSubstringMatch(ManufacturerUrl, profileInfo.ManufacturerUrl)
                && IsRegexOrSubstringMatch(ModelDescription, profileInfo.ModelDescription)
                && IsRegexOrSubstringMatch(ModelName, profileInfo.ModelName)
                && IsRegexOrSubstringMatch(ModelNumber, profileInfo.ModelNumber)
                && IsRegexOrSubstringMatch(ModelUrl, profileInfo.ModelUrl)
                && IsRegexOrSubstringMatch(SerialNumber, profileInfo.SerialNumber)
            )
            {
                return matchRating;
            }

            return ProfileComparison.NoMatch;

            // <summary>
            // Local method that performs a match, or regular expression match between <paramref name="input"/> and <paramref name="pattern"/>.
            // </summary>
            // <param name="input">The source string.</param>
            // <param name="pattern">The destination string, or regular expression.</param>
            // <param name="permitRegex">When true, Regex will not be attempted</param>
            // <returns><c>True</c> if the parameters match.</returns>
            // <exception cref="ArgumentException">Raised if the regular expression is invalid.</exception>
            bool IsRegexOrSubstringMatch(string? input, string? pattern, bool permitRegex = true)
            {
                if (string.IsNullOrEmpty(pattern))
                {
                    // In profile identification: An empty pattern matches anything.
                    return true;
                }

                if (string.IsNullOrEmpty(input))
                {
                    // The profile contains a value, and the device doesn't.
                    return false;
                }

                try
                {
                    if (input.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                    {
                        // Equal matches result in a higher rating then regular expression matches.
                        matchRating += ProfileComparison.ExactMatch;
                        return true;
                    }

                    if (permitRegex && Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    {
                        matchRating += ProfileComparison.RegExMatch;
                        return true;
                    }

                    return false;
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException("Error evaluating regex pattern {Pattern}", pattern, ex);
                }
            }
        }

        /// <summary>
        /// Compares the information in <paramref name="headers"/> and <paramref name="header"/> to see if there is a match.
        /// </summary>
        /// <param name="headers">A <see cref="IHeaderDictionary"/> instance.</param>
        /// <param name="header">A <see cref="HttpHeaderInfo"/> instance.</param>
        /// <returns><c>True</c> if they match.</returns>
        private static int IsMatch(IHeaderDictionary headers, HttpHeaderInfo header)
        {
            // Handle invalid user setup
            if (string.IsNullOrEmpty(header.Name))
            {
                return ProfileComparison.NoMatch;
            }

            if (!headers.TryGetValue(header.Name, out StringValues value))
            {
                return ProfileComparison.NoMatch;
            }

            switch (header.Match)
            {
                case HeaderMatchType.Equals:
                    return string.Equals(value, header.Value, StringComparison.OrdinalIgnoreCase) ? ProfileComparison.ExactMatch : ProfileComparison.NoMatch;

                case HeaderMatchType.Substring:
                    var isMatch = value.ToString().IndexOf(header.Value, StringComparison.OrdinalIgnoreCase) != -1;
                    return isMatch ? ProfileComparison.SubStringMatch : ProfileComparison.NoMatch;

                case HeaderMatchType.Regex:
                    return Regex.IsMatch(value, header.Value, RegexOptions.IgnoreCase) ? ProfileComparison.RegExMatch : ProfileComparison.NoMatch;

                default:
                    throw new ArgumentException("Unrecognized HeaderMatchType");
            }
        }
    }
}
