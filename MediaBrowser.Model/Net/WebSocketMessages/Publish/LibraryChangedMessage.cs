using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Session;

namespace MediaBrowser.Model.Net.WebSocketMessages.Publish;

/// <summary>
/// Library changed message.
/// </summary>
public class LibraryChangedMessage : WebSocketMessage<LibraryUpdateInfo>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryChangedMessage"/> class.
    /// </summary>
    /// <param name="data">The library update info.</param>
    public LibraryChangedMessage(LibraryUpdateInfo data)
        : base(data)
    {
    }

    /// <inheritdoc />
    public override SessionMessageType MessageType => SessionMessageType.LibraryChanged;
}
