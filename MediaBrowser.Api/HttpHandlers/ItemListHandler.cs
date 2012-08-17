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

                User user = Kernel.Instance.Users.First(u => u.Id == UserId);
                
                if (ListType.Equals("inprogressitems", StringComparison.OrdinalIgnoreCase))
                {
                    return parent.GetInProgressItems(user);
                }
                else if (ListType.Equals("recentlyaddeditems", StringComparison.OrdinalIgnoreCase))
                {
                    return parent.GetRecentlyAddedItems(user);
                }
                else if (ListType.Equals("recentlyaddedunplayeditems", StringComparison.OrdinalIgnoreCase))
                {
                    return parent.GetRecentlyAddedUnplayedItems(user);
                }
                else if (ListType.Equals("itemswithgenre", StringComparison.OrdinalIgnoreCase))
                {
                    return parent.GetItemsWithGenre(QueryString["name"], user);
                }
                else if (ListType.Equals("itemswithyear", StringComparison.OrdinalIgnoreCase))
                {
                    return parent.GetItemsWithYear(int.Parse(QueryString["year"]), user);
                }
                else if (ListType.Equals("itemswithstudio", StringComparison.OrdinalIgnoreCase))
                {
                    return parent.GetItemsWithStudio(QueryString["name"], user);
                }
                else if (ListType.Equals("itemswithperson", StringComparison.OrdinalIgnoreCase))
                {
                    return parent.GetItemsWithPerson(QueryString["name"], null, user);
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
