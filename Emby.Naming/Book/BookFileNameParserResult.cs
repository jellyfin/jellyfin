using System;

namespace Emby.Naming.Book
{
    /// <summary>
    /// Data object used to pass metadata parsed from a book filename.
    /// </summary>
    public class BookFileNameParserResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BookFileNameParserResult"/> class.
        /// </summary>
        public BookFileNameParserResult()
        {
            Name = null;
            Index = null;
            Year = null;
            SeriesName = null;
        }

        /// <summary>
        /// Gets or sets the name of the book.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the book index.
        /// </summary>
        public int? Index { get; set; }

        /// <summary>
        /// Gets or sets the publication year.
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// Gets or sets the series name.
        /// </summary>
        public string? SeriesName { get; set; }
    }
}
