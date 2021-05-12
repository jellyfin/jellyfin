using System;
using System.Text.RegularExpressions;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="DeviceDetails"/>.
    /// </summary>
    public class DeviceDetails
    {
        /// <summary>
        /// Gets or sets the ip address.
        ///
        /// If the instance is a <see cref="DeviceProfile"/> this is the address of the device that last used the profile.
        /// If the instance is a <see cref="DeviceIdentification"/> this address is used for client ip matching purposes.
        /// </summary>
        public string? Address { get; set; }

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
        /// Copies settings from another <see cref="DeviceDetails"/>.
        /// </summary>
        /// <param name="deviceInfo">Source <see cref="DeviceDetails"/> instance.</param>
        public void CopyFrom(DeviceDetails deviceInfo)
        {
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
        /// <returns>The number of comparison matches, or zero if none.</returns>
        /// <exception cref="ArgumentException">Raised if the regular expression is invalid.</exception>
        public int Matches(DeviceDetails profileInfo)
        {
            int matchRating = 0;
            if (IsRegexOrSubstringMatch(Address, profileInfo.Address, false)
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

            return 0;

            // <summary>
            // Performs an match, or regular expression match between <paramref name="input"/> and <paramref name="pattern"/>.
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
                        matchRating += 10;
                        return true;
                    }

                    if (permitRegex && Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                    {
                        matchRating++;
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
    }
}
