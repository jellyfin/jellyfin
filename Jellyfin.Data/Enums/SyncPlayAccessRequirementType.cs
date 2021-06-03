namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// Enum SyncPlayAccessRequirementType.
    /// </summary>
    public enum SyncPlayAccessRequirementType
    {
        /// <summary>
        /// User must have access to SyncPlay, in some form.
        /// </summary>
        HasAccess = 0,

        /// <summary>
        /// User must be able to create groups.
        /// </summary>
        CreateGroup = 1,

        /// <summary>
        /// User must be able to join groups.
        /// </summary>
        JoinGroup = 2,

        /// <summary>
        /// User must be in a group.
        /// </summary>
        IsInGroup = 3
    }
}
