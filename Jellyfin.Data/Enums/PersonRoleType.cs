namespace Jellyfin.Data.Enums
{
    /// <summary>
    /// An enum representing a person's role in a specific media item.
    /// </summary>
    public enum PersonRoleType
    {
        /// <summary>
        /// Another role, not covered by the other types.
        /// </summary>
        Other = 0,

        /// <summary>
        /// The director of the media.
        /// </summary>
        Director = 1,

        /// <summary>
        /// An artist.
        /// </summary>
        Artist = 2,

        /// <summary>
        /// The original artist.
        /// </summary>
        OriginalArtist = 3,

        /// <summary>
        /// An actor.
        /// </summary>
        Actor = 4,

        /// <summary>
        /// A voice actor.
        /// </summary>
        VoiceActor = 5,

        /// <summary>
        /// A producer.
        /// </summary>
        Producer = 6,

        /// <summary>
        /// A remixer.
        /// </summary>
        Remixer = 7,

        /// <summary>
        /// A conductor.
        /// </summary>
        Conductor = 8,

        /// <summary>
        /// A composer.
        /// </summary>
        Composer = 9,

        /// <summary>
        /// An author.
        /// </summary>
        Author = 10,

        /// <summary>
        /// An editor.
        /// </summary>
        Editor = 11
    }
}
