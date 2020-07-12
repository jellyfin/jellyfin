using System;

namespace Emby.Dlna.PlayTo
{
    public class DlnaEventArgs
    {
        public DlnaEventArgs(string id, string response)
        {
            Id = id;
            Response = response;
        }

        public string Id { get; }

        public string Response { get; }
    }
}
