using System;
using System.IO;

namespace Jellyfin.Extensions;

/// <summary>
/// A custom StreamWriter which supports setting a IFormatProvider.
/// </summary>
public class FormattingStreamWriter : StreamWriter
{
    private readonly IFormatProvider _formatProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormattingStreamWriter"/> class.
    /// </summary>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="formatProvider">The format provider to use.</param>
    public FormattingStreamWriter(Stream stream, IFormatProvider formatProvider)
        : base(stream)
    {
        _formatProvider = formatProvider;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormattingStreamWriter"/> class.
    /// </summary>
    /// <param name="path">The complete file path to write to.</param>
    /// <param name="formatProvider">The format provider to use.</param>
    public FormattingStreamWriter(string path, IFormatProvider formatProvider)
        : base(path)
    {
        _formatProvider = formatProvider;
    }

    /// <inheritdoc />
    public override IFormatProvider FormatProvider
        => _formatProvider;
}
