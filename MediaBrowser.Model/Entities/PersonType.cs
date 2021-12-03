namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Types of persons.
    /// </summary>
    public static class PersonType
    {
        /// <summary>
        /// A person whose profession is acting on the stage, in films, or on television.
        /// </summary>
        public const string Actor = "Actor";

        /// <summary>
        /// A person who supervises the actors and other staff in a film, play, or similar production.
        /// </summary>
        public const string Director = "Director";

        /// <summary>
        /// A person who writes music, especially as a professional occupation.
        /// </summary>
        public const string Composer = "Composer";

        /// <summary>
        /// A writer of a book, article, or document. Can also be used as a generic term for music writer if there is a lack of specificity.
        /// </summary>
        public const string Writer = "Writer";

        /// <summary>
        /// A well-known actor or other performer who appears in a work in which they do not have a regular role.
        /// </summary>
        public const string GuestStar = "GuestStar";

        /// <summary>
        /// A person responsible for the financial and managerial aspects of the making of a film or broadcast or for staging a play, opera, etc.
        /// </summary>
        public const string Producer = "Producer";

        /// <summary>
        /// A person who directs the performance of an orchestra or choir.
        /// </summary>
        public const string Conductor = "Conductor";

        /// <summary>
        /// A person who writes the words to a song or musical.
        /// </summary>
        public const string Lyricist = "Lyricist";

        /// <summary>
        /// A person who adapts a musical composition for performance.
        /// </summary>
        public const string Arranger = "Arranger";

        /// <summary>
        /// An audio engineer who performed a general engineering role.
        /// </summary>
        public const string Engineer = "Engineer";

        /// <summary>
        /// An engineer responsible for using a mixing console to mix a recorded track into a single piece of music suitable for release.
        /// </summary>
        public const string Mixer = "Mixer";

        /// <summary>
        /// A person who remixed a recording by taking one or more other tracks, substantially altering them and mixing them together with other material.
        /// </summary>
        public const string Remixer = "Remixer";
    }
}
