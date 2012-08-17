using System;
using System.Collections.Generic;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class GenresHandler : BaseJsonHandler<IEnumerable<CategoryInfo<Genre>>>
    {
        protected override IEnumerable<CategoryInfo<Genre>> GetObjectToSerialize()
        {
            Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            Guid userId = Guid.Parse(QueryString["userid"]);

            return Kernel.Instance.GetAllGenres(parent, userId);
        }
    }
}
