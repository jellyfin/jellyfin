namespace Jellyfin.Api.Models.ClientLogDtos;

/// <summary>
/// Client log document response dto.
/// </summary>
public class ClientLogDocumentResponseDto
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClientLogDocumentResponseDto"/> class.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    public ClientLogDocumentResponseDto(string fileName)
    {
        FileName = fileName;
    }

    /// <summary>
    /// Gets the resulting filename.
    /// </summary>
    public string FileName { get; }
}
