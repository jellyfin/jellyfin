using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Api.HttpHandlers
{
    public class StudiosHandler : BaseJsonHandler<IEnumerable<IBNItem<Studio>>>
    {
        protected override IEnumerable<IBNItem<Studio>> GetObjectToSerialize()
        {
            Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            Guid userId = Guid.Parse(QueryString["userid"]);
            User user = Kernel.Instance.Users.First(u => u.Id == userId);

            return Kernel.Instance.GetAllStudios(parent, user);
        }
    }
}
