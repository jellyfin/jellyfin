namespace MediaBrowser.Model.QuickConnect
{
    /// <summary>
    /// Quick connect state.
    /// </summary>
    public enum QuickConnectState
    {
        /// <summary>
        /// This feature has not been opted into and is unavailable until the server administrator chooses to opt-in.
        /// </summary>
        Unavailable,

        /// <summary>
        /// The feature is enabled for use on the server but is not currently accepting connection requests.
        /// </summary>
        Available,

        /// <summary>
        /// The feature is actively accepting connection requests.
        /// </summary>
        Active
    }
}
