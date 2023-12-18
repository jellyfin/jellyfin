namespace Jellyfin.Data.Enums;

/// <summary>
/// The person kind.
/// </summary>
public enum PersonKind
{
    /// <summary>
    /// An unknown person kind.
    /// </summary>
    Unknown,

    /// <summary>
    /// A person whose profession is acting on the stage, in films, or on television.
    /// </summary>
    Actor,

    /// <summary>
    /// A person who supervises the actors and other staff in a film, play, or similar production.
    /// </summary>
    Director,

    /// <summary>
    /// A person who writes music, especially as a professional occupation.
    /// </summary>
    Composer,

    /// <summary>
    /// A writer of a book, article, or document. Can also be used as a generic term for music writer if there is a lack of specificity.
    /// </summary>
    Writer,

    /// <summary>
    /// A well-known actor or other performer who appears in a work in which they do not have a regular role.
    /// </summary>
    GuestStar,

    /// <summary>
    /// A person responsible for the financial and managerial aspects of the making of a film or broadcast or for staging a play, opera, etc.
    /// </summary>
    Producer,

    /// <summary>
    /// A person who directs the performance of an orchestra or choir.
    /// </summary>
    Conductor,

    /// <summary>
    /// A person who writes the words to a song or musical.
    /// </summary>
    Lyricist,

    /// <summary>
    /// A person who adapts a musical composition for performance.
    /// </summary>
    Arranger,

    /// <summary>
    /// An audio engineer who performed a general engineering role.
    /// </summary>
    Engineer,

    /// <summary>
    /// An engineer responsible for using a mixing console to mix a recorded track into a single piece of music suitable for release.
    /// </summary>
    Mixer,

    /// <summary>
    /// A person who remixed a recording by taking one or more other tracks, substantially altering them and mixing them together with other material.
    /// </summary>
    Remixer,

    /// <summary>
    /// A person who created the material.
    /// </summary>
    Creator,

    /// <summary>
    /// A person who was the artist.
    /// </summary>
    Artist,

    /// <summary>
    /// A person who was the album artist.
    /// </summary>
    AlbumArtist,

    /// <summary>
    /// A person who was the author.
    /// </summary>
    Author,

    /// <summary>
    /// A person who was the illustrator.
    /// </summary>
    Illustrator,

    /// <summary>
    /// A person responsible for drawing the art.
    /// </summary>
    Penciller,

    /// <summary>
    /// A person responsible for inking the pencil art.
    /// </summary>
    Inker,

    /// <summary>
    /// A person responsible for applying color to drawings.
    /// </summary>
    Colorist,

    /// <summary>
    /// A person responsible for drawing text and speech bubbles.
    /// </summary>
    Letterer,

    /// <summary>
    /// A person responsible for drawing the cover art.
    /// </summary>
    CoverArtist,

    /// <summary>
    /// A person contributing to a resource by revising or elucidating the content, e.g., adding an introduction, notes, or other critical matter.
    /// An editor may also prepare a resource for production, publication, or distribution.
    /// </summary>
    Editor,

    /// <summary>
    /// A person who renders a text from one language into another.
    /// </summary>
    Translator
}
