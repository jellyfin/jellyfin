using MediaBrowser.Controller.Net;
using System;
using System.IO;
using System.IO.Compression;
using MediaBrowser.Common.Json;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller;

namespace MediaBrowser.Api.HttpHandlers
{
    public class ItemHandler : Response
    {
        public ItemHandler(RequestContext ctx)
            : base(ctx)
        {
            ContentType = "application/json";

            Headers["Content-Encoding"] = "gzip";

            WriteStream = s =>
            {
                WriteReponse(s);
                s.Close();
            };
        }

        private Guid ItemId
        {
            get
            {
                string id = RequestContext.Request.QueryString["id"];

                if (string.IsNullOrEmpty(id))
                {
                    return Guid.Empty;
                }

                return Guid.Parse(id);
            }
        }

        BaseItem Item
        {
            get
            {
                Guid id = ItemId;

                if (id == Guid.Empty)
                {
                    return Kernel.Instance.RootFolder;
                }

                return Kernel.Instance.RootFolder.FindById(id);
            }
        }

        private void WriteReponse(Stream stream)
        {
            BaseItem item = Item;

            object returnObject;

            Folder folder = item as Folder;

            if (folder != null)
            {
                returnObject = new
                {
                    Item = item,
                    Children = folder.Children
                };
            }
            else
            {
                returnObject = new
                {
                    Item = item
                };
            }

            WriteJsonResponse(returnObject, stream);
        }

        private void WriteJsonResponse(object obj, Stream stream)
        {
            using (GZipStream gzipStream = new GZipStream(stream, CompressionMode.Compress, false))
            {
                JsonSerializer.Serialize(obj, gzipStream);
                //gzipStream.Flush();
            }
        }
    }
}
