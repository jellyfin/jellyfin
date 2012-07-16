using System;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class StudiosHandler : JsonHandler
    {
        public StudiosHandler(RequestContext ctx)
            : base(ctx)
        {
        }

        protected sealed override object ObjectToSerialize
        {
            get
            {
                Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;
                Guid userId = Guid.Parse(QueryString["userid"]);

                return ApiService.GetAllStudios(parent, userId);
            }
        }
    }
}
