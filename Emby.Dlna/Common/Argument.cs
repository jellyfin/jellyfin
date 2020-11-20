namespace Emby.Dlna.Common
{
    /// <summary>
    /// DLNA Query parameter type, used when querying DLNA devices via SOAP.
    /// </summary>
    public class Argument
    {
        /// <summary>
        /// Gets or sets name of the DLNA argument.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the direction of the parameter.
        /// </summary>
        public string Direction { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the related DLNA state variable for this argument.
        /// </summary>
        public string RelatedStateVariable { get; set; } = string.Empty;
    }
}
