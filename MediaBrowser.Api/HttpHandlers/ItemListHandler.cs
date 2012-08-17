using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class ItemListHandler : BaseJsonHandler<IEnumerable<BaseItemContainer<BaseItem>>>
    {
        protected override IEnumerable<BaseItemContainer<BaseItem>> GetObjectToSerialize()
        {
            return ItemsToSerialize.Select(i =>
            {
                return ApiService.GetSerializationObject(i, false, UserId);

            });
        }

        protected IEnumerable<BaseItem> ItemsToSerialize
        {
            get
            {
                Folder parent = ApiService.GetItemById(ItemId) as Folder;
                
                if (ListType.Equals("inprogressitems", StringComparison.OrdinalIgnoreCase))
                {
                    return Kernel.Instance.GetInProgressItems(parent, UserId);
                }
                else if (ListType.Equals("recentlyaddeditems", StringComparison.OrdinalIgnoreCase))
                {
                    return Kernel.Instance.GetRecentlyAddedItems(parent, UserId);
                }
                else if (ListType.Equals("recentlyaddedunplayeditems", StringComparison.OrdinalIgnoreCase))
                {
                    return Kernel.Instance.GetRecentlyAddedUnplayedItems(parent, UserId);
                }
                else if (ListType.Equals("itemswithgenre", StringComparison.OrdinalIgnoreCase))
                {
                    return Kernel.Instance.GetItemsWithGenre(parent, QueryString["name"], UserId);
                }
                else if (ListType.Equals("itemswithyear", StringComparison.OrdinalIgnoreCase))
                {
                    return Kernel.Instance.GetItemsWithYear(parent, int.Parse(QueryString["year"]), UserId);
                }
                else if (ListType.Equals("itemswithstudio", StringComparison.OrdinalIgnoreCase))
                {
                    return Kernel.Instance.GetItemsWithStudio(parent, QueryString["name"], UserId);
                }
                else if (ListType.Equals("itemswithperson", StringComparison.OrdinalIgnoreCase))
                {
                    return Kernel.Instance.GetItemsWithPerson(parent, QueryString["name"], UserId);
                }

                throw new InvalidOperationException();
            }
        }

        protected string ItemId
        {
            get
            {
                return QueryString["id"];
            }
        }

        protected Guid UserId
        {
            get
            {
                return Guid.Parse(QueryString["userid"]);
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
