namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// The linked child type.
    /// </summary>
    public enum LinkedChildType
    {
        /// <summary>
        /// Manually linked child.
        /// </summary>
        Manual = 0,

        /// <summary>
        /// Shortcut linked child.
        /// </summary>
        Shortcut = 1,

        /// <summary>
        /// Local alternate version (same item, different file path).
        /// </summary>
        LocalAlternateVersion = 2,

        /// <summary>
        /// Linked alternate version (different item ID).
        /// </summary>
        LinkedAlternateVersion = 3,

        /// <summary>
        /// Linked alternate version (different item ID) created automatically by the library scan,
        /// e.g. same-numbered episodes of a series spread across multiple folders. Managed by the
        /// scan rather than the user, so it is re-evaluated on every scan instead of being treated
        /// as a manual merge.
        /// </summary>
        AutoLinkedAlternateVersion = 4
    }
}
