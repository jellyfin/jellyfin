using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    [Export(typeof(BaseHandler))]
    public class ItemListHandler : BaseSerializationHandler<DtoBaseItem[]>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("itemlist", request);
        }

        protected override Task<DtoBaseItem[]> GetObjectToSerialize()
        {
            User user = ApiService.GetUserById(QueryString["userid"], true);

            return Task.WhenAll(GetItemsToSerialize(user).Select(i => ApiService.GetDtoBaseItem(i, user, includeChildren: false, includePeople: false)));
        }

        private IEnumerable<BaseItem> GetItemsToSerialize(User user)
        {
            var parent = ApiService.GetItemById(ItemId) as Folder;

            if (ListType.Equals("inprogressitems", StringComparison.OrdinalIgnoreCase))
            {
                return parent.GetInProgressItems(user);
            }
            if (ListType.Equals("recentlyaddeditems", StringComparison.OrdinalIgnoreCase))
            {
                return parent.GetRecentlyAddedItems(user);
            }
            if (ListType.Equals("recentlyaddedunplayeditems", StringComparison.OrdinalIgnoreCase))
            {
                return parent.GetRecentlyAddedUnplayedItems(user);
            }
            if (ListType.Equals("itemswithgenre", StringComparison.OrdinalIgnoreCase))
            {
                return parent.GetItemsWithGenre(QueryString["name"], user);
            }
            if (ListType.Equals("itemswithyear", StringComparison.OrdinalIgnoreCase))
            {
                return parent.GetItemsWithYear(int.Parse(QueryString["year"]), user);
            }
            if (ListType.Equals("itemswithstudio", StringComparison.OrdinalIgnoreCase))
            {
                return parent.GetItemsWithStudio(QueryString["name"], user);
            }
            if (ListType.Equals("itemswithperson", StringComparison.OrdinalIgnoreCase))
            {
                return parent.GetItemsWithPerson(QueryString["name"], null, user);
            }
            if (ListType.Equals("favorites", StringComparison.OrdinalIgnoreCase))
            {
                return parent.GetFavoriteItems(user);
            }

            throw new InvalidOperationException();
        }

        protected string ItemId
        {
            get
            {
                return QueryString["id"];
            }
        }

        private string ListType
        {
            get
            {
                return QueryString["listtype"] ?? string.Empty;
            }
        }
    }
}
