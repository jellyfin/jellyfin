using System;
using System.Collections.Generic;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Gets all items within containing a person
    /// </summary>
    public class ItemsWithPersonHandler : ItemListHandler
    {
        protected override IEnumerable<BaseItem> ItemsToSerialize
        {
            get
            {
                Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;

                PersonType? personType = null;

                string type = QueryString["persontype"];

                if (!string.IsNullOrEmpty(type))
                {
                    personType = (PersonType)Enum.Parse(typeof(PersonType), type, true);
                }

                return Kernel.Instance.GetItemsWithPerson(parent, QueryString["name"], personType, UserId);
            }
        }
    }
}
